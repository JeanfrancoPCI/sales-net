IF DB_ID('SalesBD') IS NULL
BEGIN
    CREATE DATABASE SalesBD;
END
GO

USE SalesBD;
GO

IF OBJECT_ID('OrdenDetalle', 'U') IS NOT NULL DROP TABLE OrdenDetalle;
IF OBJECT_ID('Ordenes', 'U') IS NOT NULL DROP TABLE Ordenes;
IF OBJECT_ID('Clientes', 'U') IS NOT NULL DROP TABLE Clientes;
IF OBJECT_ID('Productos', 'U') IS NOT NULL DROP TABLE Productos;
IF OBJECT_ID('Categorias', 'U') IS NOT NULL DROP TABLE Categorias;

CREATE TABLE Categorias (
    CategoriaID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FechaModificacion DATETIME NULL
);

CREATE TABLE Productos (
    ProductoID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Precio DECIMAL(10, 2) NOT NULL,
    CategoriaID INT,
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FechaModificacion DATETIME NULL,
    FOREIGN KEY (CategoriaID) REFERENCES Categorias(CategoriaID)
);

CREATE TABLE Clientes (
    ClienteID INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    Telefono VARCHAR(15),
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FechaModificacion DATETIME NULL
);

CREATE TABLE Ordenes (
    OrdenID INT IDENTITY(1,1) PRIMARY KEY,
    ClienteID INT,
    Fecha DATETIME NOT NULL,
    Total DECIMAL(10, 2) NOT NULL,
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FechaModificacion DATETIME NULL,
    FOREIGN KEY (ClienteID) REFERENCES Clientes(ClienteID)
);

CREATE TABLE OrdenDetalle (
    OrdenDetalleID INT IDENTITY(1,1) PRIMARY KEY,
    OrdenID INT,
    ProductoID INT,
    Cantidad INT NOT NULL,
    Precio DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY (OrdenID) REFERENCES Ordenes(OrdenID),
    FOREIGN KEY (ProductoID) REFERENCES Productos(ProductoID)
);