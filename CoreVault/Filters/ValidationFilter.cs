using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace CoreVault.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Validate namespace parameter
            if (context.ActionArguments.TryGetValue("namespace", out var nsValue) && nsValue is string namespaceParam)
            {
                if (string.IsNullOrWhiteSpace(namespaceParam))
                {
                    context.Result = new BadRequestObjectResult(new { error = "Namespace cannot be empty" });
                    return;
                }

                if (namespaceParam.Length > 100)
                {
                    context.Result = new BadRequestObjectResult(new { error = "Namespace too long (max 100 characters)" });
                    return;
                }

                if (!IsValidNamespace(namespaceParam))
                {
                    context.Result = new BadRequestObjectResult(new { error = "Invalid namespace format" });
                    return;
                }
            }

            // Validate key parameter
            if (context.ActionArguments.TryGetValue("key", out var keyValue) && keyValue is string keyParam)
            {
                if (string.IsNullOrWhiteSpace(keyParam))
                {
                    context.Result = new BadRequestObjectResult(new { error = "Key cannot be empty" });
                    return;
                }

                if (keyParam.Length > 200)
                {
                    context.Result = new BadRequestObjectResult(new { error = "Key too long (max 200 characters)" });
                    return;
                }
                
                if (!IsValidKey(keyParam))
                {
                    context.Result = new BadRequestObjectResult(new { error = "Invalid key format" });
                    return;
                }
            }

            await next();
        }

        private static bool IsValidNamespace(string ns)
        {
            // Sprawdzamy czy nazwa zawiera tylko dozwolone znaki
            return ns.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.');
        }
        
        private static bool IsValidKey(string key)
        {
            // Sprawdzamy czy klucz zawiera tylko dozwolone znaki
            return key.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.');
        }
    }
}
