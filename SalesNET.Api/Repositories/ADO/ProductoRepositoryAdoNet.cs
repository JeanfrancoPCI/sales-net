using System.Data;
using Microsoft.Data.SqlClient;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Repositories.ADO
{
        public class ProductoRepositoryAdoNet : IProductoRepository
    {
        private readonly string _connectionString;

        public ProductoRepositoryAdoNet(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerProductosAsync(int? categoriaId = null, string? nombre = null)
        {
            var productos = new List<ProductoDto>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_LISTAR_PRODUCTOS", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = (object?)categoriaId ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.VarChar, 100) { Value = (object?)nombre ?? DBNull.Value });

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                productos.Add(new ProductoDto
                {
                    ProductoID = reader.GetInt32(reader.GetOrdinal("ProductoID")),
                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                    Precio = reader.GetDecimal(reader.GetOrdinal("Precio")),
                    CategoriaID = reader.GetInt32(reader.GetOrdinal("CategoriaID")),
                    CategoriaNombre = reader.GetString(reader.GetOrdinal("CategoriaNombre")),
                    Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion"))
                });
            }

            return productos;
        }

        public async Task<ResultadoOperacion<int>> CrearProductoAsync(ProductoDto producto)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_CREAR_PRODUCTO", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.VarChar, 100) { Value = producto.Nombre });
            command.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Value = producto.Precio, Precision = 10, Scale = 2 });
            command.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = producto.CategoriaID });

            var productoIdParam = new SqlParameter("@ProductoID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

            command.Parameters.Add(productoIdParam);
            command.Parameters.Add(codMensajeParam);
            command.Parameters.Add(mensajeParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            bool exito = codMensajeParam.Value != DBNull.Value && (int)codMensajeParam.Value == 1;

            return new ResultadoOperacion<int>
            {
                Exito = exito,
                Mensaje = mensajeParam.Value != DBNull.Value ? mensajeParam.Value.ToString()! : string.Empty,
                Data = exito && productoIdParam.Value != DBNull.Value ? (int)productoIdParam.Value : 0
            };
        }

        public async Task<ResultadoOperacion> ActualizarProductoAsync(ProductoDto producto)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_ACTUALIZAR_PRODUCTO", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = producto.ProductoID });
            command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.VarChar, 100) { Value = producto.Nombre });
            command.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Value = producto.Precio, Precision = 10, Scale = 2 });
            command.Parameters.Add(new SqlParameter("@CategoriaID", SqlDbType.Int) { Value = producto.CategoriaID });

            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

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

        public async Task<ResultadoOperacion> EliminarProductoAsync(int productoId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_ELIMINAR_PRODUCTO", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId });

            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

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