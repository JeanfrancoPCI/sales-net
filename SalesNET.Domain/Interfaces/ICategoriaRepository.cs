using SalesNET.Domain.DTOs;

namespace SalesNET.Domain.Interfaces
{
    public interface ICategoriaRepository
    {
        Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync();
        Task<ResultadoOperacion<int>> CrearCategoriaAsync(CategoriaDto categoria);
        Task<ResultadoOperacion> ActualizarCategoriaAsync(CategoriaDto categoria);
        Task<ResultadoOperacion> EliminarCategoriaAsync(int categoriaId);
    }
}
