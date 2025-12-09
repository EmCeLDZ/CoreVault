using Microsoft.EntityFrameworkCore;
using CoreVault.Infrastructure;
using CoreVault.API.Modules.Security.Entities;
using CoreVault.Infrastructure.Domain.Interfaces;

namespace CoreVault.Infrastructure.Persistence;

/// <summary>
/// Entity Framework repository implementation for vault secrets
/// </summary>
public class VaultRepository : IVaultRepository
{
    private readonly CoreVaultContext _context;

    public VaultRepository(CoreVaultContext context)
    {
        _context = context;
    }

    public async Task<VaultSecret?> GetAsync(string key)
    {
        return await _context.VaultSecrets
            .FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task AddAsync(VaultSecret secret)
    {
        await _context.VaultSecrets.AddAsync(secret);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(VaultSecret secret)
    {
        _context.VaultSecrets.Update(secret);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string key)
    {
        var secret = await GetAsync(key);
        if (secret != null)
        {
            _context.VaultSecrets.Remove(secret);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _context.VaultSecrets
            .AnyAsync(s => s.Key == key);
    }
}
