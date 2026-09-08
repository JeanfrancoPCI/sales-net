using Microsoft.AspNetCore.Mvc;
using SalesNET.Api.Services;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Controllers
{
    [ApiController]
    [Route("api/{proveedor}/categorias")]
    public class CategoriasController : ControllerBase
    {
        private readonly RepositorioProveedor<ICategoriaRepository> _repositorioProveedor;

        public CategoriasController(RepositorioProveedor<ICategoriaRepository> repositorioProveedor)
        {
            _repositorioProveedor = repositorioProveedor;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCategorias(string proveedor)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var categorias = await repo.ObtenerCategoriasAsync();
            return Ok(categorias);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoria(string proveedor, [FromBody] CategoriaDto categoria)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.CrearCategoriaAsync(categoria);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPut]
        public async Task<IActionResult> ActualizarCategoria(string proveedor, [FromBody] CategoriaDto categoria)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.ActualizarCategoriaAsync(categoria);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpDelete("{categoriaId:int}")]
        public async Task<IActionResult> EliminarCategoria(string proveedor, int categoriaId)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.EliminarCategoriaAsync(categoriaId);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
    }
}
