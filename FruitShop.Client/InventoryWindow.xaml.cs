using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FruitShop.Client;

public partial class InventoryWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<BranchDto> _branches = new();
    private int? _currentBranchId;
    private bool _isLoadingBranches = false;

    public InventoryWindow(int? branchId = null)
    {
        InitializeComponent();
        _currentBranchId = branchId;
        Loaded += async (_, _) =>
        {
            await LoadBranchesAsync();
            await LoadInventoryAsync();
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
                SubtitleTextBlock.Text = $"Hiển thị tồn kho của chi nhánh: {selectedBranch.BranchName}";
            }
            else
            {
                _currentBranchId = null;
                BranchBadgeTextBlock.Text = "🏢 Kho Chi Nhánh: Toàn bộ hệ thống";
                SubtitleTextBlock.Text = "Chọn chi nhánh và double-click sản phẩm để xem hoặc nhập lô hàng mới.";
            }

            await LoadInventoryAsync();
        }
    }

    private async Task LoadInventoryAsync()
    {
        try
        {
            var response = await _clientService.GetProductsAsync(_currentBranchId);
            if (response.Success)
            {
                InventoryDataGrid.ItemsSource = response.Items;
                var branchName = _currentBranchId.HasValue
                    ? _branches.FirstOrDefault(b => b.Id == _currentBranchId.Value)?.BranchName ?? "Chi nhánh"
                    : "Toàn bộ hệ thống";
                StatusTextBlock.Text = $"Đã tải {response.Items.Count} sản phẩm (Kho: {branchName}).";
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

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadInventoryAsync();

    private void ViewBatchesButton_Click(object sender, RoutedEventArgs e)
    {
        if (InventoryDataGrid.SelectedItem is ProductDto item)
        {
            new InventoryBatchesWindow(item.Id, _currentBranchId) { Owner = this }.ShowDialog();
        }
        else
        {
            MessageBox.Show("Vui lòng chọn một sản phẩm để xem lịch sử lô hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UpdateStockButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedProduct = InventoryDataGrid.SelectedItem as ProductDto;
        var window = new UpdateStockWindow(selectedProduct?.Id, _currentBranchId) { Owner = this };
        if (window.ShowDialog() == true)
        {
            _ = LoadInventoryAsync();
        }
    }

    private void InventoryDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (InventoryDataGrid.SelectedItem is ProductDto item)
        {
            new InventoryBatchesWindow(item.Id, _currentBranchId) { Owner = this }.ShowDialog();
        }
    }
}
