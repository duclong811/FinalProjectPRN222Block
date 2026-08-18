using System.Text.Json;
using FruitShop.Shared.Contracts;
using FruitShop.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Web.Controllers;

public class AccountController : Controller
{
    private readonly TcpClientService _tcpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AccountController(TcpClientService tcpClient)
    {
        _tcpClient = tcpClient;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu.";
            return View();
        }

        var response = await _tcpClient.SendRequestAsync("LOGIN", JsonSerializer.Serialize(new LoginRequest { Username = username, Password = password }));
        if (response.Status == "SUCCESS" && !string.IsNullOrEmpty(response.Data))
        {
            var loginResult = JsonSerializer.Deserialize<LoginResponse>(response.Data, JsonOptions);
            if (loginResult != null && loginResult.Success)
            {
                if (!string.Equals(loginResult.RoleName, "Customer", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Tài khoản quản trị/nhân viên vui lòng đăng nhập trên ứng dụng quản lý Desktop.";
                    return View();
                }

                HttpContext.Session.SetInt32("UserId", loginResult.UserId);
                HttpContext.Session.SetString("Username", loginResult.Username);
                HttpContext.Session.SetString("FullName", loginResult.FullName);
                HttpContext.Session.SetString("RoleName", loginResult.RoleName);

                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = loginResult?.Message ?? "Đăng nhập thất bại.";
            return View();
        }

        ViewBag.Error = response.Message;
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var response = await _tcpClient.SendRequestAsync("REGISTER", JsonSerializer.Serialize(request));
        if (response.Status == "SUCCESS")
        {
            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        ViewBag.Error = response.Message;
        return View(request);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}