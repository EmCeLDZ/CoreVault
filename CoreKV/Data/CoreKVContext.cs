using Microsoft.EntityFrameworkCore;
using CoreKV.Domain.Entities;

namespace CoreKV.Data
{
    public class CoreKVContext : DbContext
    {
        public CoreKVContext(DbContextOptions<CoreKVContext> options) : base(options)
        {
        }

        public DbSet<KeyValueItem> KeyValueItems { get; set; } = null!;
        public DbSet<ApiKey> ApiKeys { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite key for KeyValueItem
            modelBuilder.Entity<KeyValueItem>()
                .HasKey(k => new { k.Namespace, k.Key });
        }
    }
}
