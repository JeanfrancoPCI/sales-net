using Microsoft.AspNetCore.Mvc;
using SalesNET.Api.Services;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Controllers
{
    [ApiController]
    [Route("api/{proveedor}/clientes")]
    public class ClientesController : ControllerBase
    {
        private readonly RepositorioProveedor<IClienteRepository> _repositorioProveedor;

        public ClientesController(RepositorioProveedor<IClienteRepository> repositorioProveedor)
        {
            _repositorioProveedor = repositorioProveedor;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerClientes(string proveedor)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var clientes = await repo.ObtenerClientesAsync();
            return Ok(clientes);
        }

        [HttpGet("{clienteId:int}")]
        public async Task<IActionResult> ObtenerClientePorId(string proveedor, int clienteId)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var cliente = await repo.ObtenerClientePorIdAsync(clienteId);
            return cliente is null ? NotFound($"No se encontró el cliente con ID {clienteId}.") : Ok(cliente);
        }

        [HttpGet("{clienteId:int}/ordenes")]
        public async Task<IActionResult> ObtenerOrdenesPorCliente(string proveedor, int clienteId)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var ordenes = await repo.ObtenerOrdenesPorClienteAsync(clienteId);
            return Ok(ordenes);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCliente(string proveedor, [FromBody] ClienteDto cliente)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.CrearClienteAsync(cliente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPut]
        public async Task<IActionResult> ActualizarCliente(string proveedor, [FromBody] ClienteDto cliente)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.ActualizarClienteAsync(cliente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpDelete("{clienteId:int}")]
        public async Task<IActionResult> EliminarCliente(string proveedor, int clienteId)
        {
            var repo = _repositorioProveedor.Resolver(proveedor);

            var resultado = await repo.EliminarClienteAsync(clienteId);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
    }
}
