using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class ExpiryManagementWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<BranchDto> _branches = new();
    private int? _currentBranchId;
    private bool _isLoadingBranches = false;

    public ExpiryManagementWindow(int? branchId = null)
    {
        InitializeComponent();
        _currentBranchId = branchId;
        Loaded += async (_, _) =>
        {
            await LoadBranchesAsync();
            await LoadExpiryDataAsync();
        };
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            _isLoadingBranches = true;
            var response = await _clientService.GetBranchesAsync();
            var branchList = new List<BranchDto>
            {
                new BranchDto { Id = 0, BranchName = "Toàn bộ hệ thống (Tất cả kho)" }
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
            _isLoadingBranches = false;
        }
    }

    private async void BranchFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingBranches) return;

        if (BranchFilterComboBox.SelectedItem is BranchDto selectedBranch)
        {
            if (selectedBranch.Id > 0)
            {
                _currentBranchId = selectedBranch.Id;
                BranchBadgeTextBlock.Text = $"🏢 Kho Chi Nhánh: {selectedBranch.BranchName}";
            }
            else
            {
                _currentBranchId = null;
                BranchBadgeTextBlock.Text = "🏢 Kho Chi Nhánh: Toàn bộ hệ thống";
            }

            await LoadExpiryDataAsync();
        }
    }

    private async Task LoadExpiryDataAsync()
    {
        try
        {
            var response = await _clientService.GetInventoryAsync(_currentBranchId);
            if (response.Success)
            {
                var today = DateTime.Today;
                var nearExpiry = response.Items
                    .Where(i => i.ExpiryDate.Date >= today && i.ExpiryDate.Date <= today.AddDays(2) && i.RemainingQuantity > 0)
                    .OrderBy(i => i.ExpiryDate)
                    .ToList();

                var expired = response.Items
                    .Where(i => i.ExpiryDate.Date < today && i.RemainingQuantity > 0)
                    .OrderBy(i => i.ExpiryDate)
                    .ToList();

                ExpiringSoonDataGrid.ItemsSource = nearExpiry;
                ExpiredDataGrid.ItemsSource = expired;

                var branchName = _currentBranchId.HasValue
                    ? _branches.FirstOrDefault(b => b.Id == _currentBranchId.Value)?.BranchName ?? "Chi nhánh"
                    : "Toàn bộ hệ thống";

                StatusTextBlock.Text = $"[Kho: {branchName}] Sắp hết hạn (0-2 ngày): {nearExpiry.Count} lô | Đã hết hạn: {expired.Count} lô";
            }
            else
            {
                StatusTextBlock.Text = response.Message;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void HideSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        InventoryDto? selected = ExpiringSoonDataGrid.SelectedItem as InventoryDto ?? ExpiredDataGrid.SelectedItem as InventoryDto;
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn một lô hàng từ danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Bạn có chắc chắn muốn ẩn sản phẩm '{selected.ProductName}' khỏi hệ thống bán hàng?", "Xác nhận ẩn", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            var res = await _clientService.HideProductAsync(selected.ProductId);
            MessageBox.Show(res.Message, res.Status == "SUCCESS" ? "Thành công" : "Lỗi", MessageBoxButton.OK, res.Status == "SUCCESS" ? MessageBoxImage.Information : MessageBoxImage.Error);
            await LoadExpiryDataAsync();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadExpiryDataAsync();

    private void ExpiringSoonDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void ExpiredDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
}
