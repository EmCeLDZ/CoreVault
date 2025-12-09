using CoreVault.API.Modules.KeyValue.Entities;

namespace CoreVault.Infrastructure.Domain.Interfaces
{
    public interface IKeyValueRepository
    {
        Task<IEnumerable<KeyValueItem>> GetAllAsync(string? @namespace = null);
        Task<KeyValueItem?> GetByKeyAsync(string @namespace, string key);
        Task<bool> ExistsAsync(string @namespace, string key);
        Task<KeyValueItem> CreateAsync(KeyValueItem item);
        Task<KeyValueItem> UpdateAsync(KeyValueItem item);
        Task DeleteAsync(string @namespace, string key);
    }
}
