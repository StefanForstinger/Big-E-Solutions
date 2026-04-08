using System.Collections.Concurrent;

namespace ProjectPlanner.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, List<DateTime>> _store;
    private readonly ReaderWriterLockSlim _lockObj = new();

    public RateLimitingMiddleware(RequestDelegate next, Dictionary<string, List<DateTime>> rateLimitStore)
    {
        _next = next;
        _store = rateLimitStore;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "";

        // ✅ Rate limits pro Endpoint
        var limits = new Dictionary<string, (int count, int minutes)>
        {
            { "/api/auth/login", (5, 15) },              // 5 attempts in 15 minutes
            { "/api/auth/create-user", (20, 60) },       // 20 requests in 60 minutes
            { "/api/auth/change-password", (10, 60) }    // 10 requests in 60 minutes
        };

        var (maxCount, minutes) = limits.ContainsKey(path) 
            ? limits[path] 
            : (100, 1); // Default: 100 requests per minute

        var key = $"{clientIp}:{path}";

        // ✅ Check rate limit (without lock for async)
        _lockObj.EnterWriteLock();
        try
        {
            if (!_store.ContainsKey(key))
                _store[key] = new List<DateTime>();

            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-minutes);

            // ✅ Remove old requests outside the window
            _store[key].RemoveAll(t => t < windowStart);

            // ✅ Check if limit exceeded
            if (_store[key].Count >= maxCount)
            {
                _lockObj.ExitWriteLock();
                
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Zu viele Anfragen. Bitte versuchen Sie es später erneut.",
                    retryAfter = minutes * 60
                });
                return;
            }

            // ✅ Add current request
            _store[key].Add(now);
        }
        finally
        {
            _lockObj.ExitWriteLock();
        }

        await _next(context);
    }
}

public static class RateLimitingExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}