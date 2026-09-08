using SalesNET.Api.Exceptions;

namespace SalesNET.Api.Services
{
    /// <summary>
    /// Resuelve la implementación keyed de <typeparamref name="TRepository"/> correspondiente
    /// al segmento {proveedor} de la ruta, centralizando la validación que antes estaba
    /// duplicada en cada controller.
    /// </summary>
    public class RepositorioProveedor<TRepository>(IServiceProvider serviceProvider) where TRepository : class
    {
        private static readonly string[] ProveedoresValidos = ["adonet", "dapper", "efcore"];

        public TRepository Resolver(string proveedor)
        {
            if (!ProveedoresValidos.Contains(proveedor))
            {
                throw new ProveedorNoValidoException(proveedor, ProveedoresValidos);
            }

            return serviceProvider.GetRequiredKeyedService<TRepository>(proveedor);
        }
    }
}
