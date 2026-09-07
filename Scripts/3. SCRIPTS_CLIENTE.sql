USE SalesBD;
GO

CREATE OR ALTER PROCEDURE SP_CREAR_CLIENTE
     @Nombre NVARCHAR(100),
     @Email NVARCHAR(100),
     @Telefono NVARCHAR(20),
     @ClienteID INT OUTPUT,
     @COD_MENSAJE INT OUTPUT,
     @MENSAJE NVARCHAR(200) OUTPUT
 AS
 BEGIN
    IF EXISTS (SELECT 1 FROM Clientes WHERE Email = @Email AND Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'El correo electrónico ya está registrado.';
        RETURN;
    END;

    INSERT INTO Clientes (Nombre, Email, Telefono)
    VALUES (@Nombre, @Email, @Telefono);

    SET @ClienteID = SCOPE_IDENTITY();

    SET @COD_MENSAJE = 1;
    SET @MENSAJE = 'Cliente creado exitosamente.';
 END;
 GO

 CREATE OR ALTER PROCEDURE SP_ACTUALIZAR_CLIENTE
     @ClienteID INT,
     @Nombre NVARCHAR(100),
     @Email NVARCHAR(100),
     @Telefono NVARCHAR(20),
     @COD_MENSAJE INT OUTPUT,
     @MENSAJE NVARCHAR(200) OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Clientes WHERE ClienteID = @ClienteID)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'El cliente no existe.';
        RETURN;
    END;

    UPDATE Clientes
    SET Nombre = @Nombre,
        Email = @Email,
        Telefono = @Telefono
    WHERE ClienteID = @ClienteID;

    SET @COD_MENSAJE = 1;
    SET @MENSAJE = 'Cliente actualizado exitosamente.';
END;
GO

CREATE OR ALTER PROCEDURE SP_ELIMINAR_CLIENTE
    @ClienteID INT,
    @COD_MENSAJE INT OUTPUT,
    @MENSAJE NVARCHAR(200) OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Clientes WHERE ClienteID = @ClienteID)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'El cliente no existe.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM Ordenes WHERE ClienteID = @ClienteID and Activo = 1)
    BEGIN
        SET @COD_MENSAJE = 0;
        SET @MENSAJE = 'El cliente tiene ventas pendientes.';
        RETURN;
    END;

    UPDATE Clientes
    SET Activo = 0
    WHERE ClienteID = @ClienteID;

    SET @COD_MENSAJE = 1;
    SET @MENSAJE = 'Cliente eliminado exitosamente.';
END;
GO
