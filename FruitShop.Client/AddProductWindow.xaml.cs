using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace FruitShop.Client;

public partial class AddProductWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private string? _selectedImagePath;
    private List<CategoryDto> _categories = new();

    public AddProductWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var response = await _clientService.GetCategoriesAsync();
            if (response.Success)
            {
                _categories = response.Items;
                CategoryComboBox.ItemsSource = _categories;
                if (_categories.Count > 0) CategoryComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Không thể tải danh mục: {ex.Message}";
        }
    }

    private void ChooseImageButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            _selectedImagePath = openFileDialog.FileName;
            ImagePathTextBox.Text = Path.GetFileName(_selectedImagePath);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (CategoryComboBox.SelectedItem is not CategoryDto selectedCategory)
        {
            StatusTextBlock.Text = "Vui lòng chọn danh mục.";
            return;
        }

        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusTextBlock.Text = "Tên sản phẩm là bắt buộc.";
            return;
        }

        if (!decimal.TryParse(PriceTextBox.Text.Trim(), out var price) || price < 0)
        {
            StatusTextBlock.Text = "Vui lòng nhập giá hợp lệ.";
            return;
        }

        var unit = UnitTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(unit))
        {
            StatusTextBlock.Text = "Đơn vị tính là bắt buộc.";
            return;
        }

        string? imageBase64 = null;
        string? imageFileName = null;
        if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
        {
            var imageBytes = await File.ReadAllBytesAsync(_selectedImagePath);
            imageBase64 = Convert.ToBase64String(imageBytes);
            imageFileName = Path.GetFileName(_selectedImagePath);
        }

        var request = new CreateProductRequest
        {
            CategoryId = selectedCategory.Id,
            Name = name,
            Description = DescriptionTextBox.Text.Trim(),
            Price = price,
            Unit = unit,
            ImageBase64 = imageBase64,
            ImageFileName = imageFileName
        };

        SaveButton.IsEnabled = false;
        try
        {
            var response = await _clientService.CreateProductAsync(request);
            if (response.Status == "SUCCESS")
            {
                var askReceive = MessageBox.Show($"{response.Message}\n\nBạn có muốn nhập lô kho đầu tiên cho sản phẩm này ngay không (để hiển thị lên Web)?", "Nhập Kho Ngay", MessageBoxButton.YesNo, MessageBoxImage.Question);
                DialogResult = true;
                Close();

                if (askReceive == MessageBoxResult.Yes)
                {
                    new UpdateStockWindow().ShowDialog();
                }
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