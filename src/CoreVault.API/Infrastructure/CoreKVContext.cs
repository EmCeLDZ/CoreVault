using Microsoft.EntityFrameworkCore;
using CoreVault.Infrastructure;
using CoreVault.API.Modules.KeyValue.Entities;
using CoreVault.API.Modules.Security.Entities;
using CoreVault.Models;

namespace CoreVault.Infrastructure
{
    public class CoreVaultContext : DbContext
    {
        public CoreVaultContext(DbContextOptions<CoreVaultContext> options) : base(options)
        {
        }

        public DbSet<KeyValueItem> KeyValueItems { get; set; } = null!;
        public DbSet<ApiKey> ApiKeys { get; set; } = null!;
        public DbSet<FileStorage> FileStorage { get; set; } = null!;
        public DbSet<VaultSecret> VaultSecrets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite key for KeyValueItem
            modelBuilder.Entity<KeyValueItem>()
                .HasKey(k => new { k.Namespace, k.Key });
                
            // Configure VaultSecret entity
            modelBuilder.Entity<VaultSecret>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Key).HasMaxLength(255);
                entity.Property(e => e.Ciphertext).IsRequired();
                entity.Property(e => e.Nonce).IsRequired();
                entity.Property(e => e.Salt).IsRequired();
                entity.Property(e => e.Tag).IsRequired();
            });
            
            // Konfiguracja encji ApiKey
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Key).HasMaxLength(255);
                entity.Property(e => e.AllowedNamespaces).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.Role).IsRequired();
            });
        }
    }
}
