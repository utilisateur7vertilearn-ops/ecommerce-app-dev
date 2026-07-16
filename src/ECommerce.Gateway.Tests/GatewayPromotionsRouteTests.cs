using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Gateway.Tests;

// Vérifie que le gateway route bien /promotions/{**catch-all} vers le cluster "promotions" en
// retirant le préfixe — pas un test de Promotions.Api elle-même. Le "downstream" est un
// HttpListener basique (BCL, aucune dépendance) écoutant sur un vrai port ; on redirige la
// destination du cluster "promotions-cluster" dessus via une config de test, en laissant le
// gateway lui-même sur le transport en mémoire habituel de WebApplicationFactory côté entrant.
public class GatewayPromotionsRouteTests : IAsyncLifetime
{
    private HttpListener? _listener;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private string? _capturedMethod;
    private string? _capturedPath;
    private string? _capturedBody;

    public Task InitializeAsync()
    {
        var port = GetFreeTcpPort();
        var prefix = $"http://127.0.0.1:{port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        _ = Task.Run(HandleRequestsAsync);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:promotions-cluster:Destinations:promotions:Address"] = prefix,
                });
            });
        });
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _listener?.Stop();
        _listener?.Close();
        _client?.Dispose();
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PromotionsRoute_StripsPrefix_AndForwardsGetToCluster()
    {
        var response = await _client!.GetAsync("/promotions/api/promotions/BLACKFRIDAY");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("GET", _capturedMethod);
        Assert.Equal("/api/promotions/BLACKFRIDAY", _capturedPath);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BLACKFRIDAY", body);
    }

    [Fact]
    public async Task PromotionsRoute_StripsPrefix_AndForwardsPostBodyToCluster()
    {
        var response = await _client!.PostAsync(
            "/promotions/api/promotions/BLACKFRIDAY/validate",
            new StringContent("""{"amount":80}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("POST", _capturedMethod);
        Assert.Equal("/api/promotions/BLACKFRIDAY/validate", _capturedPath);
        Assert.Equal("""{"amount":80}""", _capturedBody);
    }

    private async Task HandleRequestsAsync()
    {
        while (_listener is { IsListening: true })
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (!_listener.IsListening)
            {
                return;
            }

            _capturedMethod = context.Request.HttpMethod;
            _capturedPath = context.Request.Url!.AbsolutePath;
            using (var reader = new StreamReader(context.Request.InputStream))
            {
                _capturedBody = await reader.ReadToEndAsync();
            }

            var responseBody = Encoding.UTF8.GetBytes("""{"code":"BLACKFRIDAY","valid":true}""");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = responseBody.Length;
            await context.Response.OutputStream.WriteAsync(responseBody);
            context.Response.Close();
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
