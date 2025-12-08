using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CoreKV.Models;
using CoreKV.Domain.Entities;
using ApiKey = CoreKV.Domain.Entities.ApiKey;
using KeyValueItem = CoreKV.Domain.Entities.KeyValueItem;

namespace CoreKV.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequireNamespaceAttribute : TypeFilterAttribute
    {
        public RequireNamespaceAttribute() : base(typeof(RequireNamespaceFilter))
        {
            Arguments = new object[] { };
        }
    }

    public class RequireNamespaceFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var allowedNamespaces = httpContext.Items["AllowedNamespaces"] as List<string> ?? new();
            var userRole = httpContext.Items["UserRole"] as ApiKeyRole?;

            // Admin ma dostęp do wszystkiego
            if (userRole == ApiKeyRole.Admin)
            {
                await next();
                return;
            }

            // Pobierz namespace z query string lub body
            var namespaceFromQuery = httpContext.Request.Query["namespace"].FirstOrDefault();
            var namespaceFromBody = context.ActionArguments.Values.OfType<KeyValueItem>().FirstOrDefault()?.Namespace;

            var targetNamespace = namespaceFromQuery ?? namespaceFromBody ?? "public";

            // Sprawdź dostęp do namespace
            if (!allowedNamespaces.Contains(targetNamespace) && !allowedNamespaces.Contains("*"))
            {
                context.Result = new ForbidResult("Access to this namespace is forbidden");
                return;
            }

            await next();
        }
    }
}
