using FruitShop.Shared.Contracts;
using FruitShop.Shared.Helpers;
using System.Text.Json;

namespace FruitShop.Server.Services;

public sealed class RequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly UserAuthenticationService _authService;
    private readonly UserRegistrationService _regService;
    private readonly ProductManagementService _productService;
    private readonly InventoryService _inventoryService;
    private readonly OrderService _orderService;
    private readonly UserService _userService;
    private readonly BranchService _branchService;

    public RequestHandler(
        UserAuthenticationService authService,
        UserRegistrationService regService,
        ProductManagementService productService,
        InventoryService inventoryService,
        OrderService orderService,
        UserService userService,
        BranchService branchService)
    {
        _authService = authService;
        _regService = regService;
        _productService = productService;
        _inventoryService = inventoryService;
        _orderService = orderService;
        _userService = userService;
        _branchService = branchService;
    }

    public async Task<TcpResponse> HandleRequestAsync(TcpRequest request, CancellationToken cancellationToken = default)
    {
        var response = new TcpResponse();
        try
        {
            switch (request.Action.ToUpperInvariant())
            {
                case "LOGIN":
                    {
                        var req = JsonSerializer.Deserialize<LoginRequest>(request.Data, JsonOptions);
                        var res = await _authService.AuthenticateAsync(req?.Username, req?.Password, cancellationToken);
                        response.Status = res.Success ? "SUCCESS" : "ERROR";
                        response.Message = res.Message;
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "REGISTER":
                    {
                        var req = JsonSerializer.Deserialize<RegisterRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu đăng ký không hợp lệ.");
                        var res = await _regService.RegisterAsync(req, cancellationToken);
                        response.Status = res.Success ? "SUCCESS" : "ERROR";
                        response.Message = res.Message;
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                // User Management Actions (Admin)
                case "GET_USERS":
                case "USERS":
                    {
                        var res = await _userService.GetUsersAsync(cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "CREATE_USER":
                    {
                        var req = JsonSerializer.Deserialize<CreateUserRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu tạo người dùng không hợp lệ.");
                        return await _userService.CreateUserAsync(req, cancellationToken);
                    }

                case "UPDATE_USER":
                    {
                        var req = JsonSerializer.Deserialize<UpdateUserRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu cập nhật người dùng không hợp lệ.");
                        return await _userService.UpdateUserAsync(req, cancellationToken);
                    }

                case "TOGGLE_USER_ACTIVE":
                    {
                        int id = int.TryParse(request.Data, out var uId) ? uId : 0;
                        return await _userService.ToggleActiveAsync(id, cancellationToken);
                    }

                case "RESET_USER_PASSWORD":
                    {
                        var req = JsonSerializer.Deserialize<ResetPasswordRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu đặt lại mật khẩu không hợp lệ.");
                        return await _userService.ResetPasswordAsync(req, cancellationToken);
                    }

                // Branch Management Actions (Manager)
                case "GET_BRANCHES":
                case "BRANCHES":
                    {
                        var res = await _branchService.GetBranchesAsync(cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "CREATE_BRANCH":
                    {
                        var req = JsonSerializer.Deserialize<CreateBranchRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu tạo chi nhánh không hợp lệ.");
                        return await _branchService.CreateBranchAsync(req, cancellationToken);
                    }

                case "UPDATE_BRANCH":
                    {
                        var req = JsonSerializer.Deserialize<UpdateBranchRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu cập nhật chi nhánh không hợp lệ.");
                        return await _branchService.UpdateBranchAsync(req, cancellationToken);
                    }

                // Product Actions
                case "GET_PRODUCTS":
                case "PRODUCTS":
                    {
                        int? branchId = null;
                        if (!string.IsNullOrWhiteSpace(request.Data))
                        {
                            if (int.TryParse(request.Data, out var bId))
                            {
                                branchId = bId > 0 ? bId : null;
                            }
                            else
                            {
                                try
                                {
                                    var reqObj = JsonSerializer.Deserialize<GetProductsRequest>(request.Data, JsonOptions);
                                    if (reqObj?.BranchId is > 0) branchId = reqObj.BranchId;
                                }
                                catch { }
                            }
                        }
                        var res = await _productService.GetActiveProductsAsync(branchId, cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "GET_PRODUCTS_PAGED":
                case "PRODUCTS_PAGED":
                    {
                        var req = JsonSerializer.Deserialize<GetProductsPagedRequest>(request.Data ?? "{}", JsonOptions) ?? new GetProductsPagedRequest();
                        var res = await _productService.GetPagedProductsForWebAsync(req, cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "GET_PRODUCT_BY_ID":
                case "PRODUCT-DETAIL":
                    {
                        int id = int.TryParse(request.Data, out var parsedId) ? parsedId : 0;
                        var res = await _productService.GetProductByIdAsync(id, null, cancellationToken);
                        if (res is not null)
                        {
                            response.Status = "SUCCESS";
                            response.Data = JsonSerializer.Serialize(res, JsonOptions);
                        }
                        else
                        {
                            response.Status = "ERROR";
                            response.Message = "Không tìm thấy sản phẩm.";
                        }
                    }
                    break;

                case "GET_WEB_PRODUCT_DETAIL":
                    {
                        int id = int.TryParse(request.Data, out var parsedId) ? parsedId : 0;
                        var res = await _productService.GetWebProductByIdAsync(id, null, cancellationToken);
                        if (res is not null)
                        {
                            response.Status = "SUCCESS";
                            response.Data = JsonSerializer.Serialize(res, JsonOptions);
                        }
                        else
                        {
                            response.Status = "ERROR";
                            response.Message = "Không tìm thấy sản phẩm.";
                        }
                    }
                    break;

                case "SEARCH_PRODUCTS":
                    {
                        var res = await _productService.SearchProductsAsync(request.Data, null, cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "CREATE_PRODUCT":
                case "CREATE-PRODUCT":
                    {
                        var req = JsonSerializer.Deserialize<CreateProductRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu tạo sản phẩm không hợp lệ.");
                        return await _productService.CreateAsync(req, cancellationToken);
                    }

                case "UPDATE_PRODUCT":
                case "UPDATE-PRODUCT":
                    {
                        var req = JsonSerializer.Deserialize<UpdateProductRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu cập nhật sản phẩm không hợp lệ.");
                        return await _productService.UpdateAsync(req, cancellationToken);
                    }

                case "HIDE_PRODUCT":
                case "HIDE-PRODUCT":
                    {
                        int id = int.TryParse(request.Data, out var pId) ? pId : 0;
                        return await _productService.HideAsync(id, cancellationToken);
                    }

                case "GET_CATEGORIES":
                case "CATEGORIES":
                    {
                        var res = await _productService.GetActiveCategoriesAsync(cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "GET_INVENTORY":
                case "INVENTORY":
                    {
                        int? branchId = null;
                        if (!string.IsNullOrWhiteSpace(request.Data))
                        {
                            if (int.TryParse(request.Data, out var bId))
                            {
                                branchId = bId > 0 ? bId : null;
                            }
                            else
                            {
                                try
                                {
                                    var reqObj = JsonSerializer.Deserialize<GetProductsRequest>(request.Data, JsonOptions);
                                    if (reqObj?.BranchId is > 0) branchId = reqObj.BranchId;
                                }
                                catch { }
                            }
                        }
                        var res = await _inventoryService.GetInventoryAsync(branchId, cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "RECEIVE_INVENTORY":
                case "RECEIVE-INVENTORY":
                    {
                        var req = JsonSerializer.Deserialize<ReceiveInventoryRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu nhập kho không hợp lệ.");
                        return await _inventoryService.ReceiveAsync(req, cancellationToken);
                    }

                case "GET_ORDERS":
                case "ORDERS":
                    {
                        int? branchId = null;
                        if (!string.IsNullOrWhiteSpace(request.Data))
                        {
                            if (int.TryParse(request.Data, out var bId))
                            {
                                branchId = bId > 0 ? bId : null;
                            }
                            else
                            {
                                try
                                {
                                    var reqObj = JsonSerializer.Deserialize<GetProductsRequest>(request.Data, JsonOptions);
                                    if (reqObj?.BranchId is > 0) branchId = reqObj.BranchId;
                                }
                                catch { }
                            }
                        }
                        var res = await _orderService.GetOrdersAsync(branchId, cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "GET_ORDERS_BY_USER":
                    {
                        int userId = int.TryParse(request.Data, out var uId) ? uId : 0;
                        var res = await _orderService.GetOrdersByUserAsync(userId, cancellationToken);
                        response.Status = "SUCCESS";
                        response.Data = JsonSerializer.Serialize(res, JsonOptions);
                    }
                    break;

                case "CREATE_ORDER":
                    {
                        var req = JsonSerializer.Deserialize<CreateOrderRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu đơn hàng không hợp lệ.");
                        return await _orderService.CreateOrderAsync(req, cancellationToken);
                    }

                case "UPDATE_ORDER_STATUS":
                    {
                        var req = JsonSerializer.Deserialize<UpdateOrderStatusRequest>(request.Data, JsonOptions);
                        if (req is null) throw new Exception("Dữ liệu cập nhật không hợp lệ.");
                        return await _orderService.UpdateOrderStatusAsync(req, cancellationToken);
                    }

                case "MARK_ORDER_PAID":
                case "MARK-ORDER-PAID":
                    {
                        int id = int.TryParse(request.Data, out var oId) ? oId : 0;
                        return await _orderService.MarkAsPaidAsync(oId, cancellationToken);
                    }

                default:
                    response.Status = "ERROR";
                    response.Message = $"Hành động '{request.Action}' không được hỗ trợ.";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.Status = "ERROR";
            response.Message = $"Lỗi xử lý Server: {ex.Message}";
        }
        return response;
    }
}