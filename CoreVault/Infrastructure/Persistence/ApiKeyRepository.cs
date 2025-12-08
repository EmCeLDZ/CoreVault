using Microsoft.EntityFrameworkCore;
using CoreVault.Domain.Entities;
using CoreVault.Domain.Interfaces;
using CoreVault.Data;

namespace CoreVault.Infrastructure.Persistence
{
    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly CoreVaultContext _context;

        public ApiKeyRepository(CoreVaultContext context)
        {
            _context = context;
        }

        public async Task<ApiKey?> GetByKeyAsync(string key)
        {
            return await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == key);
        }

        public async Task<IEnumerable<ApiKey>> GetAllAsync()
        {
            return await _context.ApiKeys.ToListAsync();
        }

        public async Task<ApiKey> CreateAsync(ApiKey apiKey)
        {
            apiKey.CreatedAt = DateTime.UtcNow;
            
            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();
            
            return apiKey;
        }

        public async Task<bool> ExistsAdminKeyAsync()
        {
            return await _context.ApiKeys
                .AnyAsync(k => k.Role == ApiKeyRole.Admin);
        }
    }
}
