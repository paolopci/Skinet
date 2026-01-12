USE SkinetDB;
GO

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
    DROP TABLE dbo.Products;
GO

CREATE TABLE dbo.Products
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    PictureUrl NVARCHAR(200) NOT NULL,
    Type NVARCHAR(100) NOT NULL,
    Brand NVARCHAR(100) NOT NULL,
    QuantityInStock INT NOT NULL
);
GO

INSERT INTO dbo.Products (Name, Description, Price, PictureUrl, Type, Brand, QuantityInStock)
VALUES
('Laptop Pro 14', 'Laptop 14 inch con CPU moderna e SSD veloce.', 1299.99, 'images/products/laptop-pro-14.png', 'Laptop', 'Contoso', 25),
('Wireless Mouse', 'Mouse wireless ergonomico con batteria lunga durata.', 29.90, 'images/products/wireless-mouse.png', 'Accessory', 'Fabrikam', 200),
('Mechanical Keyboard', 'Tastiera meccanica con switch blu e retroilluminazione.', 89.50, 'images/products/mech-keyboard.png', 'Accessory', 'Northwind', 120),
('27in Monitor', 'Monitor 27 pollici IPS QHD.', 249.00, 'images/products/monitor-27.png', 'Monitor', 'AdventureWorks', 60),
('USB-C Hub', 'Hub USB-C 6-in-1 con HDMI e SD.', 49.99, 'images/products/usb-c-hub.png', 'Accessory', 'Tailspin', 180),
('Smartphone X', 'Smartphone con display OLED e doppia camera.', 799.00, 'images/products/smartphone-x.png', 'Phone', 'Contoso', 40),
('Tablet Air 11', 'Tablet leggero con penna e display 11 pollici.', 599.00, 'images/products/tablet-air-11.png', 'Tablet', 'Fabrikam', 55),
('Gaming Headset', 'Cuffie gaming con microfono e surround.', 69.90, 'images/products/gaming-headset.png', 'Accessory', 'Northwind', 140),
('External SSD 1TB', 'SSD esterno USB-C ad alte prestazioni.', 119.00, 'images/products/external-ssd-1tb.png', 'Storage', 'AdventureWorks', 90),
('4K Action Camera', 'Action cam 4K con stabilizzazione.', 149.99, 'images/products/action-camera-4k.png', 'Camera', 'Tailspin', 70);
GO
