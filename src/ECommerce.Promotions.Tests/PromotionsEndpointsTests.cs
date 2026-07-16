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
        var response = await _client.PostAsJsonAsync("/api/promotions", ValidRequest("WELCOME10"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PromotionDto>(JsonOptions);
        Assert.Equal("WELCOME10", dto!.Code);
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
        await _client.PostAsJsonAsync("/api/promotions", ValidRequest("BLACKFRIDAY"), JsonOptions);

        var response = await _client.PostAsJsonAsync(
            "/api/promotions/BLACKFRIDAY/validate", new ValidateRequest(80m), JsonOptions);

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
}
