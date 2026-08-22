using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;

namespace FruitShop.Client.Services;

public sealed class TcpClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _host;
    private readonly int _port;

    public TcpClientService(string host = "127.0.0.1", int port = 5055)
    {
        _host = host;
        _port = port;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var response = await SendRequestAsync("LOGIN", new LoginRequest { Username = username, Password = password });
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new LoginResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<LoginResponse>(response.Data, JsonOptions)
            ?? new LoginResponse { Success = false, Message = "Lỗi giải mã dữ liệu đăng nhập." };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await SendRequestAsync("REGISTER", request);
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new LoginResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<LoginResponse>(response.Data, JsonOptions)
            ?? new LoginResponse { Success = false, Message = "Lỗi giải mã dữ liệu đăng ký." };
    }

    public async Task<ProductListResponse> GetProductsAsync(int? branchId = null)
    {
        var response = await SendRequestAsync("GET_PRODUCTS", branchId?.ToString() ?? string.Empty);
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new ProductListResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<ProductListResponse>(response.Data, JsonOptions)
            ?? new ProductListResponse { Success = false, Message = "Lỗi giải mã danh sách sản phẩm." };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId, int? branchId = null)
    {
        var response = await SendRequestAsync("GET_PRODUCT_BY_ID", productId.ToString());
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data)) return null;
        return JsonSerializer.Deserialize<ProductDto>(response.Data, JsonOptions);
    }

    public async Task<ProductDto?> GetProductDetailAsync(int productId, int? branchId = null) => await GetProductByIdAsync(productId, branchId);

    public async Task<CategoryListResponse> GetCategoriesAsync()
    {
        var response = await SendRequestAsync("GET_CATEGORIES");
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new CategoryListResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<CategoryListResponse>(response.Data, JsonOptions)
            ?? new CategoryListResponse { Success = false, Message = "Lỗi giải mã danh mục." };
    }

    public async Task<InventoryListResponse> GetInventoryAsync(int? branchId = null)
    {
        var response = await SendRequestAsync("GET_INVENTORY", branchId?.ToString() ?? string.Empty);
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new InventoryListResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<InventoryListResponse>(response.Data, JsonOptions)
            ?? new InventoryListResponse { Success = false, Message = "Lỗi giải mã kho hàng." };
    }

    public async Task<OrderListResponse> GetOrdersAsync(int? branchId = null)
    {
        var response = await SendRequestAsync("GET_ORDERS", branchId?.ToString() ?? string.Empty);
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new OrderListResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<OrderListResponse>(response.Data, JsonOptions)
            ?? new OrderListResponse { Success = false, Message = "Lỗi giải mã đơn hàng." };
    }

    public async Task<TcpResponse> CreateProductAsync(CreateProductRequest request) => await SendRequestAsync("CREATE_PRODUCT", request);
    public async Task<TcpResponse> UpdateProductAsync(UpdateProductRequest request) => await SendRequestAsync("UPDATE_PRODUCT", request);
    public async Task<TcpResponse> HideProductAsync(int productId) => await SendRequestAsync("HIDE_PRODUCT", productId.ToString());
    public async Task<TcpResponse> ReceiveInventoryAsync(ReceiveInventoryRequest request) => await SendRequestAsync("RECEIVE_INVENTORY", request);
    public async Task<TcpResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request) => await SendRequestAsync("UPDATE_ORDER_STATUS", request);
    public async Task<TcpResponse> MarkOrderPaidAsync(int orderId) => await SendRequestAsync("MARK_ORDER_PAID", orderId.ToString());

    // User Management (Admin)
    public async Task<UserListResponse> GetUsersAsync()
    {
        var response = await SendRequestAsync("GET_USERS");
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new UserListResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<UserListResponse>(response.Data, JsonOptions)
            ?? new UserListResponse { Success = false, Message = "Lỗi giải mã danh sách người dùng." };
    }

    public async Task<TcpResponse> CreateUserAsync(CreateUserRequest request) => await SendRequestAsync("CREATE_USER", request);
    public async Task<TcpResponse> UpdateUserAsync(UpdateUserRequest request) => await SendRequestAsync("UPDATE_USER", request);
    public async Task<TcpResponse> ToggleUserActiveAsync(int userId) => await SendRequestAsync("TOGGLE_USER_ACTIVE", userId.ToString());
    public async Task<TcpResponse> ResetUserPasswordAsync(ResetPasswordRequest request) => await SendRequestAsync("RESET_USER_PASSWORD", request);

    // Branch Management (Manager)
    public async Task<BranchListResponse> GetBranchesAsync()
    {
        var response = await SendRequestAsync("GET_BRANCHES");
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new BranchListResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<BranchListResponse>(response.Data, JsonOptions)
            ?? new BranchListResponse { Success = false, Message = "Lỗi giải mã danh sách chi nhánh." };
    }

    public async Task<TcpResponse> CreateBranchAsync(CreateBranchRequest request) => await SendRequestAsync("CREATE_BRANCH", request);
    public async Task<TcpResponse> UpdateBranchAsync(UpdateBranchRequest request) => await SendRequestAsync("UPDATE_BRANCH", request);

    // Notifications (Manager / Staff / Admin)
    public async Task<NotificationListResponse> GetNotificationsAsync(int? branchId = null, int? userId = null)
    {
        var request = new GetNotificationsRequest { BranchId = branchId, UserId = userId };
        var response = await SendRequestAsync("GET_NOTIFICATIONS", request);
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new NotificationListResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<NotificationListResponse>(response.Data, JsonOptions)
            ?? new NotificationListResponse { Success = false, Message = "Lỗi giải mã danh sách thông báo." };
    }

    public async Task<TcpResponse> MarkNotificationReadAsync(int notificationId) => await SendRequestAsync("MARK_NOTIFICATION_READ", notificationId.ToString());
    public async Task<TcpResponse> MarkAllNotificationsReadAsync(int? branchId = null) => await SendRequestAsync("MARK_ALL_NOTIFICATIONS_READ", branchId?.ToString() ?? string.Empty);

    // Revenue & Analytics (Manager)
    public async Task<RevenueReportResponse> GetRevenueReportAsync(GetRevenueReportRequest request)
    {
        var response = await SendRequestAsync("GET_REVENUE_REPORT", request);
        if (response.Status != "SUCCESS" || string.IsNullOrEmpty(response.Data))
            return new RevenueReportResponse { Success = false, Message = response.Message };

        return JsonSerializer.Deserialize<RevenueReportResponse>(response.Data, JsonOptions)
            ?? new RevenueReportResponse { Success = false, Message = "Lỗi giải mã báo cáo doanh thu." };
    }

    private async Task<TcpResponse> SendRequestAsync<T>(string action, T payload)
    {
        var dataJson = JsonSerializer.Serialize(payload, JsonOptions);
        return await SendRequestAsync(action, dataJson);
    }

    private async Task<TcpResponse> SendRequestAsync(string action, string? data = null)
    {
        using var client = new TcpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await client.ConnectAsync(_host, _port, cts.Token);
        using var stream = client.GetStream();

        var request = new TcpRequest { Action = action, Data = data ?? string.Empty };
        await TcpHelper.SendMessageAsync(stream, JsonSerializer.Serialize(request, JsonOptions));

        var responseJson = await TcpHelper.ReceiveMessageAsync(stream);
        if (string.IsNullOrEmpty(responseJson))
            return new TcpResponse { Status = "ERROR", Message = "Máy chủ TCP không phản hồi." };

        return JsonSerializer.Deserialize<TcpResponse>(responseJson, JsonOptions)
            ?? new TcpResponse { Status = "ERROR", Message = "Lỗi giải mã phản hồi TCP." };
    }
}