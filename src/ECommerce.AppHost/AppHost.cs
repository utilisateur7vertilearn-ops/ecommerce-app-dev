var builder = DistributedApplication.CreateBuilder(args);

// Catalog microservice.
var catalog = builder.AddProject<Projects.ECommerce_Catalog_Api>("catalog");

// Ordering microservice — depends on Catalog (resolved via service discovery).
var ordering = builder.AddProject<Projects.ECommerce_Ordering_Api>("ordering")
    .WithReference(catalog)
    .WaitFor(catalog);

// Promotions microservice — codes promo, backed by PostgreSQL (provisioned as a container by Aspire).
var promotionsDb = builder.AddPostgres("postgres").AddDatabase("promotionsdb");

var promotions = builder.AddProject<Projects.ECommerce_Promotions_Api>("promotions")
    .WithReference(promotionsDb)
    .WaitFor(promotionsDb);

// API Gateway (YARP) — the single entry point for the services.
var gateway = builder.AddProject<Projects.ECommerce_Gateway>("gateway")
    .WithReference(catalog)
    .WithReference(ordering)
    .WithReference(promotions)
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WaitFor(promotions);

// Blazor web frontend — talks to the services through the gateway.
builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();
