using SalesNET.Domain.DTOs;

namespace SalesNET.Domain.Interfaces
{
    public interface IOrdenRepository
    {
        Task<IEnumerable<OrdenDto>> ObtenerOrdenesAsync(int? clienteId = null, DateTime? fechaInicio = null, DateTime? fechaFin = null);
        Task<OrdenDto?> ObtenerOrdenPorIdAsync(int ordenId);
        Task<IEnumerable<OrdenDetalleDto>> ObtenerDetallesOrdenAsync(int ordenId);
        Task<ResultadoOperacion<int>> CrearOrdenAsync(OrdenDto orden);
        Task<ResultadoOperacion<OrdenDto>> ActualizarProductoOrdenAsync(int ordenId, int productoId, int cantidad);
        Task<ResultadoOperacion> EliminarOrdenAsync(int ordenId);
    }
}