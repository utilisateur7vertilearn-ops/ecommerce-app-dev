using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Promotions.Api.Data;

// Permet à `dotnet ef` de construire le DbContext hors Aspire. La chaîne de
// connexion ci-dessous ne sert qu'à générer le SQL des migrations (dotnet ef
// migrations add n'a pas besoin d'une base réellement joignable) ; au
// runtime, Aspire injecte la vraie chaîne via AddNpgsqlDbContext. Chaîne de
// design-time locale, non sensible — pas un secret.
public class PromotionsDbContextFactory : IDesignTimeDbContextFactory<PromotionsDbContext>
{
    public PromotionsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PromotionsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=promotionsdb;Username=postgres;Password=postgres");

        return new PromotionsDbContext(optionsBuilder.Options);
    }
}
