namespace ECommerce.Promotions.Api.Domain;

public sealed class PromotionRules(IClock clock)
{
    public string? ValidateForCreation(CreatePromotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return "Code is required.";
        if (request.Percent is < 1 or > 90)
            return "Percent must be between 1 and 90.";
        if (request.ValidUntil <= request.ValidFrom)
            return "ValidUntil must be after ValidFrom.";
        if (request.MaxUses < 1)
            return "MaxUses must be at least 1.";
        if (request.MinAmount < 0)
            return "MinAmount cannot be negative.";

        return null;
    }

    public ValidationOutcome Validate(Promotion? promotion, decimal amount)
    {
        if (promotion is null)
            return ValidationOutcome.Invalid("unknown_code");

        var now = clock.UtcNow;
        if (now < promotion.ValidFrom)
            return ValidationOutcome.Invalid("not_started");
        if (now > promotion.ValidUntil)
            return ValidationOutcome.Invalid("expired");
        if (promotion.UsesCount >= promotion.MaxUses)
            return ValidationOutcome.Invalid("exhausted");
        if (amount < promotion.MinAmount)
            return ValidationOutcome.Invalid("amount_too_low");

        var discountedAmount = Math.Round(amount * (1 - promotion.Percent / 100m), 2, MidpointRounding.AwayFromZero);
        return ValidationOutcome.Success(discountedAmount);
    }
}

public record ValidationOutcome(bool Valid, decimal? DiscountedAmount, string? Reason)
{
    public static ValidationOutcome Success(decimal discountedAmount) => new(true, discountedAmount, null);
    public static ValidationOutcome Invalid(string reason) => new(false, null, reason);
}

public record CreatePromotionRequest(
    string Code,
    int Percent,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidUntil,
    int MaxUses,
    decimal MinAmount);
