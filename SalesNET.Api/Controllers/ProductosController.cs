using Microsoft.AspNetCore.Mvc;
using SalesNET.Api.Services;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Controllers
{
    [ApiController]
    [Route("api/{proveedor}/productos")]
    public class ProductosController : ControllerBase
    {
        private readonly RepositorioProveedor<IProductoRepository> _repositorioProveedor;

        public ProductosController(RepositorioProveedor<IProductoRepository> repositorioProveedor)
        {
            _repositorioProveedor = repositorioProveedor;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProductos(string proveedor, [FromQuery] int? categoriaId, [FromQuery] string? nombre)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var productos = await repo.ObtenerProductosAsync(categoriaId, nombre);
            return Ok(productos);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProducto(string proveedor, [FromBody] ProductoDto producto)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.CrearProductoAsync(producto);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPut]
        public async Task<IActionResult> ActualizarProducto(string proveedor, [FromBody] ProductoDto producto)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.ActualizarProductoAsync(producto);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpDelete("{productoId:int}")]
        public async Task<IActionResult> EliminarProducto(string proveedor, int productoId)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.EliminarProductoAsync(productoId);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
    }
}
