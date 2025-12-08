using Microsoft.EntityFrameworkCore;
using CoreKV.Data;
using CoreKV.Middleware;
using CoreKV.Services;
using CoreKV.Domain.Entities;
using CoreKV.Domain.Interfaces;
using CoreKV.Infrastructure.Persistence;
using CoreKV.Domain.Services;
using CoreKV.Application.Services;
using CoreKV.Application.Interfaces;
using CoreKV.Infrastructure.Services;
using CoreKV.Filters;
using CoreKV.Infrastructure.Logging;
using Microsoft.AspNetCore.Http.Features;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    LoggingConfiguration.ConfigureSerilog(context.Configuration));

// Add DbContext with SQLite
builder.Services.AddDbContext<CoreKVContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Register domain services
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

// Register application services
builder.Services.AddScoped<IKeyValueService, KeyValueService>();
builder.Services.AddScoped<IFileStorageManagementService, FileStorageManagementService>();

// Register infrastructure services
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Register logging services
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "CoreKV API", 
        Version = "v1",
        Description = "A simple Key-Value store with namespace-based access control"
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreKV API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger at root
});

// Add global exception handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Create database and initialize data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CoreKVContext>();
    context.Database.EnsureCreated();
    
    // Skip seeding data in test environment completely
    var env = app.Environment;
    if (env.IsDevelopment() || env.IsProduction())
    {
        // Initialize data
        await DatabaseSeeder.SeedData(context);
        
        // Create admin key if none exists
        var apiKeyRepository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
        if (!await apiKeyRepository.ExistsAdminKeyAsync())
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
            
            await apiKeyRepository.CreateAsync(apiKey);
            
            Console.WriteLine($"=== ADMIN KEY CREATED ===");
            Console.WriteLine($"Key: {adminKey}");
            Console.WriteLine("========================");
            Console.WriteLine("Save this key securely! It won't be shown again.");
        }
    }
    // In test environment, do nothing - tests will handle their own data
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
