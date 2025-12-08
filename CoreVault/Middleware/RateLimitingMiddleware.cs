using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace CoreVault.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ConcurrentDictionary<string, DateTime> _requestTimes;
        private readonly int _maxRequests = 100;
        private readonly TimeSpan _window = TimeSpan.FromMinutes(1);

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
            _requestTimes = new ConcurrentDictionary<string, DateTime>();
        }

        public async Task Invoke(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Clean old entries
            var cutoff = DateTime.UtcNow - _window;
            var oldKeys = _requestTimes.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
            foreach (var key in oldKeys)
            {
                _requestTimes.TryRemove(key, out _);
            }

            // Count recent requests
            var recentCount = _requestTimes.Count(kvp => kvp.Value >= cutoff);
            
            if (recentCount >= _maxRequests)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            _requestTimes[clientIp + "_" + DateTime.UtcNow.Ticks] = DateTime.UtcNow;
            await _next(context);
        }
    }
}
