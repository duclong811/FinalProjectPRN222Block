using System.Windows;
using System.Windows.Controls;
using FruitShop.Client.Services;
using FruitShop.Shared.Contracts;

namespace FruitShop.Client;

public partial class CreateAccountWindow : Window
{
    private readonly TcpClientService _clientService = new("127.0.0.1", 5055);

    // Các thuộc tính Public để Form cha có thể đọc dữ liệu ra
    public string AccountUsername { get; private set; } = string.Empty;
    public string AccountPassword { get; private set; } = string.Empty;
    public string AccountFullName { get; private set; } = string.Empty;
    public int RoleId { get; private set; }
    public int? BranchId { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }

    public CreateUserRequest ResultRequest => new()
    {
        Username = AccountUsername,
        Password = AccountPassword,
        FullName = AccountFullName,
        RoleId = RoleId,
        BranchId = BranchId,
        Email = Email,
        Phone = Phone,
        Address = Address
    };

    public CreateAccountWindow()
    {
        InitializeComponent();
        InitializeRoleOptions();
        Loaded += async (_, _) => await LoadBranchesAsync();
    }

    private void InitializeRoleOptions()
    {
        var roles = new[]
        {
            new { Id = 4, Name = "Staff (Nhân viên)" }
        };
        RoleComboBox.ItemsSource = roles;
        RoleComboBox.SelectedValue = 4; // Mặc định là Staff
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var response = await _clientService.GetBranchesAsync();
            if (response.Success && response.Items.Count > 0)
            {
                BranchComboBox.ItemsSource = response.Items;
                BranchComboBox.SelectedIndex = 0;
            }
        }
        catch
        {
            // Bỏ qua lỗi nếu không load được danh sách chi nhánh
        }
    }

    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Cả Staff và Manager đều gắn liền với chi nhánh
        BranchComboBox.IsEnabled = true;
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;
        var fullName = FullNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            StatusTextBlock.Text = "Vui lòng nhập Tên đăng nhập.";
            UsernameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            StatusTextBlock.Text = "Vui lòng nhập Mật khẩu.";
            PasswordBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            StatusTextBlock.Text = "Vui lòng nhập Họ và tên.";
            FullNameTextBox.Focus();
            return;
        }

        var phone = PhoneTextBox.Text.Trim();
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

        var selectedRoleId = (int)(RoleComboBox.SelectedValue ?? 4);
        int? selectedBranchId = null;

        if (BranchComboBox.IsEnabled && BranchComboBox.SelectedValue is int branchIdVal)
        {
            selectedBranchId = branchIdVal;
        }

        AccountUsername = username;
        AccountPassword = password;
        AccountFullName = fullName;
        RoleId = selectedRoleId;
        BranchId = selectedBranchId;
        Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        Address = string.IsNullOrWhiteSpace(AddressTextBox.Text) ? null : AddressTextBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
