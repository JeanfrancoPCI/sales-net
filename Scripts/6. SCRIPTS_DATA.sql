USE SalesBD;
GO

-- ============================================
-- SEED DATA: Categorías y Productos
-- Rubro: Computación y Sistemas
-- ============================================

IF NOT EXISTS (SELECT 1 FROM Categorias)
BEGIN
    INSERT INTO Categorias (Nombre, Descripcion) VALUES
        ('Laptops', 'Equipos portátiles para uso personal y profesional'),
        ('Componentes de PC', 'Procesadores, tarjetas de video, memorias RAM y placas madre'),
        ('Periféricos', 'Teclados, mouses y monitores'),
        ('Almacenamiento', 'Discos SSD, HDD y unidades USB'),
        ('Redes', 'Routers, switches, cables de red y repetidores WiFi'),
        ('Software y Licencias', 'Sistemas operativos, ofimática y antivirus'),
        ('Impresión', 'Impresoras, tóners y cartuchos de tinta'),
        ('Accesorios y Fundas', 'Mochilas, bases refrigerantes, hubs y cargadores'),
        ('Audio y Video', 'Audífonos, webcams, micrófonos y parlantes'),
        ('Gaming', 'Sillas, mandos, mousepads y auriculares gamer');
END
GO

IF NOT EXISTS (SELECT 1 FROM Productos)
BEGIN
    INSERT INTO Productos (Nombre, Precio, CategoriaID) VALUES
        -- Laptops
        ('Laptop Dell Inspiron 15 3520', 2199.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Laptops')),
        ('Laptop HP Pavilion 14', 2450.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Laptops')),
        ('Laptop Lenovo ThinkPad E14', 3199.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Laptops')),
        ('Laptop Asus VivoBook 15', 1899.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Laptops')),

        -- Componentes de PC
        ('Procesador Intel Core i5-13400F', 899.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Componentes de PC')),
        ('Procesador AMD Ryzen 5 7600', 950.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Componentes de PC')),
        ('Tarjeta de Video RTX 4060', 1899.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Componentes de PC')),
        ('Memoria RAM DDR4 16GB Kingston', 189.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Componentes de PC')),

        -- Periféricos
        ('Teclado Mecánico Logitech G413', 249.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Periféricos')),
        ('Mouse Inalámbrico Logitech MX Master 3', 349.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Periféricos')),
        ('Monitor LG 24" Full HD', 599.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Periféricos')),
        ('Monitor Samsung 27" Curvo', 899.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Periféricos')),

        -- Almacenamiento
        ('SSD Kingston NV2 1TB NVMe', 249.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Almacenamiento')),
        ('Disco Duro Externo Seagate 2TB', 289.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Almacenamiento')),
        ('Memoria USB SanDisk 64GB', 39.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Almacenamiento')),
        ('SSD Samsung 970 EVO 500GB', 199.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Almacenamiento')),

        -- Redes
        ('Router TP-Link Archer C6', 149.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Redes')),
        ('Switch de Red 8 Puertos TP-Link', 89.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Redes')),
        ('Cable de Red Cat6 (10m)', 29.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Redes')),
        ('Repetidor WiFi TP-Link RE305', 79.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Redes')),

        -- Software y Licencias
        ('Licencia Windows 11 Home', 399.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Software y Licencias')),
        ('Licencia Windows 11 Pro', 599.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Software y Licencias')),
        ('Licencia Microsoft Office 365', 249.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Software y Licencias')),
        ('Antivirus Kaspersky Total Security', 129.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Software y Licencias')),

        -- Impresión
        ('Impresora Multifuncional Epson L3250', 699.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Impresión')),
        ('Impresora HP LaserJet Pro M15w', 599.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Impresión')),
        ('Tóner HP 85A Original', 249.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Impresión')),
        ('Cartucho de Tinta Epson 664 Negro', 39.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Impresión')),

        -- Accesorios y Fundas
        ('Mochila para Laptop 15.6"', 89.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Accesorios y Fundas')),
        ('Base Refrigerante para Laptop', 69.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Accesorios y Fundas')),
        ('Hub USB-C 7 en 1', 99.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Accesorios y Fundas')),
        ('Cargador Universal para Laptop', 79.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Accesorios y Fundas')),

        -- Audio y Video
        ('Audífonos HyperX Cloud Stinger', 189.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Audio y Video')),
        ('Webcam Logitech C920', 249.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Audio y Video')),
        ('Micrófono USB Blue Yeti', 599.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Audio y Video')),
        ('Parlantes Logitech Z313', 129.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Audio y Video')),

        -- Gaming
        ('Silla Gamer Cougar Armor One', 899.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Gaming')),
        ('Mando Inalámbrico Xbox Series', 249.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Gaming')),
        ('Mousepad Gamer XL', 49.90, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Gaming')),
        ('Auriculares Gamer Razer Kraken', 299.00, (SELECT CategoriaID FROM Categorias WHERE Nombre = 'Gaming'));
END
GO
