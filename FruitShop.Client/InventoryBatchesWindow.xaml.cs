using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class InventoryBatchesWindow : Window
{
    private readonly int _productId;
    private readonly TcpClientService _client = new("127.0.0.1", 5055);

    public InventoryBatchesWindow(int productId)
    {
        InitializeComponent();
        _productId = productId;
        Loaded += async (_, _) => await LoadBatchesAsync();
    }

    private async Task LoadBatchesAsync()
    {
        try
        {
            var response = await _client.GetInventoryAsync();
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
            MessageBox.Show($"Could not load batches: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
