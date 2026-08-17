using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class ProductDetailWindow : Window
{
    private readonly int _productId;
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private ProductDto? _product;

    public ProductDetailWindow(int productId)
    {
        InitializeComponent();
        _productId = productId;
        Loaded += async (_, _) => await LoadDetailAsync();
    }

    private async Task LoadDetailAsync()
    {
        try
        {
            _product = await _clientService.GetProductDetailAsync(_productId);
            if (_product is null)
            {
                MessageBox.Show("Could not load product detail.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            ProductNameTextBlock.Text = _product.Name;
            CategoryTextBlock.Text = $"Category: {_product.CategoryName}";
            PriceTextBlock.Text = $"${_product.Price:F2} / {_product.Unit}";
            StockTextBlock.Text = $"{_product.StockQuantity}";
            DescriptionTextBlock.Text = string.IsNullOrWhiteSpace(_product.Description) ? "No description available." : _product.Description;
            MetadataTextBlock.Text = $"Product ID: #{_product.Id} | Created: {_product.CreatedAt:dd/MM/yyyy}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading product detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_product is null) return;
        var window = new EditProductWindow(_product) { Owner = this };
        if (window.ShowDialog() == true)
        {
            await LoadDetailAsync();
        }
    }

    private async void HideProductButton_Click(object sender, RoutedEventArgs e)
    {
        if (_product is null) return;
        var result = MessageBox.Show($"Are you sure you want to hide '{_product.Name}' from sale?", "Confirm Hide", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var response = await _clientService.HideProductAsync(_product.Id);
                if (response.Status == "SUCCESS")
                {
                    MessageBox.Show(response.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(response.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error hiding product: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
