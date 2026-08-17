using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class ProductManagementService
{
    private const int MaximumImageBytes = 5 * 1024 * 1024;
    private readonly string _connectionString;
    private readonly string _webImageRoot;

    public ProductManagementService(string connectionString, string webImageRoot)
    {
        _connectionString = connectionString;
        _webImageRoot = webImageRoot;
    }

    public async Task<CategoryListResponse> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var items = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
        return new CategoryListResponse { Success = true, Items = items };
    }

    public async Task<ProductListResponse> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var items = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                MinStockThreshold = p.MinStockThreshold,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return new ProductListResponse { Success = true, Items = items };
    }

    public async Task<WebProductPagedResponse> GetPagedProductsForWebAsync(GetProductsPagedRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var today = DateTime.Today;
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        var query = db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .Where(p => p.IsActive && p.Inventories.Any(inv => inv.RemainingQuantity > 0 && inv.ExpiryDate >= today));

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        query = request.PriceRange switch
        {
            "under-500" => query.Where(p => p.Price < 500000),
            "500-to-1500" => query.Where(p => p.Price >= 500000 && p.Price <= 1500000),
            "over-1500" => query.Where(p => p.Price > 1500000),
            _ => query
        };

        query = request.Sort switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 24);
        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 0;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var rawProducts = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var webProducts = new List<WebProductDto>();

        foreach (var p in rawProducts)
        {
            var validBatches = p.Inventories
                .Where(inv => inv.RemainingQuantity > 0 && inv.ExpiryDate >= today)
                .OrderBy(inv => inv.ExpiryDate)
                .ThenBy(inv => inv.ReceivedAt)
                .ThenBy(inv => inv.Id)
                .Select(inv => new WebProductSaleBatchDto
                {
                    InventoryId = inv.Id,
                    BatchCode = inv.BatchCode,
                    RemainingQuantity = inv.RemainingQuantity,
                    ExpiryDate = inv.ExpiryDate,
                    HasExpiryDiscount = (inv.ExpiryDate.Date - today).Days <= 2,
                    SalePrice = (inv.ExpiryDate.Date - today).Days <= 2 ? p.Price * 0.5m : p.Price
                })
                .ToList();

            var firstBatch = validBatches.FirstOrDefault();
            if (firstBatch is null) continue;

            webProducts.Add(new WebProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SalePrice = firstBatch.SalePrice,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                HasExpiryDiscount = firstBatch.HasExpiryDiscount,
                SaleBatchAvailableQuantity = firstBatch.RemainingQuantity,
                SaleBatchExpiryDate = firstBatch.ExpiryDate,
                CreatedAt = p.CreatedAt,
                SaleBatches = validBatches
            });
        }

        return new WebProductPagedResponse
        {
            Success = true,
            Items = webProducts,
            Categories = categories,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<WebProductDto?> GetWebProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var today = DateTime.Today;
        var p = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);

        if (p is null) return null;

        var validBatches = p.Inventories
            .Where(inv => inv.RemainingQuantity > 0 && inv.ExpiryDate >= today)
            .OrderBy(inv => inv.ExpiryDate)
            .ThenBy(inv => inv.ReceivedAt)
            .ThenBy(inv => inv.Id)
            .Select(inv => new WebProductSaleBatchDto
            {
                InventoryId = inv.Id,
                BatchCode = inv.BatchCode,
                RemainingQuantity = inv.RemainingQuantity,
                ExpiryDate = inv.ExpiryDate,
                HasExpiryDiscount = (inv.ExpiryDate.Date - today).Days <= 2,
                SalePrice = (inv.ExpiryDate.Date - today).Days <= 2 ? p.Price * 0.5m : p.Price
            })
            .ToList();

        var firstBatch = validBatches.FirstOrDefault();
        if (firstBatch is null) return null;

        return new WebProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            SalePrice = firstBatch.SalePrice,
            StockQuantity = p.StockQuantity,
            Unit = p.Unit,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            HasExpiryDiscount = firstBatch.HasExpiryDiscount,
            SaleBatchAvailableQuantity = firstBatch.RemainingQuantity,
            SaleBatchExpiryDate = firstBatch.ExpiryDate,
            CreatedAt = p.CreatedAt,
            SaleBatches = validBatches
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var p = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);

        if (p is null) return null;
        return new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            Unit = p.Unit,
            ImageUrl = p.ImageUrl,
            MinStockThreshold = p.MinStockThreshold,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }

    public async Task<ProductListResponse> SearchProductsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        keyword = keyword?.Trim() ?? string.Empty;
        var items = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Name.Contains(keyword))
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                MinStockThreshold = p.MinStockThreshold,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return new ProductListResponse { Success = true, Items = items };
    }

    public async Task<TcpResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsValid(request.CategoryId, request.Name, request.Price, request.Unit))
            return new TcpResponse { Status = "ERROR", Message = "Dữ liệu sản phẩm không hợp lệ." };

        var imageError = DecodeImage(request.ImageBase64, request.ImageFileName, out var imageBytes, out var extension);
        if (imageError is not null)
            return new TcpResponse { Status = "ERROR", Message = imageError };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken))
            return new TcpResponse { Status = "ERROR", Message = "Danh mục được chọn không tồn tại." };

        string? storedFilePath = null;
        try
        {
            string? imageUrl = null;
            if (imageBytes is not null)
            {
                Directory.CreateDirectory(_webImageRoot);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                storedFilePath = Path.Combine(_webImageRoot, fileName);
                await File.WriteAllBytesAsync(storedFilePath, imageBytes, cancellationToken);
                imageUrl = $"/images/products/{fileName}";
            }

            var product = new Product
            {
                CategoryId = request.CategoryId,
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Price = request.Price,
                StockQuantity = 0,
                Unit = request.Unit.Trim(),
                ImageUrl = imageUrl,
                MinStockThreshold = request.MinStockThreshold > 0 ? request.MinStockThreshold : 10,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(cancellationToken);

            return new TcpResponse { Status = "SUCCESS", Message = "Tạo sản phẩm mới thành công." };
        }
        catch (Exception ex)
        {
            if (storedFilePath is not null && File.Exists(storedFilePath)) File.Delete(storedFilePath);
            return new TcpResponse { Status = "ERROR", Message = $"Lỗi hệ thống: {ex.Message}" };
        }
    }

    public async Task<TcpResponse> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsValid(request.CategoryId, request.Name, request.Price, request.Unit) || request.ProductId <= 0)
            return new TcpResponse { Status = "ERROR", Message = "Dữ liệu sản phẩm không hợp lệ." };

        byte[]? imageBytes = null;
        string? extension = null;
        var imageError = request.ReplaceImage ? DecodeImage(request.ImageBase64, request.ImageFileName, out imageBytes, out extension) : null;
        if (imageError is not null)
            return new TcpResponse { Status = "ERROR", Message = imageError };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
            return new TcpResponse { Status = "ERROR", Message = "Không tìm thấy sản phẩm." };

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken))
            return new TcpResponse { Status = "ERROR", Message = "Danh mục không tồn tại." };

        var oldImageUrl = product.ImageUrl;
        string? newImagePath = null;
        try
        {
            var imageUrl = request.ReplaceImage ? null : oldImageUrl;
            if (imageBytes is not null)
            {
                Directory.CreateDirectory(_webImageRoot);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                newImagePath = Path.Combine(_webImageRoot, fileName);
                await File.WriteAllBytesAsync(newImagePath, imageBytes, cancellationToken);
                imageUrl = $"/images/products/{fileName}";
            }

            product.CategoryId = request.CategoryId;
            product.Name = request.Name.Trim();
            product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            product.Price = request.Price;
            product.Unit = request.Unit.Trim();
            product.ImageUrl = imageUrl;
            product.MinStockThreshold = request.MinStockThreshold;
            product.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync(cancellationToken);

            if (request.ReplaceImage && !string.IsNullOrWhiteSpace(oldImageUrl))
            {
                var oldFile = Path.Combine(_webImageRoot, Path.GetFileName(oldImageUrl));
                if (File.Exists(oldFile)) File.Delete(oldFile);
            }

            return new TcpResponse { Status = "SUCCESS", Message = "Cập nhật sản phẩm thành công." };
        }
        catch (Exception ex)
        {
            if (newImagePath is not null && File.Exists(newImagePath)) File.Delete(newImagePath);
            return new TcpResponse { Status = "ERROR", Message = $"Lỗi hệ thống: {ex.Message}" };
        }
    }

    public async Task<TcpResponse> HideAsync(int productId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0) return new TcpResponse { Status = "ERROR", Message = "Id sản phẩm không hợp lệ." };

        await using var db = FruitStoreDbContextFactory.Create(_connectionString);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);
        if (product is null) return new TcpResponse { Status = "ERROR", Message = "Sản phẩm không tồn tại hoặc đã ẩn." };

        product.IsActive = false;
        product.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);

        return new TcpResponse { Status = "SUCCESS", Message = "Đã ẩn sản phẩm khỏi hệ thống." };
    }

    private static bool IsValid(int categoryId, string? name, decimal price, string? unit) =>
        categoryId > 0 && !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 200 && price >= 0 && !string.IsNullOrWhiteSpace(unit) && unit.Trim().Length <= 50;

    private static string? DecodeImage(string? base64, string? fileName, out byte[]? bytes, out string? extension)
    {
        bytes = null; extension = null;
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try { bytes = Convert.FromBase64String(base64); } catch (FormatException) { return "Dữ liệu hình ảnh không hợp lệ."; }
        if (bytes.Length == 0 || bytes.Length > MaximumImageBytes) return "Kích thước ảnh phải từ 1 byte đến 5 MB.";
        extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".webp" ? null : "Chỉ hỗ trợ ảnh định dạng JPG, JPEG, PNG, WEBP.";
    }
}
