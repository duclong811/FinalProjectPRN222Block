using FruitShop.Server.Data;
using FruitShop.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Services;

public sealed class ReportService
{
    private readonly string _connectionString;

    public ReportService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<RevenueReportResponse> GetRevenueReportAsync(GetRevenueReportRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = FruitStoreDbContextFactory.Create(_connectionString);

        var query = db.Orders.AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .AsQueryable();

        if (request.BranchId.HasValue && request.BranchId.Value > 0)
        {
            query = query.Where(o => o.BranchId == request.BranchId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(o => o.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(o => o.CreatedAt < to);
        }

        var orders = await query.ToListAsync(cancellationToken);

        var completedOrders = orders
            .Where(o => o.OrderStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                     || o.OrderStatus.Equals("Complete", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var cancelledOrdersCount = orders
            .Count(o => o.OrderStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                     || o.OrderStatus.Equals("Cancel", StringComparison.OrdinalIgnoreCase));

        var totalRevenue = completedOrders.Sum(o => o.FinalAmount);
        var totalCompletedOrders = completedOrders.Count;
        var totalItemsSold = completedOrders.SelectMany(o => o.OrderDetails).Sum(d => d.Quantity);
        var averageOrderValue = totalCompletedOrders > 0 ? totalRevenue / totalCompletedOrders : 0;

        // 1. Phân rã doanh thu theo từng cơ sở (Branch Summaries)
        var allBranches = await db.Branches.AsNoTracking().Where(b => b.IsActive).ToListAsync(cancellationToken);
        var branchSummaries = new List<BranchRevenueSummaryDto>();

        if (request.BranchId.HasValue && request.BranchId.Value > 0)
        {
            var b = allBranches.FirstOrDefault(x => x.Id == request.BranchId.Value) 
                    ?? new Branch { Id = request.BranchId.Value, BranchName = $"Chi nhánh #{request.BranchId.Value}" };
            var bOrders = completedOrders.Where(o => o.BranchId == b.Id).ToList();
            var bRev = bOrders.Sum(o => o.FinalAmount);
            var bCount = bOrders.Count;
            branchSummaries.Add(new BranchRevenueSummaryDto
            {
                BranchId = b.Id,
                BranchName = b.BranchName,
                TotalCompletedOrders = bCount,
                TotalRevenue = bRev,
                RevenuePercentage = 100.0,
                AverageOrderValue = bCount > 0 ? bRev / bCount : 0
            });
        }
        else
        {
            foreach (var b in allBranches)
            {
                var bOrders = completedOrders.Where(o => o.BranchId == b.Id).ToList();
                var bRev = bOrders.Sum(o => o.FinalAmount);
                var bCount = bOrders.Count;
                branchSummaries.Add(new BranchRevenueSummaryDto
                {
                    BranchId = b.Id,
                    BranchName = b.BranchName,
                    TotalCompletedOrders = bCount,
                    TotalRevenue = bRev,
                    RevenuePercentage = totalRevenue > 0 ? Math.Round((double)(bRev / totalRevenue * 100), 2) : 0,
                    AverageOrderValue = bCount > 0 ? bRev / bCount : 0
                });
            }

            // Kiểm tra các đơn không gắn branchId nếu có
            var unassignedOrders = completedOrders.Where(o => !o.BranchId.HasValue || o.BranchId.Value == 0).ToList();
            if (unassignedOrders.Any())
            {
                var uRev = unassignedOrders.Sum(o => o.FinalAmount);
                var uCount = unassignedOrders.Count;
                branchSummaries.Add(new BranchRevenueSummaryDto
                {
                    BranchId = 0,
                    BranchName = "Đơn Online chưa gán chi nhánh",
                    TotalCompletedOrders = uCount,
                    TotalRevenue = uRev,
                    RevenuePercentage = totalRevenue > 0 ? Math.Round((double)(uRev / totalRevenue * 100), 2) : 0,
                    AverageOrderValue = uCount > 0 ? uRev / uCount : 0
                });
            }
        }

        // 2. Top sản phẩm bán chạy (Best Sellers) kèm note rõ số lượng theo từng cơ sở
        var allDetails = completedOrders
            .SelectMany(o => o.OrderDetails.Select(d => new { Detail = d, Order = o }))
            .ToList();

        var topSelling = allDetails
            .GroupBy(x => x.Detail.ProductId)
            .Select(g =>
            {
                var first = g.First();
                var pName = !string.IsNullOrWhiteSpace(first.Detail.ProductName) 
                    ? first.Detail.ProductName 
                    : (first.Detail.Product?.Name ?? $"Sản phẩm #{g.Key}");
                var catName = first.Detail.Product?.Category?.Name ?? "Trái cây";
                var unit = !string.IsNullOrWhiteSpace(first.Detail.Product?.Unit) 
                    ? first.Detail.Product.Unit 
                    : "kg";
                var qtySold = g.Sum(x => x.Detail.Quantity);
                var rev = g.Sum(x => x.Detail.SubTotal);

                // Note rõ số lượng bán ở từng cơ sở
                var branchGroup = g
                    .GroupBy(x => x.Order.Branch?.BranchName ?? "Trực tuyến/Khác")
                    .Select(bg => $"{bg.Key}: {bg.Sum(item => item.Detail.Quantity)} {unit}")
                    .ToList();

                var breakdownStr = string.Join(" | ", branchGroup);

                return new TopSellingProductDto
                {
                    ProductId = g.Key,
                    ProductName = pName,
                    CategoryName = catName,
                    Unit = unit,
                    TotalQuantitySold = qtySold,
                    TotalRevenue = rev,
                    AveragePrice = qtySold > 0 ? rev / qtySold : 0,
                    BranchBreakdown = breakdownStr
                };
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .ThenByDescending(p => p.TotalRevenue)
            .ToList();

        // Gán thứ hạng Rank
        for (int i = 0; i < topSelling.Count; i++)
        {
            topSelling[i].Rank = i + 1;
        }

        return new RevenueReportResponse
        {
            Success = true,
            TotalRevenue = totalRevenue,
            TotalCompletedOrders = totalCompletedOrders,
            TotalCancelledOrders = cancelledOrdersCount,
            TotalItemsSold = totalItemsSold,
            AverageOrderValue = averageOrderValue,
            BranchSummaries = branchSummaries.OrderByDescending(b => b.TotalRevenue).ToList(),
            TopSellingProducts = topSelling
        };
    }
}
