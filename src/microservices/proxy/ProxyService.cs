using Microsoft.AspNetCore.Http.Features;

namespace Proxy;

public class ProxyService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProxyService> _logger;
    private static readonly HashSet<string> HopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailer", "Transfer-Encoding", "Upgrade"
    };

    public ProxyService(HttpClient client, IConfiguration configuration, ILogger<ProxyService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ForwardAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var target = _configuration["MONOLITH_URL"] ?? "http://monolith:8080";
        if (path.StartsWithSegments("/api/events"))
            target = _configuration["EVENTS_SERVICE_URL"] ?? "http://events-service:8082";
        else if (path.StartsWithSegments("/api/movies"))
        {
            var gradual = bool.TryParse(_configuration["GRADUAL_MIGRATION"], out var enabled) && enabled;
            var percent = int.TryParse(_configuration["MOVIES_MIGRATION_PERCENT"], out var value)
                ? Math.Clamp(value, 0, 100) : 0;
            // При выключенном флаге переход полностью завершён.
            if (!gradual || Random.Shared.Next(100) < percent)
                target = _configuration["MOVIES_SERVICE_URL"] ?? "http://movies-service:8081";
        }

        var uri = new Uri(target.TrimEnd('/') + context.Request.PathBase + path + context.Request.QueryString);
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), uri);
        if (context.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true)
            request.Content = new StreamContent(context.Request.Body);

        var excluded = new HashSet<string>(HopHeaders, StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Request.Headers.Connection.ToString().Split(',', StringSplitOptions.TrimEntries))
            excluded.Add(header);
        foreach (var header in context.Request.Headers)
        {
            if (excluded.Contains(header.Key) || header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        _logger.LogInformation("{Method} {Path} -> {Target}", context.Request.Method, path, target);
        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;
            excluded = new HashSet<string>(HopHeaders, StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers.Connection)
                excluded.Add(header);
            foreach (var header in response.Headers.Concat(response.Content.Headers))
                if (!excluded.Contains(header.Key))
                    context.Response.Headers[header.Key] = header.Value.ToArray();
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
        {
            _logger.LogError(error, "Ошибка обращения к {Target}", target);
            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = 502;
                await context.Response.WriteAsJsonAsync(new { error = "Upstream service is unavailable" });
            }
        }
    }
}
