using CoreVault.Infrastructure;
using CoreVault.Infrastructure.Logging;
using CoreVault.Infrastructure.Domain.Interfaces;
using Serilog;

namespace CoreVault.Middleware
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
            try
            {
                Console.WriteLine($"[MIDDLEWARE] Request: {context.Request.Method} {context.Request.Path}");
                _logger.Information("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
                
                // 1. Allow public GET requests only for specific endpoints
                if (context.Request.Method == "GET" && 
                    context.Request.Path.StartsWithSegments("/api/file/public"))
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
                var apiKeyHeader = context.Request.Headers["X-API-Key"].FirstOrDefault();
                
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
                Console.WriteLine("[MIDDLEWARE] About to get API key from repository");
                Console.Out.Flush();
                var keyRecord = await apiKeyRepository.GetByKeyAsync(apiKeyHeader);
                Console.WriteLine($"[MIDDLEWARE] API Key validation result: {keyRecord != null}");
                Console.Out.Flush();
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
                
                var allowedNamespaces = keyRecord.AllowedNamespaces == "*" 
                    ? new List<string> { "*" }
                    : keyRecord.AllowedNamespaces
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();
                
                context.Items["AllowedNamespaces"] = allowedNamespaces;
                
                Console.WriteLine($"[MIDDLEWARE] API Key validated successfully for role: {keyRecord.Role}");
                Console.WriteLine($"[MIDDLEWARE] AllowedNamespaces: {string.Join(", ", allowedNamespaces)}");
                Console.Out.Flush();
                _logger.Information("API Key validated successfully for role: {Role}", keyRecord.Role);
                _logger.Information("AllowedNamespaces: {Namespaces}", string.Join(", ", allowedNamespaces));
                
                Console.WriteLine("[MIDDLEWARE] About to call next middleware");
                Console.Out.Flush();
                await _next(context);
                Console.WriteLine("[MIDDLEWARE] Returned from next middleware");
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MIDDLEWARE] Exception: {ex.Message}");
                Console.WriteLine($"[MIDDLEWARE] Stack Trace: {ex.StackTrace}");
                Console.Out.Flush();
                throw;
            }
        }
    }
}