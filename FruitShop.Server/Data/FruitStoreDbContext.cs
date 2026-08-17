using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Data;

internal sealed class FruitStoreDbContext : DbContext
{
    public FruitStoreDbContext(DbContextOptions<FruitStoreDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Username).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.Avatar).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.HasOne(e => e.Role).WithMany(e => e.Users).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Branch).WithMany(e => e.StaffMembers).HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BranchName).HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false);
            entity.HasOne(e => e.Manager).WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500).IsUnicode(false);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500).IsUnicode(false);
            entity.HasOne(e => e.Category).WithMany(e => e.Products).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventories", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchCode).HasMaxLength(50);
            entity.Property(e => e.ExpiryDate).HasColumnType("date");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SupplierName).HasMaxLength(150);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasOne(e => e.Product).WithMany(e => e.Inventories).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Branch).WithMany(e => e.Inventories).HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", "dbo");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderCode).IsUnique();
            entity.Property(e => e.OrderCode).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.CustomerPhone).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.CustomerEmail).HasMaxLength(150).IsUnicode(false);
            entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FinalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderStatus).HasMaxLength(30).IsUnicode(false);
            entity.HasOne(e => e.Customer).WithMany(e => e.Orders).HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Branch).WithMany(e => e.Orders).HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Staff).WithMany().HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(29, 2)").HasComputedColumnSql("([UnitPrice]*[Quantity]*(1-[DiscountPercent]/100))", true);
            entity.HasOne(e => e.Order).WithMany(e => e.OrderDetails).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany(e => e.OrderDetails).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Batch).WithMany().HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentMethod).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.PaymentStatus).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TransactionCode).HasMaxLength(150).IsUnicode(false);
            entity.Property(e => e.PaymentUrl).HasMaxLength(1000).IsUnicode(false);
            entity.HasOne(e => e.Order).WithMany(e => e.Payments).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts", "dbo");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Customer).WithMany(e => e.CartItems).HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(30).IsUnicode(false);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

internal sealed class Role
{
    public int Id { get; set; }
    public string RoleName { get; set; } = null!;
    public ICollection<User> Users { get; } = new List<User>();
}

internal sealed class User
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int? BranchId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Avatar { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public Role Role { get; set; } = null!;
    public Branch? Branch { get; set; }
    public ICollection<Order> Orders { get; } = new List<Order>();
    public ICollection<Cart> CartItems { get; } = new List<Cart>();
}

internal sealed class Branch
{
    public int Id { get; set; }
    public int ManagerId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public User Manager { get; set; } = null!;
    public ICollection<User> StaffMembers { get; } = new List<User>();
    public ICollection<Inventory> Inventories { get; } = new List<Inventory>();
    public ICollection<Order> Orders { get; } = new List<Order>();
}

internal sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<Product> Products { get; } = new List<Product>();
}

internal sealed class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public int MinStockThreshold { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<Inventory> Inventories { get; } = new List<Inventory>();
    public ICollection<OrderDetail> OrderDetails { get; } = new List<OrderDetail>();
}

internal sealed class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public string BatchCode { get; set; } = null!;
    public int QuantityReceived { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public DateTime ExpiryDate { get; set; }
    public decimal? UnitCost { get; set; }
    public string? SupplierName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Product Product { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}

internal sealed class Order
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = null!;
    public int? CustomerId { get; set; }
    public int? BranchId { get; set; }
    public int? StaffId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string? CustomerEmail { get; set; }
    public string ShippingAddress { get; set; } = null!;
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string OrderStatus { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public User? Customer { get; set; }
    public Branch? Branch { get; set; }
    public User? Staff { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; } = new List<OrderDetail>();
    public ICollection<Payment> Payments { get; } = new List<Payment>();
}

internal sealed class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int? BatchId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal SubTotal { get; set; }
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Inventory? Batch { get; set; }
}

internal sealed class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string PaymentStatus { get; set; } = "Pending";
    public decimal Amount { get; set; }
    public string? TransactionCode { get; set; }
    public string? PaymentUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public Order Order { get; set; } = null!;
}

internal sealed class Cart
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
    public User Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}

internal sealed class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? BranchId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public User User { get; set; } = null!;
    public Branch? Branch { get; set; }
}
