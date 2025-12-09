using Microsoft.EntityFrameworkCore;
using CoreVault.API.Modules.KeyValue.Entities;
using CoreVault.Infrastructure.Domain.Interfaces;
using CoreVault.Infrastructure;

namespace CoreVault.Infrastructure.Persistence
{
    public class KeyValueRepository : IKeyValueRepository
    {
        private readonly CoreVaultContext _context;

        public KeyValueRepository(CoreVaultContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<KeyValueItem>> GetAllAsync(string? @namespace = null)
        {
            var query = _context.KeyValueItems.AsQueryable();
            
            if (!string.IsNullOrEmpty(@namespace))
            {
                query = query.Where(x => x.Namespace == @namespace);
            }

            return await query.ToListAsync();
        }

        public async Task<KeyValueItem?> GetByKeyAsync(string @namespace, string key)
        {
            return await _context.KeyValueItems
                .FirstOrDefaultAsync(x => x.Namespace == @namespace && x.Key == key);
        }

        public async Task<bool> ExistsAsync(string @namespace, string key)
        {
            return await _context.KeyValueItems
                .AnyAsync(x => x.Namespace == @namespace && x.Key == key);
        }

        public async Task<KeyValueItem> CreateAsync(KeyValueItem item)
        {
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            
            _context.KeyValueItems.Add(item);
            await _context.SaveChangesAsync();
            
            return item;
        }

        public async Task<KeyValueItem> UpdateAsync(KeyValueItem item)
        {
            item.UpdatedAt = DateTime.UtcNow;
            
            _context.KeyValueItems.Update(item);
            await _context.SaveChangesAsync();
            
            return item;
        }

        public async Task DeleteAsync(string @namespace, string key)
        {
            var item = await GetByKeyAsync(@namespace, key);
            if (item != null)
            {
                _context.KeyValueItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
