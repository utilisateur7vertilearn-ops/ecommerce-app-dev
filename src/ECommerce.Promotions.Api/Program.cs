using ECommerce.Promotions.Api.Data;
using ECommerce.Promotions.Api.Domain;
using ECommerce.Promotions.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<PromotionsDbContext>("promotionsdb");
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

// Applique les migrations en attente (et leur seed embarqué) au démarrage.
// Pratique en dev ; en preprod/prod, appliquer une migration est un geste
// délibéré et validé par un humain, pas quelque chose qui arrive à chaque boot.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PromotionsDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program;
