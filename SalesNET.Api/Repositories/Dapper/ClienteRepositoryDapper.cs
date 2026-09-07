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
    public class ClienteRepositoryDapper : IClienteRepository
    {
        private readonly string _connectionString;

        public ClienteRepositoryDapper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<IEnumerable<ClienteDto>> ObtenerClientesAsync()
        {
            // Igual que en ADO.NET: SQL parametrizado directo, sin SP, por decisión explícita
            // (no hay SP_LISTAR_CLIENTES a propósito, para practicar la variante sin SP).
            const string query =
                @"SELECT ClienteID, Nombre, Email, Telefono, Activo, FechaCreacion, FechaModificacion
                FROM Clientes
                WHERE Activo = 1";

            using var connection = new SqlConnection(_connectionString);

            var clientes = await connection.QueryAsync<ClienteDto>(query);

            return clientes;
        }

        public async Task<ClienteDto?> ObtenerClientePorIdAsync(int clienteId)
        {
            const string query =
                @"SELECT ClienteID, Nombre, Email, Telefono, Activo, FechaCreacion, FechaModificacion
                FROM Clientes
                WHERE ClienteID = @ClienteID AND Activo = 1";

            using var connection = new SqlConnection(_connectionString);

            var cliente = await connection.QueryFirstOrDefaultAsync<ClienteDto>(query, new { ClienteID = clienteId });

            return cliente;
        }

        public async Task<ResultadoOperacion<int>> CrearClienteAsync(ClienteDto cliente)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@Nombre", cliente.Nombre, DbType.String, size: 100);
            parametros.Add("@Email", cliente.Email, DbType.String, size: 100);
            parametros.Add("@Telefono", cliente.Telefono, DbType.String, size: 20);
            parametros.Add("@ClienteID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 250);

            await connection.ExecuteAsync("SP_CREAR_CLIENTE", parametros, commandType: CommandType.StoredProcedure);

            int codMensaje = parametros.Get<int?>("@COD_MENSAJE") ?? 0;

            return new ResultadoOperacion<int>
            {
                Exito = codMensaje == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty,
                Data = codMensaje == 1 ? parametros.Get<int?>("@ClienteID") ?? 0 : 0
            };
        }

        public async Task<ResultadoOperacion> ActualizarClienteAsync(ClienteDto cliente)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@ClienteID", cliente.ClienteID, DbType.Int32);
            parametros.Add("@Nombre", cliente.Nombre, DbType.String, size: 100);
            parametros.Add("@Email", cliente.Email, DbType.String, size: 100);
            parametros.Add("@Telefono", cliente.Telefono, DbType.String, size: 20);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 250);

            await connection.ExecuteAsync("SP_ACTUALIZAR_CLIENTE", parametros, commandType: CommandType.StoredProcedure);

            return new ResultadoOperacion
            {
                Exito = (parametros.Get<int?>("@COD_MENSAJE") ?? 0) == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty
            };
        }

        public async Task<ResultadoOperacion> EliminarClienteAsync(int clienteId)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@ClienteID", clienteId, DbType.Int32);
            parametros.Add("@COD_MENSAJE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parametros.Add("@MENSAJE", dbType: DbType.String, direction: ParameterDirection.Output, size: 250);

            await connection.ExecuteAsync("SP_ELIMINAR_CLIENTE", parametros, commandType: CommandType.StoredProcedure);

            return new ResultadoOperacion
            {
                Exito = (parametros.Get<int?>("@COD_MENSAJE") ?? 0) == 1,
                Mensaje = parametros.Get<string?>("@MENSAJE") ?? string.Empty
            };
        }

        public async Task<IEnumerable<OrdenDto>> ObtenerOrdenesPorClienteAsync(int clienteId)
        {
            // Reutilizamos SP_LISTAR_ORDENES filtrando por @ClienteID (igual que ADO.NET).
            // Como la columna "Cliente" ahora coincide con la propiedad "Cliente" de OrdenDto,
            // Dapper mapea automático sin necesidad de leer filas dinámicas.
            using var connection = new SqlConnection(_connectionString);

            var ordenes = await connection.QueryAsync<OrdenDto>(
                "SP_LISTAR_ORDENES",
                new { ClienteID = clienteId, FechaInicio = (DateTime?)null, FechaFin = (DateTime?)null },
                commandType: CommandType.StoredProcedure);

            return ordenes;
        }
    }
}
