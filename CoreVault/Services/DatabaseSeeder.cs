using CoreVault.Domain.Entities;
using CoreVault.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Services
{
    public class DatabaseSeeder
    {
        public static async Task SeedData(CoreVaultContext context)
        {
            // Check if database already has data
            if (await context.ApiKeys.AnyAsync())
            {
                return; // Database already seeded
            }

            // Add sample public data only
            var sampleData = new List<KeyValueItem>
            {
                new KeyValueItem
                {
                    Namespace = "public",
                    Key = "welcome",
                    Value = "Welcome to CoreVault API!"
                },
                new KeyValueItem
                {
                    Namespace = "public",
                    Key = "version",
                    Value = "1.0.0"
                }
            };

            await context.KeyValueItems.AddRangeAsync(sampleData);
            await context.SaveChangesAsync();
        }
    }
}
