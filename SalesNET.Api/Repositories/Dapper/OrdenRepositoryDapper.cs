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
    public class OrdenRepositoryDapper : IOrdenRepository
    {
        private readonly string _connectionString;

        public OrdenRepositoryDapper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<ResultadoOperacion<OrdenDto>> ActualizarProductoOrdenAsync(int ordenId, int productoId, int cantidad)
        {
            if (cantidad < 0)
            {
                return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "La cantidad no puede ser negativa.", Data = null };
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var ordenExiste = await connection.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM Ordenes WHERE OrdenID = @OrdenId AND Activo = 1;",
                    new { OrdenId = ordenId },
                    transaction);

                if (ordenExiste is null)
                {
                    transaction.Rollback();
                    return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "La orden no existe o ha sido eliminada.", Data = null };
                }

                var precio = await connection.ExecuteScalarAsync<decimal?>(
                    "SELECT Precio FROM Productos WHERE ProductoID = @ProductoID AND Activo = 1;",
                    new { ProductoID = productoId },
                    transaction);

                if (precio is null)
                {
                    transaction.Rollback();
                    return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "El producto con ID " + productoId + " no existe o ha sido eliminado.", Data = null };
                }

                var yaExiste = await connection.ExecuteScalarAsync<int?>(
                    @"SELECT 1 FROM OrdenDetalle
                    WHERE OrdenID = @OrdenId AND ProductoID = @ProductoID;",
                    new { OrdenId = ordenId, ProductoID = productoId },
                    transaction);

                if (yaExiste is null)
                {
                    if (cantidad == 0)
                    {
                        transaction.Rollback();
                        return new ResultadoOperacion<OrdenDto>
                        {
                            Exito = false,
                            Mensaje = "El producto no está en la orden; no hay nada que eliminar.",
                            Data = null
                        };
                    }

                    await connection.ExecuteAsync(
                        @"INSERT INTO OrdenDetalle (OrdenID, ProductoID, Cantidad, Precio)
                        VALUES (@OrdenId, @ProductoID, @Cantidad, @Precio)",
                        new { OrdenId = ordenId, ProductoID = productoId, Cantidad = cantidad, Precio = precio },
                        transaction);
                }
                else
                {
                    if (cantidad == 0)
                    {
                        var detallesActuales = await connection.ExecuteScalarAsync<int>(
                            "SELECT COUNT(*) FROM OrdenDetalle WHERE OrdenID = @OrdenId",
                            new { OrdenId = ordenId },
                            transaction);

                        if (detallesActuales <= 1)
                        {
                            transaction.Rollback();
                            return new ResultadoOperacion<OrdenDto>
                            {
                                Exito = false,
                                Mensaje = "No se puede quitar el último producto de la orden. Si deseas cancelarla por completo, usa EliminarOrdenAsync.",
                                Data = null
                            };
                        }

                        await connection.ExecuteAsync(
                            @"DELETE FROM OrdenDetalle
                            WHERE OrdenID = @OrdenId
                                AND ProductoID = @ProductoID",
                            new { OrdenId = ordenId, ProductoID = productoId },
                            transaction);
                    }
                    else
                    {
                        await connection.ExecuteAsync(
                            @"UPDATE OrdenDetalle
                            SET Cantidad = @Cantidad
                            WHERE OrdenID = @OrdenId
                                AND ProductoID = @ProductoID",
                            new { OrdenId = ordenId, ProductoID = productoId, Cantidad = cantidad },
                            transaction);
                    }
                }

                await connection.ExecuteAsync(
                    @"UPDATE Ordenes
                    SET Total = ISNULL((SELECT SUM(Cantidad * Precio) FROM OrdenDetalle WHERE OrdenID = @OrdenId), 0)
                    WHERE OrdenID = @OrdenId",
                    new { OrdenId = ordenId },
                    transaction);

                transaction.Commit();

                return new ResultadoOperacion<OrdenDto>
                {
                    Exito = true,
                    Mensaje = "Orden actualizada exitosamente.",
                    Data = await ObtenerOrdenPorIdAsync(ordenId)
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = $"Error al crear la orden: {ex.Message}", Data = null };
            }
        }

        public async Task<ResultadoOperacion<int>> CrearOrdenAsync(OrdenDto orden)
        {
            if (orden.Detalles is null || orden.Detalles.Count == 0)
            {
                return new ResultadoOperacion<int> { Exito = false, Mensaje = "La orden debe incluir al menos un producto." };
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var clienteExiste = await connection.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM Clientes WHERE ClienteID = @ClienteID AND Activo = 1",
                    new { orden.ClienteID },
                    transaction);

                if (clienteExiste is null)
                {
                    transaction.Rollback();
                    return new ResultadoOperacion<int> { Exito = false, Mensaje = "El cliente no existe o ha sido eliminado." };
                }

                var detalles = new List<(int ProductoID, int Cantidad, decimal Precio)>();

                foreach (var detalle in orden.Detalles)
                {
                    var precio = await connection.ExecuteScalarAsync<decimal?>(
                        "SELECT Precio FROM Productos WHERE ProductoID = @ProductoID AND Activo = 1",
                        new { detalle.ProductoID },
                        transaction);

                    if (precio is null)
                    {
                        transaction.Rollback();
                        return new ResultadoOperacion<int> { Exito = false, Mensaje = "El producto con ID " + detalle.ProductoID + " no existe o ha sido eliminado." };
                    }

                    detalles.Add((detalle.ProductoID, detalle.Cantidad, precio.Value));
                }

                var parametrosOrden = new DynamicParameters();
                parametrosOrden.Add("@ClienteID", orden.ClienteID, DbType.Int32);
                parametrosOrden.Add("@Fecha", orden.Fecha, DbType.DateTime);
                parametrosOrden.Add("@OrdenId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    @"INSERT INTO Ordenes (ClienteID, Fecha, Total)
                    VALUES (@ClienteID, @Fecha, 0.00);
                    SET @OrdenId = SCOPE_IDENTITY();",
                    parametrosOrden,
                    transaction);

                int ordenId = parametrosOrden.Get<int>("@OrdenId");

                foreach (var detalle in detalles)
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO OrdenDetalle (OrdenID, ProductoID, Cantidad, Precio)
                        VALUES (@OrdenId, @ProductoID, @Cantidad, @Precio)",
                        new { OrdenId = ordenId, detalle.ProductoID, detalle.Cantidad, detalle.Precio },
                        transaction);
                }

                await connection.ExecuteAsync(
                    @"UPDATE Ordenes
                    SET Total = (SELECT SUM(Cantidad * Precio) FROM OrdenDetalle WHERE OrdenID = @OrdenId)
                    WHERE OrdenID = @OrdenId",
                    new { OrdenId = ordenId },
                    transaction);

                transaction.Commit();

                return new ResultadoOperacion<int>
                {
                    Exito = true,
                    Mensaje = "Orden creada exitosamente.",
                    Data = ordenId
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new ResultadoOperacion<int> { Exito = false, Mensaje = $"Error al crear la orden: {ex.Message}" };
            }
        }

        public async Task<ResultadoOperacion> EliminarOrdenAsync(int ordenId)
        {
            using var connection = new SqlConnection(_connectionString);

            var parametros = new DynamicParameters();
            parametros.Add("@OrdenId", ordenId, DbType.Int32);
            parametros.Add("@REGISTROS", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                @"UPDATE Ordenes
                SET Activo = 0,
                    FechaModificacion = GETDATE()
                WHERE OrdenID = @OrdenId
                    AND Activo = 1;
                SET @REGISTROS = @@ROWCOUNT;",
                parametros);

            int registros = parametros.Get<int>("@REGISTROS");

            return new ResultadoOperacion
            {
                Exito = registros > 0,
                Mensaje = registros > 0 ? "Orden eliminada correctamente." : "No se ha podido eliminar la orden."
            };
        }

        public async Task<IEnumerable<OrdenDto>> ObtenerOrdenesAsync(int? clienteId = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            using var connection = new SqlConnection(_connectionString);

            var ordenes = await connection.QueryAsync<OrdenDto>(
                "SP_LISTAR_ORDENES",
                new { ClienteID = clienteId, FechaInicio = fechaInicio, FechaFin = fechaFin },
                commandType: CommandType.StoredProcedure);

            return ordenes;
        }

        public async Task<OrdenDto?> ObtenerOrdenPorIdAsync(int ordenId)
        {
            const string query =
                @"SELECT O.OrdenID,
                    O.ClienteID,
                    C.Nombre AS Cliente,
                    O.Fecha,
                    O.Total,
                    SUM(OD.Cantidad) AS TotalProductos
                FROM Ordenes O
                    INNER JOIN Clientes C ON O.ClienteID = C.ClienteID
                    INNER JOIN OrdenDetalle OD ON O.OrdenID = OD.OrdenID
                WHERE O.OrdenID = @OrdenId
                    AND O.Activo = 1
                GROUP BY O.OrdenID, O.ClienteID, C.Nombre, O.Fecha, O.Total";

            using var connection = new SqlConnection(_connectionString);

            var orden = await connection.QueryFirstOrDefaultAsync<OrdenDto>(query, new { OrdenId = ordenId });

            if (orden is not null)
            {
                orden.Detalles = (await ObtenerDetallesOrdenAsync(ordenId)).ToList();
            }

            return orden;
        }

        public async Task<IEnumerable<OrdenDetalleDto>> ObtenerDetallesOrdenAsync(int ordenId)
        {
            // Alias como "ProductoNombre" (en vez de "Producto", como en la versión ADO.NET)
            // para que Dapper mapee automático a la propiedad del DTO sin lectura manual.
            const string query =
                @"SELECT OD.OrdenDetalleID,
                    OD.ProductoID,
                    P.Nombre AS ProductoNombre,
                    OD.Cantidad,
                    OD.Precio
                FROM OrdenDetalle OD
                    INNER JOIN Productos P ON OD.ProductoID = P.ProductoID
                WHERE OD.OrdenID = @OrdenId";

            using var connection = new SqlConnection(_connectionString);

            var detalles = await connection.QueryAsync<OrdenDetalleDto>(query, new { OrdenId = ordenId });

            return detalles;
        }
    }
}
