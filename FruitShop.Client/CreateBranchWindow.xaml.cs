using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;
using System.Windows;

namespace FruitShop.Client;

public partial class CreateBranchWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);

    public CreateBranchRequest? ResultRequest { get; private set; }

    public CreateBranchWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadManagersAsync();
    }

    private async Task LoadManagersAsync()
    {
        try
        {
            var response = await _clientService.GetUsersAsync();
            if (response.Success && response.Items.Count > 0)
            {
                var managers = response.Items
                    .Where(u => u.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase) || u.RoleId == 3)
                    .Select(u => new
                    {
                        Id = u.Id,
                        DisplayName = $"{u.FullName} ({u.Username})"
                    })
                    .ToList();

                if (managers.Count > 0)
                {
                    ManagerComboBox.ItemsSource = managers;
                    ManagerComboBox.SelectedIndex = 0;
                }
                else
                {
                    StatusTextBlock.Text = "Chưa có tài khoản Manager trong hệ thống. Vui lòng tạo tài khoản Manager trước.";
                }
            }
            else
            {
                StatusTextBlock.Text = !string.IsNullOrEmpty(response.Message) ? response.Message : "Không thể tải danh sách tài khoản.";
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi tải danh sách quản lý: {ex.Message}";
        }
    }

    private async void btnSave_Click(object sender, RoutedEventArgs e)
    {
        var branchName = BranchNameTextBox.Text.Trim();
        var address = AddressTextBox.Text.Trim();
        var phone = PhoneTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(branchName))
        {
            StatusTextBlock.Text = "Vui lòng nhập tên chi nhánh.";
            BranchNameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            StatusTextBlock.Text = "Vui lòng nhập địa chỉ chi nhánh.";
            AddressTextBox.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            if (phone.Length != 10 || !phone.StartsWith("0") || !phone.All(char.IsDigit))
            {
                StatusTextBlock.Text = "Số điện thoại phải có 10 chữ số và bắt đầu bằng số 0.";
                MessageBox.Show("Số điện thoại phải có 10 chữ số và bắt đầu bằng số 0.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return;
            }
        }

        if (ManagerComboBox.SelectedValue is not int managerId || managerId <= 0)
        {
            StatusTextBlock.Text = "Vui lòng chọn Quản lý (Manager) phụ trách chi nhánh.";
            ManagerComboBox.Focus();
            return;
        }

        var request = new CreateBranchRequest
        {
            ManagerId = managerId,
            BranchName = branchName,
            Address = address,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone
        };

        btnSave.IsEnabled = false;
        try
        {
            var response = await _clientService.CreateBranchAsync(request);
            if (response.Status == "SUCCESS")
            {
                MessageBox.Show(response.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                ResultRequest = request;
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
            StatusTextBlock.Text = $"Lỗi gửi dữ liệu: {ex.Message}";
        }
        finally
        {
            btnSave.IsEnabled = true;
        }
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
