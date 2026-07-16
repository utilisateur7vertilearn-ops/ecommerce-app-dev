using ECommerce.Promotions.Api.Data;
using ECommerce.Promotions.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Api.Endpoints;

public static class PromotionsEndpoints
{
    public static RouteGroupBuilder MapPromotionsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/promotions").WithTags("Promotions");

        group.MapPost("/", async (CreatePromotionRequest request, PromotionsDbContext db, PromotionRules rules) =>
        {
            var error = rules.ValidateForCreation(request);
            if (error is not null)
                return Results.BadRequest(error);

            var code = request.Code.Trim().ToUpperInvariant();
            if (await db.Promotions.AnyAsync(p => p.Code == code))
                return Results.Conflict($"Promotion code {code} already exists.");

            var promotion = new Promotion
            {
                Code = code,
                Percent = request.Percent,
                ValidFrom = request.ValidFrom,
                ValidUntil = request.ValidUntil,
                MaxUses = request.MaxUses,
                MinAmount = request.MinAmount,
            };

            db.Promotions.Add(promotion);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict($"Promotion code {code} already exists.");
            }

            return Results.CreatedAtRoute("GetPromotionByCode", new { code = promotion.Code }, ToDto(promotion));
        })
        .WithName("CreatePromotion");

        group.MapGet("/{code}", async (string code, PromotionsDbContext db) =>
        {
            var normalized = code.Trim().ToUpperInvariant();
            var promotion = await db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Code == normalized);
            return promotion is not null ? Results.Ok(ToDto(promotion)) : Results.NotFound();
        })
        .WithName("GetPromotionByCode");

        group.MapPost("/{code}/validate", async (string code, ValidateRequest request, PromotionsDbContext db, PromotionRules rules) =>
        {
            if (request.Amount <= 0)
                return Results.BadRequest("Amount must be greater than zero.");

            var normalized = code.Trim().ToUpperInvariant();
            var promotion = await db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Code == normalized);
            var outcome = rules.Validate(promotion, request.Amount);
            if (!outcome.Valid)
                return Results.Ok(outcome);

            // Incrément atomique conditionnel : Postgres évalue le WHERE au moment de
            // l'UPDATE, sous verrou de ligne. Si deux requêtes valident le même code
            // au même instant avec 1 utilisation restante, l'UPDATE de la seconde
            // s'exécute après le commit de la première et réévalue "UsesCount < MaxUses"
            // sur la valeur déjà incrémentée : elle affecte 0 ligne au lieu de doubler
            // le compteur. Pas de token de concurrence ni de retry nécessaires.
            var consumed = await db.Promotions
                .Where(p => p.Code == normalized && p.UsesCount < p.MaxUses)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.UsesCount, p => p.UsesCount + 1));

            return consumed == 0
                ? Results.Ok(ValidationOutcome.Invalid("exhausted"))
                : Results.Ok(outcome);
        })
        .WithName("ValidatePromotion");

        return group;
    }

    private static PromotionDto ToDto(Promotion promotion) => new(
        promotion.Code,
        promotion.Percent,
        promotion.ValidFrom,
        promotion.ValidUntil,
        promotion.MaxUses,
        promotion.UsesCount,
        promotion.MinAmount);
}

public record PromotionDto(
    string Code,
    int Percent,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    int MaxUses,
    int UsesCount,
    decimal MinAmount);

public record ValidateRequest(decimal Amount);
