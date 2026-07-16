using ECommerce.Promotions.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Api.Data;

public class PromotionsDbContext(DbContextOptions<PromotionsDbContext> options) : DbContext(options)
{
    public DbSet<Promotion> Promotions => Set<Promotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.MinAmount).HasPrecision(18, 2);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Promotion_Percent_Range", "\"Percent\" BETWEEN 1 AND 90");
                t.HasCheckConstraint("CK_Promotion_MinAmount_NonNegative", "\"MinAmount\" >= 0");
                t.HasCheckConstraint("CK_Promotion_MaxUses_Positive", "\"MaxUses\" >= 1");
                t.HasCheckConstraint("CK_Promotion_UsesCount_NonNegative", "\"UsesCount\" >= 0");
                t.HasCheckConstraint("CK_Promotion_ValidUntil_After_ValidFrom", "\"ValidUntil\" > \"ValidFrom\"");
            });
        });

        // Seed synthétique (aucune donnée client réelle) : 3 codes de démo pour que le
        // service soit utilisable immédiatement (dont un déjà expiré).
        modelBuilder.Entity<Promotion>().HasData(
            new Promotion
            {
                Id = 1,
                Code = "BLACKFRIDAY",
                Percent = 20,
                ValidFrom = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero),
                ValidUntil = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero),
                MaxUses = 1000,
                UsesCount = 0,
                MinAmount = 50m,
            },
            new Promotion
            {
                Id = 2,
                Code = "WELCOME10",
                Percent = 10,
                ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ValidUntil = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
                MaxUses = 100000,
                UsesCount = 0,
                MinAmount = 0m,
            },
            new Promotion
            {
                Id = 3,
                Code = "SUMMER2025",
                Percent = 15,
                ValidFrom = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
                ValidUntil = new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero),
                MaxUses = 500,
                UsesCount = 0,
                MinAmount = 0m,
            });
    }
}
