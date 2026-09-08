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
    public class OrdenRepositoryEfCore : IOrdenRepository
    {
        private readonly SalesBDContext _context;

        public OrdenRepositoryEfCore(SalesBDContext context)
        {
            _context = context;
        }

        public async Task<ResultadoOperacion<OrdenDto>> ActualizarProductoOrdenAsync(int ordenId, int productoId, int cantidad)
        {
            if (cantidad < 0)
            {
                return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "La cantidad no puede ser negativa.", Data = null };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ordenExiste = await _context.Ordenes.AnyAsync(o => o.OrdenID == ordenId && o.Activo);
                if (!ordenExiste)
                {
                    await transaction.RollbackAsync();
                    return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "La orden no existe o ha sido eliminada.", Data = null };
                }

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoID == productoId && p.Activo);
                if (producto is null)
                {
                    await transaction.RollbackAsync();
                    return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "El producto con ID " + productoId + " no existe o ha sido eliminado.", Data = null };
                }

                var detalleExistente = await _context.OrdenDetalles
                    .FirstOrDefaultAsync(od => od.OrdenID == ordenId && od.ProductoID == productoId);

                if (detalleExistente is null)
                {
                    if (cantidad == 0)
                    {
                        await transaction.RollbackAsync();
                        return new ResultadoOperacion<OrdenDto>
                        {
                            Exito = false,
                            Mensaje = "El producto no está en la orden; no hay nada que eliminar.",
                            Data = null
                        };
                    }

                    _context.OrdenDetalles.Add(new OrdenDetalle
                    {
                        OrdenID = ordenId,
                        ProductoID = productoId,
                        Cantidad = cantidad,
                        Precio = producto.Precio
                    });
                }
                else
                {
                    if (cantidad == 0)
                    {
                        var detallesActuales = await _context.OrdenDetalles.CountAsync(od => od.OrdenID == ordenId);
                        if (detallesActuales <= 1)
                        {
                            await transaction.RollbackAsync();
                            return new ResultadoOperacion<OrdenDto>
                            {
                                Exito = false,
                                Mensaje = "No se puede quitar el último producto de la orden. Si deseas cancelarla por completo, usa EliminarOrdenAsync.",
                                Data = null
                            };
                        }

                        _context.OrdenDetalles.Remove(detalleExistente);
                    }
                    else
                    {
                        detalleExistente.Cantidad = cantidad;
                    }
                }

                await _context.SaveChangesAsync();

                var total = await _context.OrdenDetalles
                    .Where(od => od.OrdenID == ordenId)
                    .SumAsync(od => (decimal?)(od.Cantidad * od.Precio)) ?? 0m;

                var ordenEntidad = await _context.Ordenes.FirstAsync(o => o.OrdenID == ordenId);
                ordenEntidad.Total = total;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new ResultadoOperacion<OrdenDto>
                {
                    Exito = true,
                    Mensaje = "Orden actualizada exitosamente.",
                    Data = await ObtenerOrdenPorIdAsync(ordenId)
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = $"Error al crear la orden: {ex.Message}", Data = null };
            }
        }

        public async Task<ResultadoOperacion<int>> CrearOrdenAsync(OrdenDto orden)
        {
            if (orden.Detalles is null || orden.Detalles.Count == 0)
            {
                return new ResultadoOperacion<int> { Exito = false, Mensaje = "La orden debe incluir al menos un producto." };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.ClienteID == orden.ClienteID && c.Activo);
                if (!clienteExiste)
                {
                    await transaction.RollbackAsync();
                    return new ResultadoOperacion<int> { Exito = false, Mensaje = "El cliente no existe o ha sido eliminado." };
                }

                var detalles = new List<OrdenDetalle>();

                foreach (var detalle in orden.Detalles)
                {
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.ProductoID == detalle.ProductoID && p.Activo);

                    if (producto is null)
                    {
                        await transaction.RollbackAsync();
                        return new ResultadoOperacion<int> { Exito = false, Mensaje = "El producto con ID " + detalle.ProductoID + " no existe o ha sido eliminado." };
                    }

                    detalles.Add(new OrdenDetalle
                    {
                        ProductoID = detalle.ProductoID,
                        Cantidad = detalle.Cantidad,
                        Precio = producto.Precio
                    });
                }

                var nuevaOrden = new Orden
                {
                    ClienteID = orden.ClienteID,
                    Fecha = orden.Fecha,
                    Total = 0m,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    Detalles = detalles
                };

                _context.Ordenes.Add(nuevaOrden);
                await _context.SaveChangesAsync();

                nuevaOrden.Total = detalles.Sum(d => d.Cantidad * d.Precio);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new ResultadoOperacion<int>
                {
                    Exito = true,
                    Mensaje = "Orden creada exitosamente.",
                    Data = nuevaOrden.OrdenID
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultadoOperacion<int> { Exito = false, Mensaje = $"Error al crear la orden: {ex.Message}" };
            }
        }

        public async Task<ResultadoOperacion> EliminarOrdenAsync(int ordenId)
        {
            var orden = await _context.Ordenes.FirstOrDefaultAsync(o => o.OrdenID == ordenId && o.Activo);
            if (orden is null)
                return new ResultadoOperacion { Exito = false, Mensaje = "No se ha podido eliminar la orden." };

            orden.Activo = false;
            orden.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = "Orden eliminada correctamente." };
        }

        public async Task<IEnumerable<OrdenDto>> ObtenerOrdenesAsync(int? clienteId = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var query = _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Detalles)
                .Where(o => o.Detalles.Any())
                .AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(o => o.ClienteID == clienteId.Value);

            if (fechaInicio.HasValue)
                query = query.Where(o => o.Fecha >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(o => o.Fecha <= fechaFin.Value);

            return await query
                .OrderByDescending(o => o.Fecha)
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

        public async Task<OrdenDto?> ObtenerOrdenPorIdAsync(int ordenId)
        {
            var orden = await _context.Ordenes
                .Where(o => o.OrdenID == ordenId && o.Activo && o.Detalles.Any())
                .Select(o => new OrdenDto
                {
                    OrdenID = o.OrdenID,
                    ClienteID = o.ClienteID,
                    Cliente = o.Cliente!.Nombre,
                    Fecha = o.Fecha,
                    Total = o.Total,
                    TotalProductos = o.Detalles.Sum(d => d.Cantidad)
                })
                .FirstOrDefaultAsync();

            if (orden is not null)
            {
                orden.Detalles = (await ObtenerDetallesOrdenAsync(ordenId)).ToList();
            }

            return orden;
        }

        public async Task<IEnumerable<OrdenDetalleDto>> ObtenerDetallesOrdenAsync(int ordenId)
        {
            return await _context.OrdenDetalles
                .Where(od => od.OrdenID == ordenId)
                .Select(od => new OrdenDetalleDto
                {
                    OrdenDetalleID = od.OrdenDetalleID,
                    ProductoID = od.ProductoID,
                    ProductoNombre = od.Producto!.Nombre,
                    Cantidad = od.Cantidad,
                    Precio = od.Precio
                })
                .ToListAsync();
        }
    }
}
