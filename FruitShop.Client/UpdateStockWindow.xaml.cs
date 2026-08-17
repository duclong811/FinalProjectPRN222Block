using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class UpdateStockWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<ProductDto> _products = new();
    private List<BranchDto> _branches = new();
    private readonly int? _preselectedProductId;

    public UpdateStockWindow(int? productId = null)
    {
        InitializeComponent();
        _preselectedProductId = productId;
        ExpiryDatePicker.SelectedDate = DateTime.Today.AddDays(30);
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var productRes = await _clientService.GetProductsAsync();
            if (productRes.Success)
            {
                _products = productRes.Items;
                ProductComboBox.ItemsSource = _products;
                if (_products.Count > 0)
                {
                    if (_preselectedProductId.HasValue)
                        ProductComboBox.SelectedValue = _preselectedProductId.Value;
                    else
                        ProductComboBox.SelectedIndex = 0;
                }
            }

            var branchRes = await _clientService.GetBranchesAsync();
            if (branchRes.Success)
            {
                _branches = branchRes.Items;
                BranchComboBox.ItemsSource = _branches;
                if (_branches.Count > 0) BranchComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Không thể tải dữ liệu: {ex.Message}";
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductComboBox.SelectedValue is not int productId || productId <= 0)
        {
            StatusTextBlock.Text = "Vui lòng chọn sản phẩm.";
            return;
        }

        var branchId = BranchComboBox.SelectedValue is int bId && bId > 0 ? bId : 1;

        var batchCode = BatchCodeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(batchCode))
        {
            StatusTextBlock.Text = "Mã lô hàng là bắt buộc.";
            return;
        }

        if (!int.TryParse(StockTextBox.Text.Trim(), out var quantity) || quantity <= 0)
        {
            StatusTextBlock.Text = "Vui lòng nhập số lượng hợp lệ.";
            return;
        }

        if (ExpiryDatePicker.SelectedDate is not DateTime expiryDate)
        {
            StatusTextBlock.Text = "Vui lòng chọn hạn sử dụng.";
            return;
        }

        var request = new ReceiveInventoryRequest
        {
            ProductId = productId,
            BranchId = branchId,
            BatchCode = batchCode,
            Quantity = quantity,
            ExpiryDate = expiryDate
        };

        SaveButton.IsEnabled = false;
        try
        {
            var response = await _clientService.ReceiveInventoryAsync(request);
            if (response.Status == "SUCCESS")
            {
                MessageBox.Show(response.Message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
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
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}