namespace ECommerce.Web.Services;

/// <summary>
/// Talks to the Promotions service through the gateway ("/promotions/...").
/// Base address is resolved by Aspire service discovery.
/// </summary>
public class PromotionsApiClient(HttpClient httpClient)
{
    public async Task<PromotionValidationResult> ValidateAsync(string code, decimal amount, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/promotions/api/promotions/{Uri.EscapeDataString(code)}/validate",
            new ValidatePromoRequestDto(amount),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return new PromotionValidationResult(false, null, "unknown_code");
        }

        return await response.Content.ReadFromJsonAsync<PromotionValidationResult>(cancellationToken: ct)
            ?? new PromotionValidationResult(false, null, "unknown_code");
    }
}

public record ValidatePromoRequestDto(decimal Amount);
public record PromotionValidationResult(bool Valid, decimal? DiscountedAmount, string? Reason);
