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
    public class CategoriaRepositoryEfCore : ICategoriaRepository
    {
        private readonly SalesBDContext _context;

        public CategoriaRepositoryEfCore(SalesBDContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync()
        {
            return await _context.Categorias
                .Where(c => c.Activo)
                .Select(c => new CategoriaDto
                {
                    CategoriaID = c.CategoriaID,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion
                })
                .ToListAsync();
        }

        public async Task<ResultadoOperacion<int>> CrearCategoriaAsync(CategoriaDto categoria)
        {
            var existente = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre == categoria.Nombre);
            if (existente is not null)
            {
                return new ResultadoOperacion<int>
                {
                    Exito = false,
                    Mensaje = "La categoría ya existe.",
                    Data = existente.CategoriaID
                };
            }

            var nuevaCategoria = new Categoria
            {
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion
            };

            _context.Categorias.Add(nuevaCategoria);
            await _context.SaveChangesAsync();

            return new ResultadoOperacion<int>
            {
                Exito = true,
                Mensaje = "Categoría creada correctamente.",
                Data = nuevaCategoria.CategoriaID
            };
        }

        public async Task<ResultadoOperacion> ActualizarCategoriaAsync(CategoriaDto categoria)
        {
            var categoriaExistente = await _context.Categorias
                .FirstOrDefaultAsync(c => c.CategoriaID == categoria.CategoriaID && c.Activo);
            if (categoriaExistente is null)
                return new ResultadoOperacion { Exito = false, Mensaje = "La categoría especificada no existe o no está activa." };

            var duplicada = await _context.Categorias.AnyAsync(c =>
                c.Nombre == categoria.Nombre &&
                c.CategoriaID != categoria.CategoriaID &&
                c.Activo);
            if (duplicada)
                return new ResultadoOperacion { Exito = false, Mensaje = "Ya existe otra categoría con el mismo nombre." };

            categoriaExistente.Nombre = categoria.Nombre;
            categoriaExistente.Descripcion = categoria.Descripcion;
            categoriaExistente.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = "Categoría actualizada correctamente." };
        }

        public async Task<ResultadoOperacion> EliminarCategoriaAsync(int categoriaId)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.CategoriaID == categoriaId && c.Activo);
            if (categoria is null)
                return new ResultadoOperacion { Exito = false, Mensaje = "La categoría no existe o ya ha sido eliminada." };

            var tieneProductosActivos = await _context.Productos
                .AnyAsync(p => p.CategoriaID == categoriaId && p.Activo);
            if (tieneProductosActivos)
                return new ResultadoOperacion { Exito = false, Mensaje = "No se puede eliminar la categoría porque tiene productos asociados." };

            categoria.Activo = false;
            categoria.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = "Categoría eliminada correctamente." };
        }
    }
}
