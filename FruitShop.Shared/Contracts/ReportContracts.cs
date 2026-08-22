namespace FruitShop.Shared.Contracts;

public class GetRevenueReportRequest
{
    public int? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class BranchRevenueSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int TotalCompletedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public double RevenuePercentage { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class TopSellingProductDto
{
    public int Rank { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public string BranchBreakdown { get; set; } = string.Empty;
}

public class RevenueReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Tổng quan KPIs
    public decimal TotalRevenue { get; set; }
    public int TotalCompletedOrders { get; set; }
    public int TotalCancelledOrders { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal AverageOrderValue { get; set; }

    // Chi tiết theo cơ sở & sản phẩm
    public List<BranchRevenueSummaryDto> BranchSummaries { get; set; } = new();
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
}
