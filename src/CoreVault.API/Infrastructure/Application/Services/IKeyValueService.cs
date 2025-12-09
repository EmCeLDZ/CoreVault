using CoreVault.Infrastructure;
using CoreVault.API.Modules.KeyValue.Entities;
using CoreVault.Infrastructure.Application.DTOs;

namespace CoreVault.Infrastructure.Application.Services
{
    public interface IKeyValueService
    {
        Task<IEnumerable<KeyValueItem>> GetAllAsync(string? @namespace = null, ApiKey? apiKey = null);
        Task<KeyValueItem?> GetByKeyAsync(string @namespace, string key, ApiKey apiKey);
        Task<KeyValueItem> CreateAsync(CreateKeyValueRequest request, ApiKey apiKey);
        Task<KeyValueItem> UpdateAsync(string @namespace, string key, UpdateKeyValueRequest request, ApiKey apiKey);
        Task DeleteAsync(string @namespace, string key, ApiKey apiKey);
    }
}
