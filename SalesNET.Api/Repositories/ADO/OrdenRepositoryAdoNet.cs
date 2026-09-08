using System.Data;
using Microsoft.Data.SqlClient;
using SalesNET.Domain.DTOs;
using SalesNET.Domain.Interfaces;

namespace SalesNET.Api.Repositories.ADO
{
    public class OrdenRepositoryAdoNet : IOrdenRepository
    {
        private readonly string _connectionString;

        public OrdenRepositoryAdoNet(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<ResultadoOperacion<OrdenDto>> ActualizarProductoOrdenAsync(int ordenId, int productoId, int cantidad)
        {
            decimal precio = 0.00m;

            if (cantidad < 0)
            {
                return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "La cantidad no puede ser negativa.", Data = null };
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                await using (
                    var ordenCmd = new SqlCommand(
                        "SELECT 1 FROM Ordenes WHERE OrdenID = @OrdenId AND Activo = 1;", 
                        connection, transaction)
                )
                {
                    ordenCmd.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });

                    if (await ordenCmd.ExecuteScalarAsync() is null)
                    {
                        await transaction.RollbackAsync();
                        return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "La orden no existe o ha sido eliminada.", Data = null };
                    }
                }

                await using (
                    var precioCmd = new SqlCommand(
                        "SELECT Precio FROM Productos WHERE ProductoID = @ProductoID AND Activo = 1;", 
                        connection, transaction)
                )
                {
                    precioCmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId });

                    var resultado = await precioCmd.ExecuteScalarAsync();
                    if (resultado is null)
                    {
                        await transaction.RollbackAsync();
                        return new ResultadoOperacion<OrdenDto> { Exito = false, Mensaje = "El producto con ID " + productoId + " no existe o ha sido eliminado.", Data = null };
                    }

                    precio = (decimal)resultado;
                }

                await using (
                    var ordenCmd = new SqlCommand(
                        @"SELECT 1 FROM OrdenDetalle 
                        WHERE OrdenID = @OrdenId AND ProductoID = @ProductoID;", 
                    connection, transaction)
                )
                {
                    ordenCmd.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });
                    ordenCmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId });

                    if (await ordenCmd.ExecuteScalarAsync() is null)
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

                        await using (
                            var detalleCmd = new SqlCommand(
                                @"INSERT INTO OrdenDetalle (OrdenID, ProductoID, Cantidad, Precio) 
                                VALUES (@OrdenId, @ProductoID, @Cantidad, @Precio)",
                            connection, transaction))
                        {
                            detalleCmd.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });
                            detalleCmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId });
                            detalleCmd.Parameters.Add(new SqlParameter("@Cantidad", SqlDbType.Int) { Value = cantidad });
                            detalleCmd.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Value = precio, Precision = 10, Scale = 2 });

                            await detalleCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        if (cantidad == 0)
                        {
                            await using (
                                var countCmd = new SqlCommand(
                                    "SELECT COUNT(*) FROM OrdenDetalle WHERE OrdenID = @OrdenId", 
                                connection, transaction))
                            {
                                countCmd.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });
                                int detallesActuales = (int)(await countCmd.ExecuteScalarAsync())!;

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
                            }

                            await using (
                                var detalleCmd = new SqlCommand(
                                    @"DELETE FROM OrdenDetalle
                                    WHERE OrdenID = @OrdenId
                                        AND ProductoID = @ProductoID",
                                connection, transaction))
                            {
                                detalleCmd.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });
                                detalleCmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId });

                                await detalleCmd.ExecuteNonQueryAsync();
                            } 
                        }
                        else
                        {
                            await using (
                                var detalleCmd = new SqlCommand(
                                    @"UPDATE OrdenDetalle
                                    SET Cantidad = @Cantidad
                                    WHERE OrdenID = @OrdenId
                                        AND ProductoID = @ProductoID",
                                connection, transaction))
                            {
                                detalleCmd.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });
                                detalleCmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = productoId });
                                detalleCmd.Parameters.Add(new SqlParameter("@Cantidad", SqlDbType.Int) { Value = cantidad });

                                await detalleCmd.ExecuteNonQueryAsync();
                            }   
                        }
                    }
                }

                await using (
                    var totalCmd = new SqlCommand(
                        @"UPDATE Ordenes
                        SET Total = ISNULL((SELECT SUM(Cantidad * Precio) FROM OrdenDetalle WHERE OrdenID = @OrdenId), 0)
                        WHERE OrdenID = @OrdenId", connection, transaction)
                )
                {
                    totalCmd.Parameters.Add(
                        new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId }
                    );
                    await totalCmd.ExecuteNonQueryAsync();
                }

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

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                await using (
                    var clienteCmd = new SqlCommand(
                        "SELECT 1 FROM Clientes WHERE ClienteID = @ClienteID AND Activo = 1", 
                        connection, transaction)
                )
                {
                    clienteCmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = orden.ClienteID });

                    if (await clienteCmd.ExecuteScalarAsync() is null)
                    {
                        await transaction.RollbackAsync();
                        return new ResultadoOperacion<int> { Exito = false, Mensaje = "El cliente no existe o ha sido eliminado." };
                    }
                }

                var detalles = new List<(int ProductoID, int Cantidad, decimal Precio)>();

                foreach (var detalle in orden.Detalles)
                {
                    await using var precioCmd = new SqlCommand(
                        "SELECT Precio FROM Productos WHERE ProductoID = @ProductoID AND Activo = 1", connection, transaction);

                    precioCmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = detalle.ProductoID });

                    var resultado = await precioCmd.ExecuteScalarAsync();
                    if (resultado is null)
                    {
                        await transaction.RollbackAsync();
                        return new ResultadoOperacion<int> { Exito = false, Mensaje = "El producto con ID " + detalle.ProductoID + " no existe o ha sido eliminado." };
                    }

                    detalles.Add((detalle.ProductoID, detalle.Cantidad, (decimal)resultado));
                }

                int ordenId;
                await using (
                    var insertOrdenCmd = new SqlCommand(
                        @"INSERT INTO Ordenes (ClienteID, Fecha, Total)
                        VALUES (@ClienteID, @Fecha, 0.00);
                        SET @OrdenId = SCOPE_IDENTITY();", connection, transaction))
                {
                    insertOrdenCmd.Parameters.AddRange(
                    [
                        new SqlParameter("@ClienteID", SqlDbType.Int) { Value = orden.ClienteID },
                        new SqlParameter("@Fecha", SqlDbType.DateTime) { Value = orden.Fecha },
                        new SqlParameter("@OrdenId", SqlDbType.Int) { Direction = ParameterDirection.Output }
                    ]);

                    await insertOrdenCmd.ExecuteNonQueryAsync();

                    ordenId = (int)insertOrdenCmd.Parameters["@OrdenId"].Value;
                }

                foreach (var detalle in detalles)
                {
                    await using var detalleCmd = new SqlCommand(
                        @"INSERT INTO OrdenDetalle (OrdenID, ProductoID, Cantidad, Precio) 
                        VALUES (@OrdenId, @ProductoID, @Cantidad, @Precio)",
                        connection, transaction);

                    detalleCmd.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });
                    detalleCmd.Parameters.Add(new SqlParameter("@ProductoID", SqlDbType.Int) { Value = detalle.ProductoID });
                    detalleCmd.Parameters.Add(new SqlParameter("@Cantidad", SqlDbType.Int) { Value = detalle.Cantidad });
                    detalleCmd.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Value = detalle.Precio, Precision = 10, Scale = 2 });

                    await detalleCmd.ExecuteNonQueryAsync();
                }

                await using (
                    var totalCmd = new SqlCommand(
                        @"UPDATE Ordenes
                        SET Total = (SELECT SUM(Cantidad * Precio) FROM OrdenDetalle WHERE OrdenID = @OrdenId)
                        WHERE OrdenID = @OrdenId", connection, transaction)
                )
                {
                    totalCmd.Parameters.Add(
                        new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId }
                    );
                    await totalCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new ResultadoOperacion<int>
                {
                    Exito = true,
                    Mensaje = "Orden creada exitosamente.",
                    Data = ordenId
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
            const string query =
                @"UPDATE Ordenes
                SET Activo = 0,
                    FechaModificacion = GETDATE()
                WHERE OrdenID = @OrdenId
                    AND Activo = 1;
                SET @REGISTROS = @@ROWCOUNT;";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text
            };

            command.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });
            command.Parameters.Add(new SqlParameter("@REGISTROS", SqlDbType.Int) { Direction = ParameterDirection.Output });

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            var registros = (int)command.Parameters["@REGISTROS"].Value;

            return new ResultadoOperacion
            {
                Exito = registros > 0,
                Mensaje =  (registros > 0) ? "Orden eliminada correctamente." : "No se ha podido eliminar la orden."
            };   
        }

        public async Task<IEnumerable<OrdenDto>> ObtenerOrdenesAsync(int? clienteId = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var ordenes = new List<OrdenDto>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SP_LISTAR_ORDENES", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddRange([
                new SqlParameter("@ClienteID", SqlDbType.Int) { Value = (object?)clienteId ?? DBNull.Value },
                new SqlParameter("@FechaInicio", SqlDbType.DateTime) { Value = (object?)fechaInicio ?? DBNull.Value },
                new SqlParameter("@FechaFin", SqlDbType.DateTime) { Value = (object?)fechaFin ?? DBNull.Value }
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

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text
            };

            command.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            OrdenDto? orden = null;
            
            if (await reader.ReadAsync())
            {
                orden = new OrdenDto
                {
                    OrdenID = reader.GetInt32(reader.GetOrdinal("OrdenID")),
                    ClienteID = reader.GetInt32(reader.GetOrdinal("ClienteID")),
                    Cliente = reader.GetString(reader.GetOrdinal("Cliente")),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                    TotalProductos = reader.GetInt32(reader.GetOrdinal("TotalProductos")),
                    Detalles = (await ObtenerDetallesOrdenAsync(ordenId)).ToList()
                };
            }

            return orden;
        }

        public async Task<IEnumerable<OrdenDetalleDto>> ObtenerDetallesOrdenAsync(int ordenId)
        {
            const string query = 
                @"SELECT OD.OrdenDetalleID,
                    OD.ProductoID, 
                    P.Nombre AS Producto, 
                    OD.Cantidad, 
                    OD.Precio
                FROM OrdenDetalle OD
                    INNER JOIN Productos P ON OD.ProductoID = P.ProductoID
                WHERE OD.OrdenID = @OrdenId";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text
            };

            command.Parameters.Add(new SqlParameter("@OrdenId", SqlDbType.Int) { Value = ordenId });

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            var detalles = new List<OrdenDetalleDto>();

            while (await reader.ReadAsync())
            {
                detalles.Add(new OrdenDetalleDto
                {
                    OrdenDetalleID = reader.GetInt32(reader.GetOrdinal("OrdenDetalleID")),
                    ProductoID = reader.GetInt32(reader.GetOrdinal("ProductoID")),
                    ProductoNombre = reader.GetString(reader.GetOrdinal("Producto")),
                    Cantidad = reader.GetInt32(reader.GetOrdinal("Cantidad")),
                    Precio = reader.GetDecimal(reader.GetOrdinal("Precio"))
                });
            }

            return detalles;
        }
    }
}