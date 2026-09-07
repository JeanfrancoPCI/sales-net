USE SalesBD;
GO

CREATE OR ALTER PROCEDURE SP_CREAR_CATEGORIA
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(255),
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(255) OUTPUT,
    @CategoriaID INT OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Categorias WHERE Nombre = @Nombre AND Activo = 1)
    BEGIN
        INSERT INTO Categorias(Nombre, Descripcion)
        VALUES (@Nombre, @Descripcion);
        SET @CategoriaID = @@IDENTITY;

        SET @COD_MENSAJE = 1;
        SET @MENSAJE = 'Categoría creada correctamente.';
    END
    ELSE
    BEGIN
        SELECT @CategoriaID = CategoriaID
        FROM Categorias
        WHERE Nombre = @Nombre;

        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'La categoría ya existe.';
    END;
END;
GO


CREATE OR ALTER PROCEDURE SP_ACTUALIZAR_CATEGORIA
    @CategoriaID INT,
    @Nombre NVARCHAR(100),
    @Descripcion NVARCHAR(255),
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(255) OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Categorias WHERE CategoriaID = @CategoriaID AND Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'La categoría especificada no existe o no está activa.';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Categorias WHERE Nombre = @Nombre AND CategoriaID <> @CategoriaID AND Activo = 1)
    BEGIN
        UPDATE Categorias
        SET Nombre = @Nombre,
            Descripcion = @Descripcion,
            FechaModificacion = GETDATE()
        WHERE CategoriaID = @CategoriaID;

        SET @COD_MENSAJE = 1;
        SET @MENSAJE = 'Categoría actualizada correctamente.';
    END
    ELSE
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'Ya existe otra categoría con el mismo nombre.';
        RETURN;
    END
END;
GO

CREATE OR ALTER PROCEDURE SP_ELIMINAR_CATEGORIA
    @CategoriaID INT,
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(255) OUTPUT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Categorias WHERE CategoriaID = @CategoriaID AND Activo = 1)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Productos WHERE CategoriaID = @CategoriaID AND Activo = 1)
        BEGIN
            UPDATE Categorias
            SET Activo = 0, 
                FechaModificacion = GETDATE()
            WHERE CategoriaID = @CategoriaID;

            SET @COD_MENSAJE = 1;
            SET @MENSAJE = 'Categoría eliminada correctamente.';
        END
        ELSE
        BEGIN
            SET @COD_MENSAJE = 0;
            SET @MENSAJE = 'No se puede eliminar la categoría porque tiene productos asociados.';
        END
    END
    ELSE
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'La categoría no existe o ya ha sido eliminada.';
    END
END;
GO

CREATE OR ALTER PROCEDURE SP_LISTAR_CATEGORIAS
AS
BEGIN
    SELECT CategoriaID, Nombre, Descripcion
    FROM Categorias
    WHERE Activo = 1;
END;
GO
