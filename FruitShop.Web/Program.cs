using FruitShop.Web.Services;

namespace FruitShop.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(2);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddSingleton<TcpClientService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<INotification, Email>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSession();
        app.UseRouting();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}