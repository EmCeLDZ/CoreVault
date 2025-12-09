using CoreVault.Infrastructure;

namespace CoreVault.Infrastructure.Domain.Interfaces
{
    public interface IApiKeyRepository
    {
        Task<ApiKey?> GetByKeyAsync(string key);
        Task<IEnumerable<ApiKey>> GetAllAsync();
        Task<ApiKey> CreateAsync(ApiKey apiKey);
        Task<bool> ExistsAdminKeyAsync();
    }
}
