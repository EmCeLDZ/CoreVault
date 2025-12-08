using Microsoft.EntityFrameworkCore;
using CoreVault.Domain.Entities;
using CoreVault.Models;
using ApiKey = CoreVault.Domain.Entities.ApiKey;
using KeyValueItem = CoreVault.Domain.Entities.KeyValueItem;

namespace CoreVault.Data
{
    public class CoreVaultContext : DbContext
    {
        public CoreVaultContext(DbContextOptions<CoreVaultContext> options) : base(options)
        {
        }

        public DbSet<KeyValueItem> KeyValueItems { get; set; } = null!;
        public DbSet<ApiKey> ApiKeys { get; set; } = null!;
        public DbSet<FileStorage> FileStorage { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite key for KeyValueItem
            modelBuilder.Entity<KeyValueItem>()
                .HasKey(k => new { k.Namespace, k.Key });
        }
    }
}
