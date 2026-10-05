using Proxy;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{builder.Configuration["PORT"] ?? "8000"}");
builder.Services.AddHttpClient<ProxyService>().ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });

var app = builder.Build();
app.MapGet("/health", () => Results.Text("Strangler Fig Proxy is healthy"));
app.MapFallback(context => context.RequestServices.GetRequiredService<ProxyService>().ForwardAsync(context));
app.Run();
