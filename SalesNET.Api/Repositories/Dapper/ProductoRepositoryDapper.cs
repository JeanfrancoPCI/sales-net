using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Repositories.Dapper
{
    public class ProductoRepositoryDapper : IProductoRepository
    {
        private readonly string _connectionString;

        public ProductoRepositoryDapper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerProductosAsync(int? categoriaId = null, string? nombre = null)
        {
            using var connection = new SqlConnection(_connectionString);

            var productos = await connection.QueryAsync<ProductoDto>(
                "SP_LISTAR_PRODUCTOS",
                new { CategoriaID = categoriaId, Nombre = nombre },
                commandType: CommandType.StoredProcedure);

            return productos;
        }

        public async Task<ResultadoOperacion<int>> CrearProductoAsync(ProductoDto producto)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@Nombre", producto.Nombre, DbType.String, size: 100);
            parametros.Add("@Precio", producto.Precio, DbType.Decimal, precision: 10, scale: 2);
            parametros.Add("@CategoriaID", producto.CategoriaID, DbType.Int32);
            parametros.Add("@ProductoID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 250);

            await connection.ExecuteAsync("SP_CREAR_PRODUCTO", parametros, commandType: CommandType.StoredProcedure);

            int codMensaje = parametros.Get<int?>("@COD_MENSAJE") ?? 0;

            return new ResultadoOperacion<int>
            {
                Exito = codMensaje == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty,
                Data = codMensaje == 1 ? parametros.Get<int?>("@ProductoID") ?? 0 : 0
            };
        }

        public async Task<ResultadoOperacion> ActualizarProductoAsync(ProductoDto producto)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@ProductoID", producto.ProductoID, DbType.Int32);
            parametros.Add("@Nombre", producto.Nombre, DbType.String, size: 100);
            parametros.Add("@Precio", producto.Precio, DbType.Decimal, precision: 10, scale: 2);
            parametros.Add("@CategoriaID", producto.CategoriaID, DbType.Int32);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 250);

            await connection.ExecuteAsync("SP_ACTUALIZAR_PRODUCTO", parametros, commandType: CommandType.StoredProcedure);

            return new ResultadoOperacion
            {
                Exito = (parametros.Get<int?>("@COD_MENSAJE") ?? 0) == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty
            };
        }

        public async Task<ResultadoOperacion> EliminarProductoAsync(int productoId)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@ProductoID", productoId, DbType.Int32);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 250);

            await connection.ExecuteAsync("SP_ELIMINAR_PRODUCTO", parametros, commandType: CommandType.StoredProcedure);

            return new ResultadoOperacion
            {
                Exito = (parametros.Get<int?>("@COD_MENSAJE") ?? 0) == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty
            };
        }
    }
}