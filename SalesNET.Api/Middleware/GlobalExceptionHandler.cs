using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SalesNET.Api.Middleware
{
    /// <summary>
    /// Captura cualquier excepción no controlada que se escape de los controllers
    /// (fallas de conexión a SQL Server, errores de EF Core al guardar, etc.) y la
    /// traduce a una respuesta HTTP consistente en formato ProblemDetails.
    ///
    /// Este handler cubre únicamente errores técnicos inesperados que hoy se propagan
    /// sin control hasta convertirse en un 500 genérico de ASP.NET Core.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}", httpContext.Request.Method, httpContext.Request.Path);

            var (statusCode, titulo, detalle) = exception switch
            {
                SqlException => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Error de base de datos",
                    "No se pudo establecer conexión con la base de datos. Intenta nuevamente en unos minutos."),

                DbUpdateConcurrencyException => (
                    StatusCodes.Status409Conflict,
                    "Conflicto de concurrencia",
                    "El registro fue modificado o eliminado por otro proceso antes de poder completar esta operación."),

                DbUpdateException => (
                    StatusCodes.Status500InternalServerError,
                    "Error al guardar los datos",
                    "Ocurrió un error al intentar guardar los cambios en la base de datos."),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Error interno del servidor",
                    "Ocurrió un error inesperado. Contacta al administrador si el problema persiste.")
            };

            httpContext.Response.StatusCode = statusCode;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = titulo,
                    Detail = detalle
                }
            });
        }
    }
}
