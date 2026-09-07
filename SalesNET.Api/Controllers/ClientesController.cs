using Microsoft.AspNetCore.Mvc;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Controllers
{
    [ApiController]
    [Route("api/{proveedor}/clientes")]
    public class ClientesController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly string[] ProveedoresValidos = { "adonet", "dapper", "efcore" };

        public ClientesController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private IClienteRepository? ObtenerRepositorio(string proveedor)
        {
            if (!ProveedoresValidos.Contains(proveedor)) return null;
            return _serviceProvider.GetRequiredKeyedService<IClienteRepository>(proveedor);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerClientes(string proveedor)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var clientes = await repo.ObtenerClientesAsync();
            return Ok(clientes);
        }

        [HttpGet("{clienteId:int}")]
        public async Task<IActionResult> ObtenerClientePorId(string proveedor, int clienteId)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var cliente = await repo.ObtenerClientePorIdAsync(clienteId);
            return cliente is null ? NotFound($"No se encontró el cliente con ID {clienteId}.") : Ok(cliente);
        }

        [HttpGet("{clienteId:int}/ordenes")]
        public async Task<IActionResult> ObtenerOrdenesPorCliente(string proveedor, int clienteId)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var ordenes = await repo.ObtenerOrdenesPorClienteAsync(clienteId);
            return Ok(ordenes);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCliente(string proveedor, [FromBody] ClienteDto cliente)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.CrearClienteAsync(cliente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPut]
        public async Task<IActionResult> ActualizarCliente(string proveedor, [FromBody] ClienteDto cliente)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.ActualizarClienteAsync(cliente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpDelete("{clienteId:int}")]
        public async Task<IActionResult> EliminarCliente(string proveedor, int clienteId)
        {
            var repo = ObtenerRepositorio(proveedor);
            if (repo is null) return BadRequest($"Proveedor '{proveedor}' no válido. Usa: {string.Join(", ", ProveedoresValidos)}.");

            var resultado = await repo.EliminarClienteAsync(clienteId);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
    }
}
