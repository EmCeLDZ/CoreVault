using CoreKV.Models;
using CoreKV.Data;

namespace CoreKV.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        
        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task Invoke(HttpContext context)
        {
            // 1. Allow public GET requests (without API key) only for namespace "public"
            if (context.Request.Method == "GET" && 
                !context.Request.Path.StartsWithSegments("/api/admin") &&
                !context.Request.Path.StartsWithSegments("/api/file"))
            {
                // For public GET, set default namespace "public"
                context.Items["UserRole"] = ApiKeyRole.ReadOnly;
                context.Items["AllowedNamespaces"] = new List<string> { "public" };
                await _next(context);
                return;
            }
            
            // 2. For write operations, require API key
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key required for write operations");
                return;
            }
            
            // 3. Check key in database
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreKVContext>();
            var keyRecord = await dbContext.ApiKeys.FindAsync(apiKey.ToString());
            
            if (keyRecord == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }
            
            // 4. Save user data in context
            context.Items["UserRole"] = keyRecord.Role;
            context.Items["AllowedNamespaces"] = keyRecord.AllowedNamespaces
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
            
            await _next(context);
        }
    }
}