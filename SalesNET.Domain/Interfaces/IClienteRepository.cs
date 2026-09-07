using SalesNET.Domain.DTOs;

namespace SalesNET.Domain.Interfaces
{
    public interface IClienteRepository
    {
        Task<IEnumerable<ClienteDto>> ObtenerClientesAsync();
        Task<ClienteDto?> ObtenerClientePorIdAsync(int clienteId);
        Task<ResultadoOperacion<int>> CrearClienteAsync(ClienteDto cliente);
        Task<ResultadoOperacion> ActualizarClienteAsync(ClienteDto cliente);
        Task<ResultadoOperacion> EliminarClienteAsync(int clienteId);
        Task<IEnumerable<OrdenDto>> ObtenerOrdenesPorClienteAsync(int clienteId);
    }
}
