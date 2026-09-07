using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesNET.Api.Data;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Entities;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Repositories.EFCore
{
    public class ClienteRepositoryEfCore : IClienteRepository
    {
        private readonly SalesBDContext _context;

        public ClienteRepositoryEfCore(SalesBDContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClienteDto>> ObtenerClientesAsync()
        {
            return await _context.Clientes
                .Where(c => c.Activo)
                .Select(c => new ClienteDto
                {
                    ClienteID = c.ClienteID,
                    Nombre = c.Nombre,
                    Email = c.Email,
                    Telefono = c.Telefono,
                    Activo = c.Activo,
                    FechaCreacion = c.FechaCreacion,
                    FechaModificacion = c.FechaModificacion
                })
                .ToListAsync();
        }

        public async Task<ClienteDto?> ObtenerClientePorIdAsync(int clienteId)
        {
            return await _context.Clientes
                .Where(c => c.ClienteID == clienteId && c.Activo)
                .Select(c => new ClienteDto
                {
                    ClienteID = c.ClienteID,
                    Nombre = c.Nombre,
                    Email = c.Email,
                    Telefono = c.Telefono,
                    Activo = c.Activo,
                    FechaCreacion = c.FechaCreacion,
                    FechaModificacion = c.FechaModificacion
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ResultadoOperacion<int>> CrearClienteAsync(ClienteDto cliente)
        {
            var emailExiste = await _context.Clientes.AnyAsync(c => c.Email == cliente.Email && c.Activo);
            if (emailExiste)
                return new ResultadoOperacion<int> { Exito = false, Mensaje = "El correo electrónico ya está registrado." };

            var nuevoCliente = new Cliente
            {
                Nombre = cliente.Nombre,
                Email = cliente.Email,
                Telefono = cliente.Telefono
            };

            _context.Clientes.Add(nuevoCliente);
            await _context.SaveChangesAsync();

            return new ResultadoOperacion<int>
            {
                Exito = true,
                Mensaje = "Cliente creado exitosamente.",
                Data = nuevoCliente.ClienteID
            };
        }

        public async Task<ResultadoOperacion> ActualizarClienteAsync(ClienteDto cliente)
        {
            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteID == cliente.ClienteID);

            if (clienteExistente is null)
                return new ResultadoOperacion { Exito = false, Mensaje = "El cliente no existe." };

            clienteExistente.Nombre = cliente.Nombre;
            clienteExistente.Email = cliente.Email;
            clienteExistente.Telefono = cliente.Telefono;

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = "Cliente actualizado exitosamente." };
        }

        public async Task<ResultadoOperacion> EliminarClienteAsync(int clienteId)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.ClienteID == clienteId);
            if (cliente is null)
                return new ResultadoOperacion { Exito = false, Mensaje = "El cliente no existe." };

            var tieneOrdenesActivas = await _context.Ordenes.AnyAsync(o => o.ClienteID == clienteId && o.Activo);
            if (tieneOrdenesActivas)
                return new ResultadoOperacion { Exito = false, Mensaje = "El cliente tiene ventas pendientes." };

            cliente.Activo = false;

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = "Cliente eliminado exitosamente." };
        }

        public async Task<IEnumerable<OrdenDto>> ObtenerOrdenesPorClienteAsync(int clienteId)
        {
            return await _context.Ordenes
                .Where(o => o.ClienteID == clienteId)
                .Select(o => new OrdenDto
                {
                    OrdenID = o.OrdenID,
                    ClienteID = o.ClienteID,
                    Cliente = o.Cliente!.Nombre,
                    Fecha = o.Fecha,
                    Total = o.Total,
                    TotalProductos = o.Detalles.Sum(d => d.Cantidad)
                })
                .ToListAsync();
        }
    }
}
