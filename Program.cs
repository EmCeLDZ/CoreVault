using Microsoft.EntityFrameworkCore;
using CoreKV.Data;
using CoreKV.Middleware;
using CoreKV.Services;
using CoreKV.Models;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with SQLite
builder.Services.AddDbContext<CoreKVContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to DI container
builder.Services.AddControllers();

var app = builder.Build();

// Add API Key middleware
app.UseMiddleware<ApiKeyMiddleware>();

// Create database and initialize data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CoreKVContext>();
    context.Database.EnsureCreated();
    
    // Initialize data
    await DatabaseSeeder.SeedData(context);
    
    // Create admin key if none exists
    if (!await context.ApiKeys.AnyAsync(k => k.Role == ApiKeyRole.Admin))
    {
        var adminKey = ApiKeyGenerator.GenerateSecureApiKey();
        var apiKey = new ApiKey
        {
            Key = adminKey,
            Role = ApiKeyRole.Admin,
            AllowedNamespaces = "*",
            Description = "Default admin key",
            CreatedAt = DateTime.UtcNow
        };
        
        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync();
        
        Console.WriteLine($"=== ADMIN KEY CREATED ===");
        Console.WriteLine($"Key: {adminKey}");
        Console.WriteLine("========================");
        Console.WriteLine("Save this key securely! It won't be shown again.");
    }
}

// Configure middleware pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
