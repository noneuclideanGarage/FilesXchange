using System.Diagnostics;

namespace FilesXchange.API.Helpers.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation(
            "[REQUEST] {Method} {Path} from {RemoteIp}",
            context.Request.Method,
            context.Request.Path,
            remoteIp);

        await _next(context);

        stopwatch.Stop();
        _logger.LogDebug(
            "[RESPONSE] {Method} {Path} -> {StatusCode} in {ElapsedMs} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.Elapsed.TotalMilliseconds);
    }
}