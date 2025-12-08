using CoreVault.Domain.Entities;

namespace CoreVault.Domain.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        public bool CanAccessNamespace(ApiKey apiKey, string @namespace)
        {
            if (apiKey.Role == ApiKeyRole.Admin)
                return true;

            var allowedNamespaces = GetAllowedNamespaces(apiKey);
            return allowedNamespaces.Contains(@namespace) || allowedNamespaces.Contains("*");
        }

        public bool CanWrite(ApiKey apiKey)
        {
            return apiKey.Role is ApiKeyRole.Admin or ApiKeyRole.ReadWrite;
        }

        public List<string> GetAllowedNamespaces(ApiKey apiKey)
        {
            if (apiKey.Role == ApiKeyRole.Admin)
                return new List<string> { "*" };

            return apiKey.AllowedNamespaces
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ns => ns.Trim())
                .ToList();
        }
    }
}
