USE SalesBD;
GO

CREATE OR ALTER PROCEDURE SP_CREAR_ORDEN
    @ClienteID INT,
    @Fecha DATETIME,
    @Productos NVARCHAR(MAX), -- cadena JSON que contiene los productos y sus cantidades ejemplo {"Productos":[{"ProductoID":1,"Cantidad":2},{"ProductoID":2,"Cantidad":1}] }
    @OrdenID INT OUTPUT,
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Clientes WHERE ClienteID = @ClienteID AND Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'El cliente no existe o ha sido eliminado.';
        RETURN;
    END

    DECLARE @ProductosTable  
        TABLE (
            ProductoID INT,
            Cantidad INT,
            Precio DECIMAL(10, 2)
        );

    INSERT INTO @ProductosTable (ProductoID, Cantidad, Precio)
    SELECT ProductoID, Cantidad, 0.00
    FROM OPENJSON(@Productos)
    WITH (
        ProductoID INT,
        Cantidad INT
    )
    
    
    UPDATE @ProductosTable
    SET Precio = P.Precio
    FROM @ProductosTable PT
        INNER JOIN Productos P ON PT.ProductoID = P.ProductoID
    WHERE P.Activo = 1;

    IF EXISTS (SELECT 1 FROM @ProductosTable WHERE Precio = 0.00)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'No se encontraron algunos productos válidos en la orden.';
        RETURN;
    END

    BEGIN TRANSACTION;

    BEGIN TRY
        INSERT INTO Ordenes (ClienteID, Fecha, Total)
        VALUES (@ClienteID, @Fecha, 0.00);

        SET @OrdenID = @@IDENTITY;

        INSERT INTO OrdenDetalle (OrdenID, ProductoID, Cantidad, Precio)
        SELECT @OrdenID, ProductoID, Cantidad, Precio
        FROM @ProductosTable;

        UPDATE Ordenes
        SET Total = (SELECT SUM(Cantidad * Precio) FROM @ProductosTable)
        WHERE OrdenID = @OrdenID;

        SET @COD_MENSAJE = 1;
        SET @MENSAJE = 'Orden creada exitosamente.';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'Error al crear la orden: ' + ERROR_MESSAGE();
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE SP_LISTAR_ORDENES
    @ClienteID INT = NULL,
    @FechaInicio DATETIME = NULL,
    @FechaFin DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT O.OrdenID, 
        O.ClienteID, 
        C.Nombre AS Cliente, 
        O.Fecha, 
        O.Total,
        SUM(OD.Cantidad) AS TotalProductos
    FROM Ordenes O
    INNER JOIN Clientes C ON O.ClienteID = C.ClienteID
    INNER JOIN OrdenDetalle OD ON O.OrdenID = OD.OrdenID
    WHERE (@ClienteID IS NULL OR O.ClienteID = @ClienteID)
      AND (@FechaInicio IS NULL OR O.Fecha >= @FechaInicio)
      AND (@FechaFin IS NULL OR O.Fecha <= @FechaFin)
    GROUP BY O.OrdenID, O.ClienteID, C.Nombre, O.Fecha, O.Total
    ORDER BY O.Fecha DESC;
END;
GO

CREATE OR ALTER PROCEDURE SP_OBTENER_DETALLE_ORDEN
    @OrdenID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT OD.OrdenDetalleID, 
        OD.OrdenID, 
        OD.ProductoID, 
        P.Nombre AS Producto, 
        C.Nombre AS Categoria,
        OD.Cantidad, 
        OD.Precio,
        (OD.Cantidad * OD.Precio) AS Subtotal
    FROM OrdenDetalle OD
        INNER JOIN Productos P ON OD.ProductoID = P.ProductoID
        INNER JOIN Categorias C ON P.CategoriaID = C.CategoriaID
    WHERE OD.OrdenID = @OrdenID
    ORDER BY OD.OrdenDetalleID;
END;

