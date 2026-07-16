using ECommerce.Promotions.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Promotions.Tests;

public class PromotionsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"promotions-tests-{Guid.NewGuid()}";
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PromotionsDbContext>>();
            services.AddDbContext<PromotionsDbContext>(options =>
                options.UseInMemoryDatabase(_dbName, _dbRoot));
        });
    }
}
