using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;

namespace FruitShop.Client;

public partial class OrdersWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<BranchDto> _branches = new();
    private int? _currentBranchId;
    private bool _isLoadingBranches = false;

    public OrdersWindow(int? branchId = null)
    {
        InitializeComponent();
        _currentBranchId = branchId;
        Loaded += async (_, _) =>
        {
            await LoadBranchesAsync();
            await LoadOrdersAsync();
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
                new BranchDto { Id = 0, BranchName = "Toàn bộ hệ thống (Tất cả chi nhánh)" }
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
                BranchBadgeTextBlock.Text = $"🏢 Chi nhánh: {selectedBranch.BranchName}";
            }
            else
            {
                _currentBranchId = null;
                BranchBadgeTextBlock.Text = "🏢 Chi nhánh: Toàn bộ hệ thống";
            }

            await LoadOrdersAsync();
        }
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            var response = await _clientService.GetOrdersAsync(_currentBranchId);
            if (response.Success)
            {
                OrdersDataGrid.ItemsSource = response.Items;
                var branchName = _currentBranchId.HasValue
                    ? _branches.FirstOrDefault(b => b.Id == _currentBranchId.Value)?.BranchName ?? "Chi nhánh"
                    : "Toàn bộ hệ thống";

                StatusTextBlock.Text = $"Đã tải {response.Items.Count} đơn hàng (Chi nhánh: {branchName}).";
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

    private async void MarkPaidButton_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersDataGrid.SelectedItem is OrderDto order)
        {
            if (order.PaymentStatus == "Paid")
            {
                MessageBox.Show("Đơn hàng này đã được xác nhận thanh toán trước đó.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Xác nhận thanh toán cho đơn hàng '{order.OrderCode}'?", "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _clientService.MarkOrderPaidAsync(order.Id);
                    if (response.Status == "SUCCESS")
                    {
                        MessageBox.Show(response.Message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadOrdersAsync();
                    }
                    else
                    {
                        MessageBox.Show(response.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xác nhận thanh toán: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Vui lòng chọn một đơn hàng từ danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadOrdersAsync();
}
