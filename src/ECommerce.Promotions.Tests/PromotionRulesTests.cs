using ECommerce.Promotions.Api.Domain;

namespace ECommerce.Promotions.Tests;

public class PromotionRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);

    private static PromotionRules CreateRules() => new(new FixedClock(Now));

    private static Promotion CreatePromotion(
        int percent = 20,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null,
        int maxUses = 100,
        int usesCount = 0,
        decimal minAmount = 50m) => new()
        {
            Code = "BLACKFRIDAY",
            Percent = percent,
            ValidFrom = validFrom ?? Now.AddDays(-1),
            ValidUntil = validUntil ?? Now.AddDays(1),
            MaxUses = maxUses,
            UsesCount = usesCount,
            MinAmount = minAmount,
        };

    [Fact]
    public void Validate_ValidCode_ReturnsDiscountedAmount()
    {
        var outcome = CreateRules().Validate(CreatePromotion(percent: 20, minAmount: 50m), amount: 80m);

        Assert.True(outcome.Valid);
        Assert.Equal(64.00m, outcome.DiscountedAmount);
        Assert.Null(outcome.Reason);
    }

    [Fact]
    public void Validate_UnknownCode_ReturnsUnknownCodeReason()
    {
        var outcome = CreateRules().Validate(promotion: null, amount: 80m);

        Assert.False(outcome.Valid);
        Assert.Equal("unknown_code", outcome.Reason);
    }

    [Fact]
    public void Validate_NotYetStarted_ReturnsNotStartedReason()
    {
        var promotion = CreatePromotion(validFrom: Now.AddDays(1), validUntil: Now.AddDays(10));

        var outcome = CreateRules().Validate(promotion, amount: 80m);

        Assert.False(outcome.Valid);
        Assert.Equal("not_started", outcome.Reason);
    }

    [Fact]
    public void Validate_Expired_ReturnsExpiredReason()
    {
        var promotion = CreatePromotion(validFrom: Now.AddDays(-10), validUntil: Now.AddDays(-1));

        var outcome = CreateRules().Validate(promotion, amount: 80m);

        Assert.False(outcome.Valid);
        Assert.Equal("expired", outcome.Reason);
    }

    [Fact]
    public void Validate_Exhausted_ReturnsExhaustedReason()
    {
        var promotion = CreatePromotion(maxUses: 5, usesCount: 5);

        var outcome = CreateRules().Validate(promotion, amount: 80m);

        Assert.False(outcome.Valid);
        Assert.Equal("exhausted", outcome.Reason);
    }

    [Fact]
    public void Validate_AmountBelowMinimum_ReturnsAmountTooLowReason()
    {
        var promotion = CreatePromotion(minAmount: 50m);

        var outcome = CreateRules().Validate(promotion, amount: 45m);

        Assert.False(outcome.Valid);
        Assert.Equal("amount_too_low", outcome.Reason);
    }

    [Fact]
    public void Validate_AmountExactlyAtMinimum_IsValid()
    {
        var promotion = CreatePromotion(minAmount: 50m);

        var outcome = CreateRules().Validate(promotion, amount: 50m);

        Assert.True(outcome.Valid);
    }

    [Fact]
    public void Validate_RoundsDiscountedAmountAwayFromZero()
    {
        var promotion = CreatePromotion(percent: 15, minAmount: 0m);

        var outcome = CreateRules().Validate(promotion, amount: 79.99m);

        Assert.True(outcome.Valid);
        Assert.Equal(67.99m, outcome.DiscountedAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void ValidateForCreation_PercentOutOfBounds_ReturnsError(int percent)
    {
        var request = new CreatePromotionRequest("WELCOME10", percent, Now, Now.AddDays(30), 100, 0m);

        var error = CreateRules().ValidateForCreation(request);

        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateForCreation_ValidUntilNotAfterValidFrom_ReturnsError()
    {
        var request = new CreatePromotionRequest("WELCOME10", 10, Now, Now, 100, 0m);

        var error = CreateRules().ValidateForCreation(request);

        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateForCreation_MaxUsesLessThanOne_ReturnsError()
    {
        var request = new CreatePromotionRequest("WELCOME10", 10, Now, Now.AddDays(30), 0, 0m);

        var error = CreateRules().ValidateForCreation(request);

        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateForCreation_NegativeMinAmount_ReturnsError()
    {
        var request = new CreatePromotionRequest("WELCOME10", 10, Now, Now.AddDays(30), 100, -1m);

        var error = CreateRules().ValidateForCreation(request);

        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateForCreation_ValidRequest_ReturnsNoError()
    {
        var request = new CreatePromotionRequest("WELCOME10", 10, Now, Now.AddDays(30), 100, 0m);

        var error = CreateRules().ValidateForCreation(request);

        Assert.Null(error);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
