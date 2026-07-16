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
            return Results.Ok(outcome);
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
