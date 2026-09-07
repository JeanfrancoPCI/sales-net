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
    public class ProductoRepositoryEfCore : IProductoRepository
    {
        private readonly SalesBDContext _context;

        public ProductoRepositoryEfCore(SalesBDContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerProductosAsync(int? categoriaId = null, string? nombre = null)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaID == categoriaId.Value);

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(p => p.Nombre.Contains(nombre));

            return await query
                .Select(p => new ProductoDto
                {
                    ProductoID = p.ProductoID,
                    Nombre = p.Nombre,
                    Precio = p.Precio,
                    CategoriaID = p.CategoriaID,
                    CategoriaNombre = p.Categoria!.Nombre,
                    Activo = p.Activo,
                    FechaCreacion = p.FechaCreacion
                })
                .ToListAsync();
        }

        public async Task<ResultadoOperacion<int>> CrearProductoAsync(ProductoDto producto)
        {
            var categoriaExiste = await _context.Categorias.AnyAsync(c => c.CategoriaID == producto.CategoriaID && c.Activo);
            if (!categoriaExiste)
                return new ResultadoOperacion<int> { Exito = false, Mensaje = "La categoría especificada no existe." };

            var yaExiste = await _context.Productos
                .AnyAsync(p => p.Nombre == producto.Nombre && p.CategoriaID == producto.CategoriaID && p.Activo);
            if (yaExiste)
                return new ResultadoOperacion<int> { Exito = false, Mensaje = "Ya existe un producto con el mismo nombre en esta categoría." };

            var nuevoProducto = new Producto
            {
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                CategoriaID = producto.CategoriaID
            };

            _context.Productos.Add(nuevoProducto);
            await _context.SaveChangesAsync();

            return new ResultadoOperacion<int>
            {
                Exito = true,
                Mensaje = "Producto creado correctamente.",
                Data = nuevoProducto.ProductoID
            };
        }

        public async Task<ResultadoOperacion> ActualizarProductoAsync(ProductoDto producto)
        {
            var productoExistente = await _context.Productos
                .FirstOrDefaultAsync(p => p.ProductoID == producto.ProductoID && p.Activo);
            if (productoExistente is null)
                return new ResultadoOperacion { Exito = false, Mensaje = "El producto especificado no existe o no está activo." };

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.CategoriaID == producto.CategoriaID && c.Activo);
            if (!categoriaExiste)
                return new ResultadoOperacion { Exito = false, Mensaje = "La categoría especificada no existe." };

            var duplicado = await _context.Productos.AnyAsync(p =>
                p.Nombre == producto.Nombre &&
                p.CategoriaID == producto.CategoriaID &&
                p.ProductoID != producto.ProductoID &&
                p.Activo);
            if (duplicado)
                return new ResultadoOperacion { Exito = false, Mensaje = "Ya existe un producto con el mismo nombre en esta categoría." };

            productoExistente.Nombre = producto.Nombre;
            productoExistente.Precio = producto.Precio;
            productoExistente.CategoriaID = producto.CategoriaID;
            productoExistente.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = "Producto actualizado correctamente." };
        }

        public async Task<ResultadoOperacion> EliminarProductoAsync(int productoId)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.ProductoID == productoId && p.Activo);
            if (producto is null)
                return new ResultadoOperacion { Exito = false, Mensaje = "El producto especificado no existe o no está activo." };

            producto.Activo = false;
            producto.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = "Producto eliminado correctamente." };
        }
    }
}