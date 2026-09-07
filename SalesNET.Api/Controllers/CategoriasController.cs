using Microsoft.AspNetCore.Mvc;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Controllers
{
    [ApiController]
    [Route("api/{proveedor}/categorias")]
    public class CategoriasController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly string[] ProveedoresValidos = { "adonet", "dapper", "efcore" };

        public CategoriasController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private ICategoriaRepository? ObtenerRepositorio(string proveedor)
        {
            if (!ProveedoresValidos.Contains(proveedor)) return null;
            return _serviceProvider.GetRequiredKeyedService<ICategoriaRepository>(proveedor);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCategorias(string proveedor)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var categorias = await repo.ObtenerCategoriasAsync();
            return Ok(categorias);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoria(string proveedor, [FromBody] CategoriaDto categoria)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.CrearCategoriaAsync(categoria);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPut]
        public async Task<IActionResult> ActualizarCategoria(string proveedor, [FromBody] CategoriaDto categoria)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.ActualizarCategoriaAsync(categoria);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpDelete("{categoriaId:int}")]
        public async Task<IActionResult> EliminarCategoria(string proveedor, int categoriaId)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.EliminarCategoriaAsync(categoriaId);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
    }
}
