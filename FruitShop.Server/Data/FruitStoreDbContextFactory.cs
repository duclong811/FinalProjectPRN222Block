using Microsoft.EntityFrameworkCore;

namespace FruitShop.Server.Data;

internal static class FruitStoreDbContextFactory
{
    public static FruitStoreDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<FruitStoreDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new FruitStoreDbContext(options);
    }
}
