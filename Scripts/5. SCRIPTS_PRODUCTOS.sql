USE SalesBD;
GO

CREATE OR ALTER PROCEDURE SP_CREAR_PRODUCTO
    @Nombre VARCHAR(100),
    @Precio DECIMAL(10, 2),
    @CategoriaID INT,
    @ProductoID INT OUTPUT,
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(250) OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Categorias WHERE CategoriaID = @CategoriaID)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'La categoría especificada no existe.';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Productos WHERE Nombre = @Nombre AND CategoriaID = @CategoriaID AND Activo = 1)
    BEGIN
        INSERT INTO Productos (Nombre, Precio, CategoriaID)
        VALUES (@Nombre, @Precio, @CategoriaID);

        SET @ProductoID = @@IDENTITY;

        SET @COD_MENSAJE = 1;
        SET @MENSAJE = 'Producto creado correctamente.';
    END
    ELSE
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'Ya existe un producto con el mismo nombre en esta categoría.';
        RETURN;
    END
END;
GO

CREATE OR ALTER PROCEDURE SP_ACTUALIZAR_PRODUCTO
    @ProductoID INT,
    @Nombre VARCHAR(100),
    @Precio DECIMAL(10, 2),
    @CategoriaID INT,
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(250) OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Productos WHERE ProductoID = @ProductoID AND Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'El producto especificado no existe o no está activo.';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Categorias WHERE CategoriaID = @CategoriaID AND Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'La categoría especificada no existe.';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Productos WHERE Nombre = @Nombre AND CategoriaID = @CategoriaID AND ProductoID <> @ProductoID AND Activo = 1)
    BEGIN
        UPDATE Productos
        SET Nombre = @Nombre,
            Precio = @Precio,
            CategoriaID = @CategoriaID,
            FechaModificacion = GETDATE()
        WHERE ProductoID = @ProductoID
            AND Activo = 1;

        SET @COD_MENSAJE = 1;
        SET @MENSAJE = 'Producto actualizado correctamente.';
    END
    ELSE
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'Ya existe un producto con el mismo nombre en esta categoría.';
        RETURN;
    END
END;
GO

CREATE OR ALTER PROCEDURE SP_ELIMINAR_PRODUCTO
    @ProductoID INT,
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(250) OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Productos WHERE ProductoID = @ProductoID AND Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'El producto especificado no existe o no está activo.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM OrdenDetalle od INNER JOIN Ordenes o ON od.OrdenID = o.OrdenID WHERE od.ProductoID = @ProductoID AND o.Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'No se puede eliminar el producto porque está asociado a una o más órdenes activas.';
        RETURN;
    END;

    UPDATE Productos
    SET Activo = 0,
        FechaModificacion = GETDATE()
     WHERE ProductoID = @ProductoID;

     SET @COD_MENSAJE = 1;
     SET @MENSAJE = 'Producto eliminado correctamente.';
END;
GO

CREATE OR ALTER PROCEDURE SP_LISTAR_PRODUCTOS
    @CategoriaID INT = NULL,
    @Nombre VARCHAR(100) = NULL
AS
BEGIN
    SELECT p.ProductoID, 
        p.Nombre, 
        p.Precio, 
        p.CategoriaID, 
        c.Nombre AS CategoriaNombre, 
        p.Activo, 
        p.FechaCreacion
    FROM Productos p
        INNER JOIN Categorias c ON p.CategoriaID = c.CategoriaID
    WHERE (@CategoriaID IS NULL OR p.CategoriaID = @CategoriaID)
      AND (@Nombre IS NULL OR p.Nombre LIKE '%' + @Nombre + '%')
      AND p.Activo = 1;
END;
GO