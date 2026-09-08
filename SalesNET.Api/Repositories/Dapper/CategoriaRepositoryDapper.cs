using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Repositories.Dapper
{
    public class CategoriaRepositoryDapper : ICategoriaRepository
    {
        private readonly string _connectionString;

        public CategoriaRepositoryDapper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var categorias = await connection.QueryAsync<CategoriaDto>(
                "SP_LISTAR_CATEGORIAS",
                commandType: CommandType.StoredProcedure);

            return categorias;
        }

        public async Task<ResultadoOperacion<int>> CrearCategoriaAsync(CategoriaDto categoria)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@Nombre", categoria.Nombre, DbType.String, size: 100);
            parametros.Add("@Descripcion", categoria.Descripcion, DbType.String, size: 255);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
            parametros.Add("@CategoriaID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("SP_CREAR_CATEGORIA", parametros, commandType: CommandType.StoredProcedure);

            int codMensaje = parametros.Get<int?>("@COD_MENSAJE") ?? 0;

            return new ResultadoOperacion<int>
            {
                Exito = codMensaje == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty,
                Data = parametros.Get<int?>("@CategoriaID") ?? 0
            };
        }

        public async Task<ResultadoOperacion> ActualizarCategoriaAsync(CategoriaDto categoria)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@CategoriaID", categoria.CategoriaID, DbType.Int32);
            parametros.Add("@Nombre", categoria.Nombre, DbType.String, size: 100);
            parametros.Add("@Descripcion", categoria.Descripcion, DbType.String, size: 255);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);

            await connection.ExecuteAsync("SP_ACTUALIZAR_CATEGORIA", parametros, commandType: CommandType.StoredProcedure);

            return new ResultadoOperacion
            {
                Exito = (parametros.Get<int?>("@COD_MENSAJE") ?? 0) == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty
            };
        }

        public async Task<ResultadoOperacion> EliminarCategoriaAsync(int categoriaId)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@CategoriaID", categoriaId, DbType.Int32);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);

            await connection.ExecuteAsync("SP_ELIMINAR_CATEGORIA", parametros, commandType: CommandType.StoredProcedure);

            return new ResultadoOperacion
            {
                Exito = (parametros.Get<int?>("@COD_MENSAJE") ?? 0) == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty
            };
        }
    }
}
