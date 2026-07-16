using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Promotions.Api.Domain;
using ECommerce.Promotions.Api.Endpoints;

namespace ECommerce.Promotions.Tests;

public class PromotionsEndpointsTests : IClassFixture<PromotionsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public PromotionsEndpointsTests(PromotionsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreatePromotionRequest ValidRequest(string code) => new(
        code,
        Percent: 20,
        ValidFrom: DateTimeOffset.UtcNow.AddDays(-1),
        ValidUntil: DateTimeOffset.UtcNow.AddDays(30),
        MaxUses: 100,
        MinAmount: 50m);

    [Fact]
    public async Task CreatePromotion_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/promotions", ValidRequest("QATEST01"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PromotionDto>(JsonOptions);
        Assert.Equal("QATEST01", dto!.Code);
    }

    [Fact]
    public async Task CreatePromotion_InvalidPercent_Returns400()
    {
        var request = ValidRequest("BADPERCENT") with { Percent = 95 };

        var response = await _client.PostAsJsonAsync("/api/promotions", request, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPromotion_UnknownCode_Returns404()
    {
        var response = await _client.GetAsync("/api/promotions/DOESNOTEXIST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ValidatePromotion_ValidAmount_ReturnsValidTrue()
    {
        await _client.PostAsJsonAsync("/api/promotions", ValidRequest("QATEST02"), JsonOptions);

        var response = await _client.PostAsJsonAsync(
            "/api/promotions/QATEST02/validate", new ValidateRequest(80m), JsonOptions);

        var outcome = await response.Content.ReadFromJsonAsync<ValidationOutcome>(JsonOptions);
        Assert.True(outcome!.Valid);
        Assert.Equal(64.00m, outcome.DiscountedAmount);
    }

    [Fact]
    public async Task ValidatePromotion_UnknownCode_ReturnsValidFalseWithUnknownCodeReason()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/promotions/NOSUCHCODE/validate", new ValidateRequest(80m), JsonOptions);

        var outcome = await response.Content.ReadFromJsonAsync<ValidationOutcome>(JsonOptions);
        Assert.False(outcome!.Valid);
        Assert.Equal("unknown_code", outcome.Reason);
    }

    [Fact]
    public async Task ValidatePromotion_LastUseConsumed_SubsequentCallReturnsExhausted()
    {
        var request = ValidRequest("QATEST03") with { MaxUses = 1 };
        await _client.PostAsJsonAsync("/api/promotions", request, JsonOptions);

        var first = await _client.PostAsJsonAsync(
            "/api/promotions/QATEST03/validate", new ValidateRequest(80m), JsonOptions);
        var firstOutcome = await first.Content.ReadFromJsonAsync<ValidationOutcome>(JsonOptions);
        Assert.True(firstOutcome!.Valid);

        var second = await _client.PostAsJsonAsync(
            "/api/promotions/QATEST03/validate", new ValidateRequest(80m), JsonOptions);
        var secondOutcome = await second.Content.ReadFromJsonAsync<ValidationOutcome>(JsonOptions);
        Assert.False(secondOutcome!.Valid);
        Assert.Equal("exhausted", secondOutcome.Reason);

        var dto = await (await _client.GetAsync("/api/promotions/QATEST03")).Content.ReadFromJsonAsync<PromotionDto>(JsonOptions);
        Assert.Equal(1, dto!.UsesCount);
    }
}
