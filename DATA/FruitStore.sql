-- ================================================================
-- SCRIPT TẠO MỚI TOÀN BỘ DATABASE: FruitStoreDb
-- Ngày tạo: 2026-08-16 (Cập nhật Seed Data: 2026-08-17)
-- Mô tả: Khởi tạo mới database từ đầu với đầy đủ 11 bảng chuẩn + Seed Data mẫu
-- Hướng dẫn: Mở SQL Server Management Studio (SSMS) -> Paste & Execute
-- ================================================================

USE [master]
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = N'FruitStoreDb')
BEGIN
    ALTER DATABASE [FruitStoreDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [FruitStoreDb];
END
GO

CREATE DATABASE [FruitStoreDb]
GO

USE [FruitStoreDb]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- 1. BẢNG Roles
CREATE TABLE [dbo].[Roles] (
    [Id]       INT IDENTITY(1,1) NOT NULL,
    [RoleName] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

INSERT INTO [dbo].[Roles] ([RoleName]) VALUES 
    (N'Customer'),
    (N'Admin'),
    (N'Manager'),
    (N'Staff');
GO

-- 2. BẢNG Users
CREATE TABLE [dbo].[Users] (
    [Id]           INT IDENTITY(1,1) NOT NULL,
    [RoleId]       INT NOT NULL,
    [BranchId]     INT NULL,
    [FullName]     NVARCHAR(150) NOT NULL,
    [Email]        VARCHAR(255) NULL,
    [Phone]        VARCHAR(20) NULL,
    [Username]     VARCHAR(100) NOT NULL,
    [PasswordHash] VARCHAR(500) NOT NULL,
    [Avatar]       VARCHAR(500) NULL,
    [Address]      NVARCHAR(500) NULL,
    [IsActive]     BIT NOT NULL DEFAULT 1,
    [CreatedAt]    DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]    DATETIME2(7) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
    CONSTRAINT [UQ_Users_Username] UNIQUE ([Username])
);
GO

-- 3. BẢNG Branches
CREATE TABLE [dbo].[Branches] (
    [Id]         INT IDENTITY(1,1) NOT NULL,
    [ManagerId]  INT NOT NULL,
    [BranchName] NVARCHAR(200) NOT NULL,
    [Address]    NVARCHAR(500) NOT NULL,
    [Phone]      VARCHAR(20) NULL,
    [IsActive]   BIT NOT NULL DEFAULT 1,
    [CreatedAt]  DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Branches] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Branches_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Users]([Id])
);
GO

ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_Users_Branches] 
    FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]);
GO

-- 4. BẢNG Categories
CREATE TABLE [dbo].[Categories] (
    [Id]          INT IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [ImageUrl]    VARCHAR(500) NULL,
    [IsActive]    BIT NOT NULL DEFAULT 1,
    [CreatedAt]   DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- 5. BẢNG Products
CREATE TABLE [dbo].[Products] (
    [Id]                INT IDENTITY(1,1) NOT NULL,
    [CategoryId]        INT NOT NULL,
    [Name]              NVARCHAR(200) NOT NULL,
    [Description]       NVARCHAR(MAX) NULL,
    [Price]             DECIMAL(18,2) NOT NULL,
    [StockQuantity]     INT NOT NULL DEFAULT 0,
    [Unit]              NVARCHAR(50) NOT NULL,
    [ImageUrl]          VARCHAR(500) NULL,
    [MinStockThreshold] INT NOT NULL DEFAULT 10,
    [IsActive]          BIT NOT NULL DEFAULT 1,
    [CreatedAt]         DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]         DATETIME2(7) NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id]),
    CONSTRAINT [CK_Products_Price] CHECK ([Price] >= 0),
    CONSTRAINT [CK_Products_StockQuantity] CHECK ([StockQuantity] >= 0)
);
GO

-- 6. BẢNG Inventories
CREATE TABLE [dbo].[Inventories] (
    [Id]                INT IDENTITY(1,1) NOT NULL,
    [ProductId]         INT NOT NULL,
    [BranchId]          INT NOT NULL,
    [BatchCode]         NVARCHAR(50) NOT NULL,
    [QuantityReceived]  INT NOT NULL,
    [RemainingQuantity] INT NOT NULL,
    [ReceivedAt]        DATETIME NOT NULL DEFAULT GETDATE(),
    [ExpiryDate]        DATE NOT NULL,
    [UnitCost]          DECIMAL(18,2) NULL,
    [SellingPrice]      DECIMAL(18,2) NULL,
    [SupplierName]      NVARCHAR(150) NULL,
    [Note]              NVARCHAR(500) NULL,
    [CreatedAt]         DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Inventories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Inventories_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
    CONSTRAINT [FK_Inventories_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]),
    CONSTRAINT [CK_Inventories_QuantityReceived] CHECK ([QuantityReceived] > 0),
    CONSTRAINT [CK_Inventories_RemainingQuantity] CHECK ([RemainingQuantity] >= 0 AND [RemainingQuantity] <= [QuantityReceived]),
    CONSTRAINT [CK_Inventories_SellingPrice] CHECK ([SellingPrice] IS NULL OR [UnitCost] IS NULL OR [SellingPrice] >= [UnitCost])
);
GO

-- 7. BẢNG Orders
CREATE TABLE [dbo].[Orders] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [OrderCode]       VARCHAR(50) NOT NULL,
    [CustomerId]      INT NULL,
    [BranchId]        INT NULL,
    [StaffId]         INT NULL,
    [CustomerName]    NVARCHAR(150) NOT NULL,
    [CustomerPhone]   VARCHAR(20) NOT NULL,
    [CustomerEmail]   VARCHAR(150) NULL,
    [ShippingAddress] NVARCHAR(500) NOT NULL,
    [Note]            NVARCHAR(500) NULL,
    [TotalAmount]     DECIMAL(18,2) NOT NULL,
    [DiscountAmount]  DECIMAL(18,2) NOT NULL DEFAULT 0,
    [FinalAmount]     DECIMAL(18,2) NOT NULL,
    [OrderStatus]    VARCHAR(30) NOT NULL DEFAULT 'Pending',
    [CreatedAt]       DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]       DATETIME2(7) NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Orders_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Orders_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]),
    CONSTRAINT [FK_Orders_Staff] FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [UQ_Orders_OrderCode] UNIQUE ([OrderCode]),
    CONSTRAINT [CK_Orders_TotalAmount] CHECK ([TotalAmount] >= 0),
    CONSTRAINT [CK_Orders_FinalAmount] CHECK ([FinalAmount] >= 0),
    CONSTRAINT [CK_Orders_Status] CHECK ([OrderStatus] IN ('Pending', 'Approved', 'Confirmed', 'Shipping', 'Completed', 'Cancelled'))
);
GO

-- 8. BẢNG OrderDetails
CREATE TABLE [dbo].[OrderDetails] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [OrderId]         INT NOT NULL,
    [ProductId]       INT NOT NULL,
    [BatchId]         INT NULL,
    [ProductName]     NVARCHAR(200) NOT NULL,
    [UnitPrice]       DECIMAL(18,2) NOT NULL,
    [Quantity]        INT NOT NULL,
    [DiscountPercent] DECIMAL(5,2) NOT NULL DEFAULT 0,
    [SubTotal]        AS ([UnitPrice] * [Quantity] * (1 - [DiscountPercent]/100)) PERSISTED,
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
    CONSTRAINT [FK_OrderDetails_Inventories] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[Inventories]([Id]),
    CONSTRAINT [CK_OrderDetails_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_OrderDetails_UnitPrice] CHECK ([UnitPrice] >= 0),
    CONSTRAINT [CK_OrderDetails_DiscountPercent] CHECK ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100)
);
GO

-- 9. BẢNG Payments
CREATE TABLE [dbo].[Payments] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [OrderId]         INT NOT NULL,
    [PaymentMethod]   VARCHAR(30) NOT NULL,
    [PaymentStatus]   VARCHAR(30) NOT NULL DEFAULT 'Pending',
    [Amount]          DECIMAL(18,2) NOT NULL,
    [TransactionCode] VARCHAR(150) NULL,
    [PaymentUrl]      VARCHAR(1000) NULL,
    [PaidAt]          DATETIME2(7) NULL,
    [CreatedAt]       DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt]       DATETIME2(7) NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Payments_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]),
    CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] >= 0),
    CONSTRAINT [CK_Payments_Method] CHECK ([PaymentMethod] IN ('COD', 'BankTransfer', 'PayOS', 'Momo', 'VNPay')),
    CONSTRAINT [CK_Payments_Status] CHECK ([PaymentStatus] IN ('Pending', 'Paid', 'Failed', 'Cancelled', 'Refunded'))
);
GO

-- 10. BẢNG Carts
CREATE TABLE [dbo].[Carts] (
    [Id]         INT IDENTITY(1,1) NOT NULL,
    [CustomerId] INT NOT NULL,
    [ProductId]  INT NOT NULL,
    [BranchId]   INT NOT NULL,
    [Quantity]   INT NOT NULL,
    [AddedAt]    DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Carts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Carts_Users] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Carts_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
    CONSTRAINT [FK_Carts_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]),
    CONSTRAINT [CK_Carts_Quantity] CHECK ([Quantity] > 0)
);
GO

-- 11. BẢNG Notifications
CREATE TABLE [dbo].[Notifications] (
    [Id]        INT IDENTITY(1,1) NOT NULL,
    [UserId]    INT NOT NULL,
    [BranchId]  INT NULL,
    [Title]     NVARCHAR(200) NOT NULL,
    [Message]   NVARCHAR(MAX) NOT NULL,
    [Type]      VARCHAR(30) NOT NULL,
    [IsRead]    BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Notifications_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]),
    CONSTRAINT [CK_Notifications_Type] CHECK ([Type] IN ('LowStock', 'NearExpiry', 'NewOrder', 'OrderUpdate', 'Expired'))
);
GO

-- INDEXES
CREATE NONCLUSTERED INDEX [IX_Users_RoleId] ON [dbo].[Users] ([RoleId]);
CREATE NONCLUSTERED INDEX [IX_Users_BranchId] ON [dbo].[Users] ([BranchId]);
CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users] ([Email]);

CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products] ([CategoryId]);
CREATE NONCLUSTERED INDEX [IX_Products_Name] ON [dbo].[Products] ([Name]);

CREATE NONCLUSTERED INDEX [IX_Inventories_BranchId] ON [dbo].[Inventories] ([BranchId]);
CREATE NONCLUSTERED INDEX [IX_Inventories_ProductId] ON [dbo].[Inventories] ([ProductId]);
CREATE NONCLUSTERED INDEX [IX_Inventories_ExpiryDate] ON [dbo].[Inventories] ([ExpiryDate]);

CREATE NONCLUSTERED INDEX [IX_Orders_OrderCode] ON [dbo].[Orders] ([OrderCode]);
CREATE NONCLUSTERED INDEX [IX_Orders_CustomerId] ON [dbo].[Orders] ([CustomerId]);
CREATE NONCLUSTERED INDEX [IX_Orders_BranchId] ON [dbo].[Orders] ([BranchId]);
CREATE NONCLUSTERED INDEX [IX_Orders_StaffId] ON [dbo].[Orders] ([StaffId]);
CREATE NONCLUSTERED INDEX [IX_Orders_CreatedAt] ON [dbo].[Orders] ([CreatedAt]);
CREATE NONCLUSTERED INDEX [IX_Orders_OrderStatus] ON [dbo].[Orders] ([OrderStatus]);

CREATE NONCLUSTERED INDEX [IX_Payments_OrderId] ON [dbo].[Payments] ([OrderId]);
CREATE NONCLUSTERED INDEX [IX_Payments_TransactionCode] ON [dbo].[Payments] ([TransactionCode]);

CREATE NONCLUSTERED INDEX [IX_Carts_CustomerId] ON [dbo].[Carts] ([CustomerId]);

CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [dbo].[Notifications] ([UserId]);
CREATE NONCLUSTERED INDEX [IX_Notifications_IsRead] ON [dbo].[Notifications] ([IsRead]);
GO

-- ================================================================
-- SEED DATA MẪU KHỞI TẠO
-- ================================================================

DECLARE @AdminRoleId INT, @ManagerRoleId INT, @StaffRoleId INT, @CustomerRoleId INT;
SELECT @AdminRoleId = [Id] FROM [dbo].[Roles] WHERE [RoleName] = N'Admin';
SELECT @ManagerRoleId = [Id] FROM [dbo].[Roles] WHERE [RoleName] = N'Manager';
SELECT @StaffRoleId = [Id] FROM [dbo].[Roles] WHERE [RoleName] = N'Staff';
SELECT @CustomerRoleId = [Id] FROM [dbo].[Roles] WHERE [RoleName] = N'Customer';

-- Admin default account (admin / admin123)
INSERT INTO [dbo].[Users] ([RoleId], [FullName], [Email], [Phone], [Username], [PasswordHash], [IsActive])
VALUES (@AdminRoleId, N'FruitShop Administrator', 'admin@fruitshop.com', '0900000000', 'admin', 'admin123', 1);

-- Manager default account (manager1 / admin123)
INSERT INTO [dbo].[Users] ([RoleId], [FullName], [Email], [Phone], [Username], [PasswordHash], [IsActive])
VALUES (@ManagerRoleId, N'Nguyễn Văn Manager', 'manager1@fruitshop.com', '0911111111', 'manager1', 'admin123', 1);

DECLARE @ManagerId INT;
SELECT @ManagerId = SCOPE_IDENTITY();

-- Seed Branches
INSERT INTO [dbo].[Branches] ([ManagerId], [BranchName], [Address], [Phone], [IsActive])
VALUES 
    (@ManagerId, N'Chi nhánh 1 - Phố Huế, Hà Nội', N'123 Phố Huế, Q. Hai Bà Trưng, Hà Nội', '0243999888', 1),
    (@ManagerId, N'Chi nhánh 2 - Quận 3, TP.HCM', N'456 Nguyễn Thị Minh Khai, Q. 3, TP.HCM', '0283888999', 1);

DECLARE @Branch1Id INT;
SELECT @Branch1Id = [Id] FROM [dbo].[Branches] WHERE [BranchName] LIKE N'%Chi nhánh 1%';

-- Seed Staff (staff1 / admin123)
INSERT INTO [dbo].[Users] ([RoleId], [BranchId], [FullName], [Email], [Phone], [Username], [PasswordHash], [IsActive])
VALUES (@StaffRoleId, @Branch1Id, N'Trần Thị Staff', 'staff1@fruitshop.com', '0922222222', 'staff1', 'admin123', 1);

-- Seed Categories
INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive]) VALUES
(N'Hoa quả nhập khẩu', N'Các loại hoa quả nhập khẩu cao cấp chọn lọc từ Nhật, Mỹ, Úc, New Zealand', 1),
(N'Hoa quả trong nước', N'Các loại hoa quả tươi đặc sản thu hoạch từ vườn Việt Nam', 1),
(N'Combo hoa quả VIP', N'Các hộp quà và giỏ hoa quả thiết kế sang trọng', 1);

DECLARE @CatImported INT, @CatLocal INT, @CatCombo INT;
SELECT @CatImported = [Id] FROM [dbo].[Categories] WHERE [Name] = N'Hoa quả nhập khẩu';
SELECT @CatLocal = [Id] FROM [dbo].[Categories] WHERE [Name] = N'Hoa quả trong nước';
SELECT @CatCombo = [Id] FROM [dbo].[Categories] WHERE [Name] = N'Combo hoa quả VIP';

-- Seed Products
INSERT INTO [dbo].[Products] ([CategoryId], [Name], [Description], [Price], [StockQuantity], [Unit], [ImageUrl], [MinStockThreshold], [IsActive]) VALUES
(@CatImported, N'Nho Mẫu Đơn Shine Muscat', N'Độ ngọt Brix > 18, vỏ mỏng giòn ngọt thanh mát từ Nagano Nhật Bản', 1850000, 50, N'Hộp', 'https://images.unsplash.com/photo-1537640538966-79f369143f8f?q=80&w=800', 10, 1),
(@CatImported, N'Cherry Đỏ Size 32+ Premium', N'Thịt giòn mọng nước, cuống tươi nguyên vận chuyển đường hàng không từ Úc', 920000, 40, N'Kg', 'https://images.unsplash.com/photo-1528825871115-3581a5387919?q=80&w=800', 10, 1),
(@CatImported, N'Dâu Tây Tuyết Seolhyang', N'Hương thơm trái dâu chín mọng, vị ngọt thanh tự nhiên từ Hàn Quốc', 650000, 30, N'Hộp', 'https://images.unsplash.com/photo-1601004890684-d8cbf643f5f2?q=80&w=800', 5, 1),
(@CatImported, N'Cam Ruột Đỏ Cara Cara', N'Giàu Vitamin C, vị ngọt đậm không chua hạt mọng từ California Mỹ', 380000, 60, N'Kg', 'https://images.unsplash.com/photo-1557800636-894a64c1696f?q=80&w=800', 10, 1),
(@CatImported, N'Táo Envy Size 24 High-Grade', N'Thịt táo đậm đặc, thơm nức, độ giòn hoàn hảo từ New Zealand', 420000, 50, N'Kg', 'https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?q=80&w=800', 10, 1);

-- Seed Inventories
DECLARE @P1 INT, @P2 INT, @P3 INT, @P4 INT, @P5 INT;
SELECT @P1 = [Id] FROM [dbo].[Products] WHERE [Name] LIKE N'%Nho Mẫu Đơn%';
SELECT @P2 = [Id] FROM [dbo].[Products] WHERE [Name] LIKE N'%Cherry%';
SELECT @P3 = [Id] FROM [dbo].[Products] WHERE [Name] LIKE N'%Dâu Tây%';
SELECT @P4 = [Id] FROM [dbo].[Products] WHERE [Name] LIKE N'%Cam Ruột Đỏ%';
SELECT @P5 = [Id] FROM [dbo].[Products] WHERE [Name] LIKE N'%Táo Envy%';

INSERT INTO [dbo].[Inventories] ([ProductId], [BranchId], [BatchCode], [QuantityReceived], [RemainingQuantity], [ReceivedAt], [ExpiryDate], [UnitCost], [SellingPrice], [SupplierName], [Note]) VALUES
(@P1, @Branch1Id, N'BATCH-NHO-20260801', 50, 50, GETDATE(), DATEADD(day, 25, GETDATE()), 1200000, 1850000, N'Nagano Fruit Co.', N'Lô nhập hàng không nguyên thùng'),
(@P2, @Branch1Id, N'BATCH-CHE-20260801', 40, 40, GETDATE(), DATEADD(day, 20, GETDATE()), 600000, 920000, N'Tasmania Orchard', N'Hàng tươi nhập mới'),
(@P3, @Branch1Id, N'BATCH-DAU-20260801', 30, 30, GETDATE(), DATEADD(day, 15, GETDATE()), 400000, 650000, N'Seolhyang Farm', N'Dâu tuyết loại 1'),
(@P4, @Branch1Id, N'BATCH-CAM-20260801', 60, 60, GETDATE(), DATEADD(day, 30, GETDATE()), 220000, 380000, N'California Fruit Exporter', N'Cam Cara ngọt thanh'),
(@P5, @Branch1Id, N'BATCH-TAO-20260801', 50, 50, GETDATE(), DATEADD(day, 45, GETDATE()), 280000, 420000, N'Envy NZ Ltd', N'Táo giòn thơm');
GO

PRINT N'===================================================='
PRINT N'  KHỞI TẠO DATABASE FruitStoreDb HOÀN TẤT THÀNH CÔNG!'
PRINT N'  - Đã seed sẵn: 4 Roles, Admin, Manager, Staff, 2 Chi nhánh, 5 Sản phẩm & Lô kho.'
PRINT N'===================================================='
GO
