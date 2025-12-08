using CoreKV.Domain.Entities;
using CoreKV.Data;
using CoreKV.Domain.Interfaces;
using Serilog;

namespace CoreKV.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;
        
        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Log.ForContext<ApiKeyMiddleware>();
        }
        
        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine($"[MIDDLEWARE] Request: {context.Request.Method} {context.Request.Path}");
            _logger.Information("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
            
            // 1. Allow public GET requests (without API key) only for namespace "public"
            if (context.Request.Method == "GET" && 
                !context.Request.Path.StartsWithSegments("/api/admin") &&
                (!context.Request.Path.StartsWithSegments("/api/file") ||
                 context.Request.Path.ToString().EndsWith("/public")))
            {
                Console.WriteLine("[MIDDLEWARE] Public GET request allowed");
                _logger.Information("Public GET request allowed");
                // For public GET, set default namespace "public"
                context.Items["UserRole"] = ApiKeyRole.ReadOnly;
                context.Items["AllowedNamespaces"] = new List<string> { "public" };
                await _next(context);
                return;
            }
            
            // 2. For write operations, require API key
            var apiKeyHeader = context.Request.Headers["X-API-Key"].FirstOrDefault() ?? 
                              context.Request.Headers["X-Api-Key"].FirstOrDefault();
            
            Console.WriteLine($"[MIDDLEWARE] API Key header found: {!string.IsNullOrEmpty(apiKeyHeader)}");
            _logger.Information("API Key header found: {HasKey}", !string.IsNullOrEmpty(apiKeyHeader));
            
            if (string.IsNullOrEmpty(apiKeyHeader))
            {
                Console.WriteLine("[MIDDLEWARE] API Key missing");
                _logger.Warning("API Key missing for request: {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key required for write operations");
                return;
            }
            
            // 3. Check key in database
            using var scope = context.RequestServices.CreateScope();
            var apiKeyRepository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
            var keyRecord = await apiKeyRepository.GetByKeyAsync(apiKeyHeader);
            
            Console.WriteLine($"[MIDDLEWARE] API Key validation result: {keyRecord != null}");
            _logger.Information("API Key validation result: {IsValid}", keyRecord != null);
            
            if (keyRecord == null)
            {
                Console.WriteLine("[MIDDLEWARE] Invalid API Key");
                _logger.Warning("Invalid API Key: {Key}", apiKeyHeader.Substring(0, Math.Min(10, apiKeyHeader.Length)) + "...");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }
            
            // 4. Save user data in context
            context.Items["ApiKey"] = keyRecord;
            context.Items["UserRole"] = keyRecord.Role;
            context.Items["AllowedNamespaces"] = keyRecord.AllowedNamespaces
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
            
            Console.WriteLine($"[MIDDLEWARE] API Key validated successfully for role: {keyRecord.Role}");
            _logger.Information("API Key validated successfully for role: {Role}", keyRecord.Role);
            
            await _next(context);
        }
    }
}