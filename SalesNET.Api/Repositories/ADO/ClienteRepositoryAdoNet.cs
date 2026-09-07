using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Repositories.ADO
{
    public class ClienteRepositoryAdoNet : IClienteRepository
    {
        private readonly string _connectionString;

        public ClienteRepositoryAdoNet(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<IEnumerable<ClienteDto>> ObtenerClientesAsync()
        {
            var clientes = new List<ClienteDto>();

            const string query =
                @"SELECT ClienteID, Nombre, Email, Telefono, Activo, FechaCreacion, FechaModificacion
                FROM Clientes
                WHERE Activo = 1";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text
            };

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                clientes.Add(new ClienteDto
                {
                    ClienteID = reader.GetInt32(reader.GetOrdinal("ClienteID")),
                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                    Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                    FechaModificacion = reader.IsDBNull(reader.GetOrdinal("FechaModificacion")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaModificacion"))
                });
            }

            return clientes;
        }

        public async Task<ClienteDto?> ObtenerClientePorIdAsync(int clienteId)
        {
            const string query = @"
                SELECT ClienteID, Nombre, Email, Telefono, Activo, FechaCreacion, FechaModificacion
                FROM Clientes
                WHERE ClienteID = @ClienteID AND Activo = 1";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text
            };

            command.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = clienteId });

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ClienteDto
                {
                    ClienteID = reader.GetInt32(reader.GetOrdinal("ClienteID")),
                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                    Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                    FechaModificacion = reader.IsDBNull(reader.GetOrdinal("FechaModificacion")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaModificacion"))
                };
            }

            return null;
        }

        public async Task<ResultadoOperacion<int>> CrearClienteAsync(ClienteDto cliente)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_CREAR_CLIENTE", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.VarChar, 100) { Value = cliente.Nombre });
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 100) { Value = cliente.Email });
            command.Parameters.Add(new SqlParameter("@Telefono", SqlDbType.VarChar, 20) { Value = (object?)cliente.Telefono ?? DBNull.Value });

            var clienteIdParam = new SqlParameter("@ClienteID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

            command.Parameters.Add(clienteIdParam);
            command.Parameters.Add(codMensajeParam);
            command.Parameters.Add(mensajeParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            bool exito = codMensajeParam.Value != DBNull.Value && (int)codMensajeParam.Value == 1;

            return new ResultadoOperacion<int>
            {
                Exito = exito,
                Mensaje = mensajeParam.Value != DBNull.Value ? mensajeParam.Value.ToString()! : string.Empty,
                Data = exito && clienteIdParam.Value != DBNull.Value ? (int)clienteIdParam.Value : 0
            };
        }

        public async Task<ResultadoOperacion> ActualizarClienteAsync(ClienteDto cliente)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_ACTUALIZAR_CLIENTE", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = cliente.ClienteID });
            command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.VarChar, 100) { Value = cliente.Nombre });
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 100) { Value = cliente.Email });
            command.Parameters.Add(new SqlParameter("@Telefono", SqlDbType.VarChar, 20) { Value = (object?)cliente.Telefono ?? DBNull.Value });

            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

            command.Parameters.Add(codMensajeParam);
            command.Parameters.Add(mensajeParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            bool exito = codMensajeParam.Value != DBNull.Value && (int)codMensajeParam.Value == 1;

            return new ResultadoOperacion
            {
                Exito = exito,
                Mensaje = mensajeParam.Value != DBNull.Value ? mensajeParam.Value.ToString()! : string.Empty
            };
        }

        public async Task<ResultadoOperacion> EliminarClienteAsync(int clienteId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_ELIMINAR_CLIENTE", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = clienteId });

            var codMensajeParam = new SqlParameter("@COD_MENSAJE", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.NVarChar, 250) { Direction = ParameterDirection.Output };

            command.Parameters.Add(codMensajeParam);
            command.Parameters.Add(mensajeParam);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            bool exito = codMensajeParam.Value != DBNull.Value && (int)codMensajeParam.Value == 1;

            return new ResultadoOperacion
            {
                Exito = exito,
                Mensaje = mensajeParam.Value != DBNull.Value ? mensajeParam.Value.ToString()! : string.Empty
            };
        }

        public async Task<IEnumerable<OrdenDto>> ObtenerOrdenesPorClienteAsync(int clienteId)
        {
            // Reutilizamos SP_LISTAR_ORDENES (el mismo que usa OrdenRepositoryAdoNet) filtrando
            // por @ClienteID, en vez de un SP propio que nunca llegó a crearse
            // (SP_OBTENER_ORDENES_POR_CLIENTE no existe en ningún script).
            var ordenes = new List<OrdenDto>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_LISTAR_ORDENES", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange([
                new SqlParameter("@ClienteID", SqlDbType.Int) { Value = clienteId },
                new SqlParameter("@FechaInicio", SqlDbType.DateTime) { Value = DBNull.Value },
                new SqlParameter("@FechaFin", SqlDbType.DateTime) { Value = DBNull.Value }
            ]);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ordenes.Add(new OrdenDto
                {
                    OrdenID = reader.GetInt32(reader.GetOrdinal("OrdenID")),
                    ClienteID = reader.GetInt32(reader.GetOrdinal("ClienteID")),
                    Cliente = reader.GetString(reader.GetOrdinal("Cliente")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                    TotalProductos = reader.GetInt32(reader.GetOrdinal("TotalProductos"))
                });
            }

            return ordenes;
        }
    }
}
