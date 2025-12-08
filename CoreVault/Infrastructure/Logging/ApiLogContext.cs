using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace CoreVault.Infrastructure.Logging
{
    public static class ApiLogContext
    {
        public static IDisposable AddApiContext(HttpContext context)
        {
            return LogContext.Push(new ApiLogEnricher(context));
        }
    }

    public class ApiLogEnricher : ILogEventEnricher
    {
        private readonly HttpContext _context;
        private readonly string _apiKey;
        private readonly string _userRole;
        private readonly string _endpoint;

        public ApiLogEnricher(HttpContext context)
        {
            _context = context;
            _apiKey = GetApiKey();
            _userRole = GetUserRole();
            _endpoint = $"{context.Request.Method} {context.Request.Path}";
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApiKey", _apiKey));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserRole", _userRole));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Endpoint", _endpoint));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RemoteIP", _context.Connection.RemoteIpAddress?.ToString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserAgent", _context.Request.Headers["User-Agent"].ToString()));
        }

        private string GetApiKey()
        {
            if (_context.Items.ContainsKey("ApiKey"))
            {
                var apiKey = _context.Items["ApiKey"] as CoreVault.Domain.Entities.ApiKey;
                return apiKey?.Key?.Substring(0, 8) + "..." ?? "unknown";
            }
            return "none";
        }

        private string GetUserRole()
        {
            if (_context.Items.ContainsKey("UserRole"))
            {
                return _context.Items["UserRole"]?.ToString() ?? "unknown";
            }
            return "anonymous";
        }
    }
}
