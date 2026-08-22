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
    private readonly int? _preselectedBranchId;

    public UpdateStockWindow(int? productId = null, int? branchId = null)
    {
        InitializeComponent();
        _preselectedProductId = productId;
        _preselectedBranchId = branchId;
        BatchCodeTextBox.Text = $"BATCH-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
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
                if (_branches.Count > 0)
                {
                    if (_preselectedBranchId.HasValue && _branches.Any(b => b.Id == _preselectedBranchId.Value))
                        BranchComboBox.SelectedValue = _preselectedBranchId.Value;
                    else
                        BranchComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Không thể tải dữ liệu: {ex.Message}";
        }
    }

    private void ProductComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProductComboBox.SelectedItem is ProductDto product)
        {
            SellingPriceTextBox.Text = product.Price.ToString("0.##");
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

        decimal? unitCost = null;
        if (!string.IsNullOrWhiteSpace(UnitCostTextBox.Text))
        {
            if (!decimal.TryParse(UnitCostTextBox.Text.Trim(), out var parsedCost) || parsedCost < 0)
            {
                StatusTextBlock.Text = "Giá nhập (vốn) không hợp lệ.";
                return;
            }
            unitCost = parsedCost;
        }

        decimal? sellingPrice = null;
        if (!string.IsNullOrWhiteSpace(SellingPriceTextBox.Text))
        {
            if (!decimal.TryParse(SellingPriceTextBox.Text.Trim(), out var parsedPrice) || parsedPrice <= 0)
            {
                StatusTextBlock.Text = "Giá bán ra phải là số dương hợp lệ.";
                return;
            }
            sellingPrice = parsedPrice;
        }

        if (unitCost.HasValue && sellingPrice.HasValue && sellingPrice.Value < unitCost.Value)
        {
            StatusTextBlock.Text = "Giá bán ra không được thấp hơn giá nhập (vốn). Vui lòng kiểm tra lại.";
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
            ExpiryDate = expiryDate,
            UnitCost = unitCost,
            SellingPrice = sellingPrice,
            SupplierName = string.IsNullOrWhiteSpace(SupplierNameTextBox.Text) ? null : SupplierNameTextBox.Text.Trim()
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