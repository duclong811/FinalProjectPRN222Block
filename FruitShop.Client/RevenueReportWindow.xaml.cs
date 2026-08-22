using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FruitShop.Client;

public partial class RevenueReportWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<BranchDto> _branches = new();
    private List<TopSellingProductDto> _allBestSellers = new();
    private int? _currentBranchId;
    private bool _isLoading = false;

    public RevenueReportWindow(int? initialBranchId = null)
    {
        InitializeComponent();
        _currentBranchId = initialBranchId;

        // Mặc định chọn kỳ tháng này
        if (TimePeriodComboBox != null && TimePeriodComboBox.Items.Count > 0)
        {
            TimePeriodComboBox.SelectedIndex = 0;
        }
        SetDateRangePreset("THIS_MONTH");

        Loaded += async (_, _) =>
        {
            await LoadBranchesAsync();
            await LoadReportAsync();
        };
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            _isLoading = true;
            var response = await _clientService.GetBranchesAsync();
            var branchList = new List<BranchDto>
            {
                new BranchDto { Id = 0, BranchName = "Toàn bộ hệ thống (Tất cả cơ sở)" }
            };

            if (response.Success && response.Items.Count > 0)
            {
                branchList.AddRange(response.Items);
            }

            _branches = branchList;
            BranchFilterComboBox.ItemsSource = _branches;

            if (_currentBranchId.HasValue && _currentBranchId.Value > 0 && _branches.Any(b => b.Id == _currentBranchId.Value))
            {
                BranchFilterComboBox.SelectedValue = _currentBranchId.Value;
            }
            else
            {
                BranchFilterComboBox.SelectedIndex = 0;
                _currentBranchId = null;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi tải chi nhánh: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadReportAsync()
    {
        if (_isLoading) return;

        try
        {
            StatusTextBlock.Text = "Đang tổng hợp dữ liệu doanh thu...";
            var selectedBranchId = _currentBranchId;

            var request = new GetRevenueReportRequest
            {
                BranchId = selectedBranchId,
                FromDate = FromDatePicker.SelectedDate,
                ToDate = ToDatePicker.SelectedDate
            };

            var response = await _clientService.GetRevenueReportAsync(request);

            if (response.Success)
            {
                // Cập nhật các KPI Cards
                if (TotalRevenueTextBlock != null) TotalRevenueTextBlock.Text = $"{response.TotalRevenue:N0} đ";
                if (TotalOrdersTextBlock != null) TotalOrdersTextBlock.Text = $"{response.TotalCompletedOrders:N0} đơn";
                if (TotalItemsSoldTextBlock != null) TotalItemsSoldTextBlock.Text = $"{response.TotalItemsSold:N0}";
                if (AverageOrderValueTextBlock != null) AverageOrderValueTextBlock.Text = $"{response.AverageOrderValue:N0} đ";
                if (CancelledOrdersTextBlock != null) CancelledOrdersTextBlock.Text = $"{response.TotalCancelledOrders:N0} đơn";

                // Cập nhật Danh sách Top bán chạy
                _allBestSellers = response.TopSellingProducts ?? new();
                FilterBestSellers();

                // Cập nhật Bảng doanh thu theo cơ sở
                if (BranchBreakdownDataGrid != null) BranchBreakdownDataGrid.ItemsSource = response.BranchSummaries ?? new();

                var branchName = _currentBranchId.HasValue && _currentBranchId.Value > 0
                    ? _branches.FirstOrDefault(b => b.Id == _currentBranchId.Value)?.BranchName ?? $"Cơ sở #{_currentBranchId.Value}"
                    : "Toàn bộ hệ thống";

                if (FilterSummaryTextBlock != null) FilterSummaryTextBlock.Text = $"Số liệu từ {FromDatePicker.SelectedDate:dd/MM/yyyy} đến {ToDatePicker.SelectedDate:dd/MM/yyyy} ({branchName})";
                if (StatusTextBlock != null) StatusTextBlock.Text = $"Tổng hợp thành công lúc {DateTime.Now:HH:mm:ss} ({_allBestSellers.Count} mặt hàng, {response.TotalCompletedOrders} đơn hoàn tất).";
            }
            else
            {
                if (StatusTextBlock != null) StatusTextBlock.Text = $"Lỗi: {response.Message}";
                MessageBox.Show(response.Message, "Thông báo lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            if (StatusTextBlock != null) StatusTextBlock.Text = $"Lỗi kết nối máy chủ: {ex.Message}";
            MessageBox.Show($"Lỗi kết nối máy chủ TCP: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterBestSellers()
    {
        if (_allBestSellers == null || BestSellersDataGrid == null || ProductSearchTextBox == null) return;

        var keyword = ProductSearchTextBox.Text?.Trim() ?? string.Empty;
        if (keyword.Equals("🔍 Tìm sản phẩm...", StringComparison.OrdinalIgnoreCase))
        {
            keyword = string.Empty;
        }

        if (string.IsNullOrEmpty(keyword))
        {
            BestSellersDataGrid.ItemsSource = _allBestSellers;
        }
        else
        {
            BestSellersDataGrid.ItemsSource = _allBestSellers
                .Where(p => p.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                         || p.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                         || p.BranchBreakdown.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void SetDateRangePreset(string tag)
    {
        if (FromDatePicker == null || ToDatePicker == null) return;

        var now = DateTime.Today;
        switch (tag)
        {
            case "TODAY":
                FromDatePicker.SelectedDate = now;
                ToDatePicker.SelectedDate = now;
                break;

            case "LAST_7_DAYS":
                FromDatePicker.SelectedDate = now.AddDays(-6);
                ToDatePicker.SelectedDate = now;
                break;

            case "THIS_MONTH":
                FromDatePicker.SelectedDate = new DateTime(now.Year, now.Month, 1);
                ToDatePicker.SelectedDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                break;

            case "LAST_MONTH":
                var lastMonth = now.AddMonths(-1);
                FromDatePicker.SelectedDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                ToDatePicker.SelectedDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                break;

            case "ALL_TIME":
                FromDatePicker.SelectedDate = new DateTime(2020, 1, 1);
                ToDatePicker.SelectedDate = now.AddYears(1);
                break;

            case "CUSTOM":
                // Giữ nguyên DatePicker
                break;
        }
    }

    private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || BranchFilterComboBox == null) return;

        if (BranchFilterComboBox.SelectedItem is BranchDto selected)
        {
            _currentBranchId = selected.Id > 0 ? selected.Id : null;
            await LoadReportAsync();
        }
    }

    private async void TimePeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TimePeriodComboBox == null) return;

        if (TimePeriodComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            SetDateRangePreset(tag);
            if (tag != "CUSTOM" && !_isLoading)
            {
                await LoadReportAsync();
            }
        }
    }

    private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        // Khi người dùng chọn lại ngày trên DatePicker
        if (TimePeriodComboBox != null && TimePeriodComboBox.SelectedItem is ComboBoxItem item && item.Tag?.ToString() != "CUSTOM")
        {
            // Người dùng chủ động thay đổi ngày tùy chọn
        }
    }

    private async void RefreshReportButton_Click(object sender, RoutedEventArgs e) => await LoadReportAsync();

    private void ProductSearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => FilterBestSellers();

    private void ProductSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (ProductSearchTextBox.Text == "🔍 Tìm sản phẩm...")
        {
            ProductSearchTextBox.Text = string.Empty;
            ProductSearchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55));
        }
    }

    private void ProductSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProductSearchTextBox.Text))
        {
            ProductSearchTextBox.Text = "🔍 Tìm sản phẩm...";
            ProductSearchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
