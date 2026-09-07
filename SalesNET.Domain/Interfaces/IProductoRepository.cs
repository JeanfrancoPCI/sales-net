using SalesNET.Domain.DTOs;

namespace SalesNET.Domain.Interfaces
{
    public interface IProductoRepository
    {
        Task<IEnumerable<ProductoDto>> ObtenerProductosAsync(int? categoriaId = null, string? nombre = null);
        Task<ResultadoOperacion<int>> CrearProductoAsync(ProductoDto producto);
        Task<ResultadoOperacion> ActualizarProductoAsync(ProductoDto producto);
        Task<ResultadoOperacion> EliminarProductoAsync(int productoId);
    }
}