using CoreVault.Infrastructure.Logging;
using Serilog;

namespace CoreVault.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var requestId = Guid.NewGuid().ToString("N")[..8];

            // Log request start
            using (ApiLogContext.AddApiContext(context))
            {
                _logger.LogInformation("Request started {RequestId}", requestId);

                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Request failed {RequestId}", requestId);
                    throw;
                }
                finally
                {
                    var duration = DateTime.UtcNow - startTime;
                    var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

                    _logger.Log(logLevel, 
                        "Request completed {RequestId} - Status: {StatusCode} - Duration: {Duration}ms",
                        requestId,
                        context.Response.StatusCode,
                        duration.TotalMilliseconds);
                }
            }
        }
    }
}
