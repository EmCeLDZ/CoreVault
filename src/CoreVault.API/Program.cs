using Microsoft.EntityFrameworkCore;
using CoreVault.Infrastructure;
using CoreVault.Middleware;
using CoreVault.API.Modules.KeyValue;
using CoreVault.API.Modules.Storage;
using CoreVault.API.Modules.Security;
using CoreVault.Infrastructure.Application.Services;
using CoreVault.Infrastructure.Application.Interfaces;
using CoreVault.Infrastructure.Domain.Interfaces;
using CoreVault.Infrastructure.Domain.Services;
using CoreVault.Infrastructure.Persistence;
using CoreVault.Infrastructure.Services;
using CoreVault.Infrastructure.Filters;
using CoreVault.Infrastructure.Logging;
using Microsoft.AspNetCore.Http.Features;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    LoggingConfiguration.ConfigureSerilog(context.Configuration));

// Add DbContext with SQLite
builder.Services.AddDbContext<CoreVaultContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Ignoruj ostrzeżenia o niezgodnościach modelu
    options.ConfigureWarnings(warnings => 
    {
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
    });
});

// Add services to DI container
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure form options for file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 209715200; // 200MB
    options.ValueLengthLimit = 209715200;
    options.BufferBody = false;
});

// Register repositories
builder.Services.AddScoped<IKeyValueRepository, KeyValueRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IVaultRepository, VaultRepository>();

// Register domain services
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

// Register application services
builder.Services.AddScoped<IKeyValueService, KeyValueService>();
builder.Services.AddScoped<IFileStorageManagementService, FileStorageManagementService>();
builder.Services.AddScoped<IVaultService, VaultService>();

// Register infrastructure services
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ISecureFileService, SecureFileService>();

// Register logging services
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "CoreVault API", 
        Version = "v1",
        Description = "A comprehensive vault platform with KV storage, file management, and security features"
    });
    
    c.AddSecurityDefinition("ApiKey", new()
    {
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "API Key authentication"
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreVault API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger at root
});

// Add global exception handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    Console.WriteLine("Starting database initialization...");
    var context = scope.ServiceProvider.GetRequiredService<CoreVaultContext>();
    
    try
    {
        // Create database if it doesn't exist and apply all migrations
        Console.WriteLine("Ensuring database is created...");
        bool created = await context.Database.EnsureCreatedAsync();
        Console.WriteLine($"Database created: {created}");
        
        // Check if database exists
        bool canConnect = await context.Database.CanConnectAsync();
        Console.WriteLine($"Can connect to database: {canConnect}");
        
        // If database was just created, seed initial data
        if (created && canConnect)
        {
            var env = app.Environment;
            Console.WriteLine("Environment: " + env.EnvironmentName);
            
            // Initialize data - only if database is empty
            try
            {
                Console.WriteLine("Seeding initial data...");
                await DatabaseSeeder.SeedData(context);
                Console.WriteLine("Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seeding failed: {ex}");
            }
            
            // Create admin key if none exists
            try
            {
                Console.WriteLine("Checking for admin key...");
                var apiKeyRepository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
                if (!await apiKeyRepository.ExistsAdminKeyAsync())
                {
                    Console.WriteLine("No admin key found. Generating new one...");
                    var adminKey = ApiKeyGenerator.GenerateApiKey();
                    var apiKey = new ApiKey
                    {
                        Key = adminKey,
                        Role = ApiKeyRole.Admin,
                        AllowedNamespaces = "*",
                        Description = "Default admin key",
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await apiKeyRepository.CreateAsync(apiKey);
                    
                    Console.WriteLine($"=== ADMIN KEY CREATED ===");
                    Console.WriteLine($"Key: {adminKey}");
                    Console.WriteLine("========================");
                    Console.WriteLine("Save this key securely! It won't be shown again.");
                }
                else
                {
                    Console.WriteLine("Admin key already exists");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin key creation failed: {ex}");
            }
        }
        // In test environment, do nothing - tests will handle their own data
        else
        {
            Console.WriteLine("Skipping data seeding in " + app.Environment.EnvironmentName + " environment");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex}");
        throw; // Rethrow to fail fast
    }
}

// Configure middleware pipeline
// app.UseHttpsRedirection(); // Wyłączone na potrzeby testów
app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
