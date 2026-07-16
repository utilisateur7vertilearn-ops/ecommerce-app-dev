using ECommerce.Promotions.Api.Data;
using ECommerce.Promotions.Api.Domain;
using ECommerce.Promotions.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<PromotionsDbContext>(options => options.UseInMemoryDatabase("promotions"));
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<PromotionRules>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.MapGet("/health", () => Results.Ok());
}

app.MapPromotionsEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PromotionsDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

public partial class Program;
