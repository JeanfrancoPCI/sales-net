namespace SalesNET.Api.Exceptions
{
    /// <summary>
    /// Se lanza cuando el segmento {proveedor} de la ruta no corresponde a ninguna
    /// implementación registrada (adonet, dapper, efcore). No es una regla de negocio
    /// de ninguna entidad: es una validación de infraestructura/routing, por eso se
    /// resuelve con una excepción propia en vez de con el patrón ResultadoOperacion
    /// que usan los repositorios. El GlobalExceptionHandler la traduce a un 400 BadRequest.
    /// </summary>
    public class ProveedorNoValidoException : Exception
    {
        public ProveedorNoValidoException(string proveedor, IEnumerable<string> proveedoresValidos)
            : base($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", proveedoresValidos)}.")
        {
        }
    }
}
