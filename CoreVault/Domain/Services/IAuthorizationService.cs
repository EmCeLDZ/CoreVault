using CoreVault.Domain.Entities;

namespace CoreVault.Domain.Services
{
    public interface IAuthorizationService
    {
        bool CanAccessNamespace(ApiKey apiKey, string @namespace);
        bool CanWrite(ApiKey apiKey);
        List<string> GetAllowedNamespaces(ApiKey apiKey);
    }
}
