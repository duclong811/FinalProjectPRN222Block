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
    private List<BranchDto> _branches = new();
    private readonly int? _preselectedBranchId;

    public AddProductWindow(int? preselectedBranchId = null)
    {
        InitializeComponent();
        _preselectedBranchId = preselectedBranchId;
        ExpiryDatePicker.SelectedDate = DateTime.Today.AddDays(30);

        Loaded += async (_, _) =>
        {
            await LoadBranchesAsync();
            await LoadCategoriesAsync();
        };
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var response = await _clientService.GetBranchesAsync();
            if (response.Success && response.Items.Count > 0)
            {
                _branches = response.Items;
                BranchComboBox.ItemsSource = _branches;

                if (_preselectedBranchId.HasValue && _preselectedBranchId.Value > 0 && _branches.Any(b => b.Id == _preselectedBranchId.Value))
                {
                    BranchComboBox.SelectedValue = _preselectedBranchId.Value;
                }
                else
                {
                    BranchComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Không thể tải danh sách chi nhánh: {ex.Message}";
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var response = await _clientService.GetCategoriesAsync();
            if (response.Success && response.Items.Count > 0)
            {
                _categories = response.Items;
                CategoryComboBox.ItemsSource = _categories;
                CategoryComboBox.SelectedIndex = 0;
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
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusTextBlock.Text = "Tên sản phẩm là bắt buộc.";
            return;
        }

        if (BranchComboBox.SelectedItem is not BranchDto selectedBranch)
        {
            StatusTextBlock.Text = "Vui lòng chọn chi nhánh áp dụng.";
            return;
        }

        if (CategoryComboBox.SelectedItem is not CategoryDto selectedCategory)
        {
            StatusTextBlock.Text = "Vui lòng chọn danh mục.";
            return;
        }

        var unit = UnitTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(unit))
        {
            StatusTextBlock.Text = "Đơn vị tính là bắt buộc.";
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

        int initialStock = 50;
        if (!string.IsNullOrWhiteSpace(InitialStockTextBox.Text))
        {
            if (!int.TryParse(InitialStockTextBox.Text.Trim(), out initialStock) || initialStock < 0)
            {
                StatusTextBlock.Text = "Số lượng tồn kho ban đầu phải là số nguyên không âm.";
                return;
            }
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
            BranchId = selectedBranch.Id,
            CategoryId = selectedCategory.Id,
            Name = name,
            Description = DescriptionTextBox.Text.Trim(),
            Price = sellingPrice ?? 0,
            UnitCost = unitCost,
            SellingPrice = sellingPrice,
            InitialStock = initialStock,
            ExpiryDate = ExpiryDatePicker.SelectedDate ?? DateTime.Today.AddDays(30),
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
                MessageBox.Show($"Tạo sản phẩm '{name}' cho chi nhánh '{selectedBranch.BranchName}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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