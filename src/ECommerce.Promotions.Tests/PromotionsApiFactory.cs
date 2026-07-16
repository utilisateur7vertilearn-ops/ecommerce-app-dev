using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace ECommerce.Promotions.Tests;

public class PromotionsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("promotionsdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    async Task IAsyncLifetime.InitializeAsync() => await _container.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs enregistre PromotionsDbContext via builder.AddNpgsqlDbContext<>("promotionsdb"),
        // qui lit sa chaîne de connexion depuis la config "ConnectionStrings:promotionsdb". On
        // pointe cette clé vers le conteneur Testcontainers plutôt que de retirer/ré-enregistrer
        // le DbContext à la main : AddNpgsqlDbContext utilise un pool (options en singleton), et
        // un simple RemoveAll<DbContextOptions<T>>() + AddDbContext(...) laisse des descripteurs
        // de pool orphelins qui cassent la construction du service provider.
        builder.UseSetting("ConnectionStrings:promotionsdb", _container.GetConnectionString());
    }
}
