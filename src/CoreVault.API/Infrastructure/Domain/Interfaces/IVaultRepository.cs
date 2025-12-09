using CoreVault.API.Modules.Security.Entities;

namespace CoreVault.Infrastructure.Domain.Interfaces;

/// <summary>
/// Repository interface for vault secrets operations
/// </summary>
public interface IVaultRepository
{
    Task<VaultSecret?> GetAsync(string key);
    Task AddAsync(VaultSecret secret);
    Task UpdateAsync(VaultSecret secret);
    Task DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
}
