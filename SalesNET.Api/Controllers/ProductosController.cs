using Microsoft.AspNetCore.Mvc;
using SalesNET.Api.Exceptions;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Controllers
{
    [ApiController]
    [Route("api/{proveedor}/productos")]
    public class ProductosController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly string[] ProveedoresValidos = { "adonet", "efcore", "dapper" };

        public ProductosController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private IProductoRepository ObtenerRepositorio(string proveedor)
        {
            if (!ProveedoresValidos.Contains(proveedor))
            {
                throw new ProveedorNoValidoException(proveedor, ProveedoresValidos);
            }

            return _serviceProvider.GetRequiredKeyedService<IProductoRepository>(proveedor);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProductos(string proveedor, [FromQuery] int? categoriaId, [FromQuery] string? nombre)
        {
            var repo = ObtenerRepositorio(proveedor);

            var productos = await repo.ObtenerProductosAsync(categoriaId, nombre);
            return Ok(productos);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProducto(string proveedor, [FromBody] ProductoDto producto)
        {
            var repo = ObtenerRepositorio(proveedor);

            var resultado = await repo.CrearProductoAsync(producto);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPut]
        public async Task<IActionResult> ActualizarProducto(string proveedor, [FromBody] ProductoDto producto)
        {
            var repo = ObtenerRepositorio(proveedor);

            var resultado = await repo.ActualizarProductoAsync(producto);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpDelete("{productoId:int}")]
        public async Task<IActionResult> EliminarProducto(string proveedor, int productoId)
        {
            var repo = ObtenerRepositorio(proveedor);

            var resultado = await repo.EliminarProductoAsync(productoId);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
    }
}
