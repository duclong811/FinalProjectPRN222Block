using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class BranchManagementWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);
    private List<BranchDto> _branches = new();

    public BranchManagementWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var response = await _clientService.GetBranchesAsync();
            if (!response.Success)
            {
                StatusTextBlock.Text = response.Message;
                return;
            }

            _branches = response.Items;
            BranchesDataGrid.ItemsSource = _branches;
            StatusTextBlock.Text = $"Đã tải {_branches.Count} chi nhánh.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadBranchesAsync();

    private async void AddBranchButton_Click(object sender, RoutedEventArgs e)
    {
        var branchName = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên chi nhánh mới:", "Tạo Chi Nhánh Mới");
        if (string.IsNullOrWhiteSpace(branchName)) return;

        var address = Microsoft.VisualBasic.Interaction.InputBox("Nhập địa chỉ chi nhánh:", "Tạo Chi Nhánh Mới");
        if (string.IsNullOrWhiteSpace(address)) return;

        var phone = Microsoft.VisualBasic.Interaction.InputBox("Nhập số điện thoại:", "Tạo Chi Nhánh Mới");

        var request = new CreateBranchRequest
        {
            ManagerId = 2,
            BranchName = branchName,
            Address = address,
            Phone = phone
        };

        var res = await _clientService.CreateBranchAsync(request);
        MessageBox.Show(res.Message, "Thông báo", MessageBoxButton.OK, res.Status == "SUCCESS" ? MessageBoxImage.Information : MessageBoxImage.Warning);
        await LoadBranchesAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}