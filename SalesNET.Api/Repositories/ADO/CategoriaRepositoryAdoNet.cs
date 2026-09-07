using System.Data;
using Microsoft.Data.SqlClient;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Repositories.ADO
{
    public class CategoriaRepositoryAdoNet : ICategoriaRepository
    {
        private readonly string _connectionString;

        public CategoriaRepositoryAdoNet(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync()
        {
            var categorias = new List<CategoriaDto>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_LISTAR_CATEGORIAS", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categorias.Add(new CategoriaDto
                {
                    CategoriaID = reader.GetInt32(reader.GetOrdinal("CategoriaID")),
                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Descripcion"))
                });
            }

            return categorias;
        }

        public async Task<ResultadoOperacion<int>> CrearCategoriaAsync(CategoriaDto categoria)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_CREAR_CATEGORIA", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 100) { Value = categoria.Nombre });
            command.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 255) { Value = (object?)categoria.Descripcion ?? DBNull.Value });

            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };
            var idCategoriaParam = new SqlParameter("@CategoriaID", SqlDbType.Int) { Direction = ParameterDirection.Output };

            command.Parameters.Add(codMensajeParam);
            command.Parameters.Add(mensajeParam);
            command.Parameters.Add(idCategoriaParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            bool exito = codMensajeParam.Value != DBNull.Value && (int)codMensajeParam.Value == 1;

            return new ResultadoOperacion<int>
            {
                Exito = exito,
                Mensaje = mensajeParam.Value != DBNull.Value ? mensajeParam.Value.ToString()! : string.Empty,
                Data = idCategoriaParam.Value != DBNull.Value ? (int)idCategoriaParam.Value : 0
            };
        }

        public async Task<ResultadoOperacion> ActualizarCategoriaAsync(CategoriaDto categoria)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_ACTUALIZAR_CATEGORIA", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = categoria.CategoriaID });
            command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 100) { Value = categoria.Nombre });
            command.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 255) { Value = (object?)categoria.Descripcion ?? DBNull.Value });

            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

            command.Parameters.Add(codMensajeParam);
            command.Parameters.Add(mensajeParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return new ResultadoOperacion
            {
                Exito = codMensajeParam.Value != DBNull.Value && (int)codMensajeParam.Value == 1,
                Mensaje = mensajeParam.Value != DBNull.Value ? mensajeParam.Value.ToString()! : string.Empty
            };
        }

        public async Task<ResultadoOperacion> EliminarCategoriaAsync(int categoriaId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_ELIMINAR_CATEGORIA", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = categoriaId });

            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

            command.Parameters.Add(codMensajeParam);
            command.Parameters.Add(mensajeParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return new ResultadoOperacion
            {
                Exito = codMensajeParam.Value != DBNull.Value && (int)codMensajeParam.Value == 1,
                Mensaje = mensajeParam.Value != DBNull.Value ? mensajeParam.Value.ToString()! : string.Empty
            };
        }
    }
}
