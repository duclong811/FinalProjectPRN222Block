using FruitShop.Web.Models;
using FruitShop.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FruitShop.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IProductService productService, ILogger<HomeController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> SetBranch(int branchId, string? returnUrl = null)
        {
            if (branchId > 0)
            {
                var branches = await _productService.GetBranchesAsync();
                var branch = branches.FirstOrDefault(b => b.Id == branchId);
                if (branch != null)
                {
                    HttpContext.Session.SetInt32("SelectedBranchId", branch.Id);
                    HttpContext.Session.SetString("SelectedBranchName", branch.BranchName);
                }
            }
            else
            {
                HttpContext.Session.Remove("SelectedBranchId");
                HttpContext.Session.Remove("SelectedBranchName");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
