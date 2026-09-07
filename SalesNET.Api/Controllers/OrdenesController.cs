using Microsoft.AspNetCore.Mvc;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Controllers
{
    [ApiController]
    [Route("api/{proveedor}/ordenes")]
    public class OrdenesController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly string[] ProveedoresValidos = { "adonet", "dapper", "efcore" };

        public OrdenesController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private IOrdenRepository? ObtenerRepositorio(string proveedor)
        {
            if (!ProveedoresValidos.Contains(proveedor)) return null;
            return _serviceProvider.GetRequiredKeyedService<IOrdenRepository>(proveedor);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerOrdenes(string proveedor, [FromQuery] int? clienteId, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var ordenes = await repo.ObtenerOrdenesAsync(clienteId, fechaInicio, fechaFin);
            return Ok(ordenes);
        }

        [HttpGet("{ordenId:int}")]
        public async Task<IActionResult> ObtenerOrdenPorId(string proveedor, int ordenId)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var orden = await repo.ObtenerOrdenPorIdAsync(ordenId);
            return orden is null ? NotFound($"No se encontró la orden con ID {ordenId}.") : Ok(orden);
        }

        [HttpGet("{ordenId:int}/detalles")]
        public async Task<IActionResult> ObtenerDetallesOrden(string proveedor, int ordenId)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var detalles = await repo.ObtenerDetallesOrdenAsync(ordenId);
            return Ok(detalles);
        }

        [HttpPost]
        public async Task<IActionResult> CrearOrden(string proveedor, [FromBody] OrdenDto orden)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.CrearOrdenAsync(orden);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPut("{ordenId:int}/productos/{productoId:int}")]
        public async Task<IActionResult> ActualizarProductoOrden(string proveedor, int ordenId, int productoId, [FromQuery] int cantidad)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.ActualizarProductoOrdenAsync(ordenId, productoId, cantidad);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpDelete("{ordenId:int}")]
        public async Task<IActionResult> EliminarOrden(string proveedor, int ordenId)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.EliminarOrdenAsync(ordenId);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
    }
}
