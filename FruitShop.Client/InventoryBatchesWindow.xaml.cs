using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class InventoryBatchesWindow : Window
{
    private readonly int _productId;
    private readonly int? _branchId;
    private readonly TcpClientService _client = new("127.0.0.1", 5055);

    public InventoryBatchesWindow(int productId, int? branchId = null)
    {
        InitializeComponent();
        _productId = productId;
        _branchId = branchId;
        Loaded += async (_, _) => await LoadBatchesAsync();
    }

    private async Task LoadBatchesAsync()
    {
        try
        {
            var response = await _client.GetInventoryAsync(_branchId);
            if (response.Success)
            {
                var batches = response.Items.Where(i => i.ProductId == _productId).ToList();
                BatchesDataGrid.ItemsSource = batches;
                var productName = batches.FirstOrDefault()?.ProductName;
                TitleTextBlock.Text = !string.IsNullOrEmpty(productName) ? $"Lịch sử nhập kho - {productName}" : "Lịch sử nhập kho";
                StatusTextBlock.Text = $"Tìm thấy {batches.Count} lô hàng cho sản phẩm này.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải lịch sử lô hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
