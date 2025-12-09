using CoreVault.Infrastructure;
using CoreVault.API.Modules.KeyValue.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.Sqlite;

namespace CoreVault.Infrastructure.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedData(CoreVaultContext context)
        {
            if (context == null) 
                throw new ArgumentNullException(nameof(context));
            
            Console.WriteLine("Seeding initial data...");
            
            try
            {
                // Sprawdźmy, czy tabela KeyValueItems istnieje
                var tableExists = await TableExistsAsync(context, "KeyValueItems");
                if (!tableExists)
                {
                    Console.WriteLine("KeyValueItems table does not exist. Skipping data seeding.");
                    return;
                }

                // Sprawdźmy, czy już są jakieś dane
                var hasData = await context.KeyValueItems.AnyAsync();
                if (hasData)
                {
                    Console.WriteLine("Database already contains data. Skipping seeding.");
                    return;
                }

                // Dodaj przykładowe dane publiczne
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
                
                Console.WriteLine("Successfully seeded initial data.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during data seeding: {ex}");
                throw;
            }
        }
        
        private static async Task<bool> TableExistsAsync(CoreVaultContext context, string tableName)
        {
            try
            {
                var connection = context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@name";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);
                
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();
                
                var result = await command.ExecuteScalarAsync();
                return result != null && result.ToString() == tableName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if table {tableName} exists: {ex.Message}");
                return false;
            }
        }
    }
}
