using System.Net;
using System.Net.Http.Json;
using CoreKV;
using CoreKV.Data;
using CoreKV.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreKV.Tests;

[Trait("Category", "Integration")]
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly CoreKVContext _dbContext;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CoreKVContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<CoreKVContext>(options =>
                {
                    options.UseSqlite("DataSource=:memory:");
                });

                // Create the service provider and initialize database
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<CoreKVContext>();
                    
                    // Ensure database is created with full schema
                    db.Database.OpenConnection();
                    db.Database.EnsureCreated();
                    
                    // Apply all migrations to ensure complete schema
                    if (db.Database.GetPendingMigrations().Any())
                    {
                        db.Database.Migrate();
                    }
                    
                    // Seed test API key
                    var testApiKey = new ApiKey
                    {
                        Key = "test-key-for-ci",
                        Role = ApiKeyRole.ReadWrite,
                        AllowedNamespaces = "*",
                        Description = "Test API key for CI",
                        CreatedAt = DateTime.UtcNow
                    };
                    db.ApiKeys.Add(testApiKey);
                    db.SaveChanges();
                }
            });
            
            // Set environment to Testing
            builder.UseEnvironment("Testing");
        });

        // Get database context for cleanup
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<CoreKVContext>();

        // Tworzymy wirtualnego klienta HTTP (jak przeglądarka/Postman)
        _client = _factory.CreateClient();
        
        // Dodajemy klucz API do nagłówków
        _client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-for-ci");
    }

    public void Dispose()
    {
        _dbContext?.Database?.CloseConnection();
        _dbContext?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        // Act (Działanie): Pytamy o klucz, którego nie ma
        var response = await _client.GetAsync("/api/keyvalue/nieistnieje");

        // Assert (Sprawdzenie): Powinniśmy dostać 404 Not Found
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_And_Get_ShouldReturnSavedValue()
    {
        // Arrange: Dane testowe
        var key = "moj-klucz-testowy";
        var value = "tajna-wartosc";
        var namespaceName = "test";
        
        // Act 1: Zapisz wartość (POST) - poprawne dane z namespace
        var postData = new { Namespace = namespaceName, Key = key, Value = value };
        var postResponse = await _client.PostAsJsonAsync("/api/keyvalue", postData);
        
        // Assert 1: Zapis powinien się udać (200 OK lub 201 Created)
        postResponse.EnsureSuccessStatusCode();

        // Act 2: Odczytaj wartość (GET) z poprawną ścieżką
        var getResponse = await _client.GetAsync($"/api/keyvalue/{namespaceName}/{key}");
        
        // Assert 2: Sprawdź czy dostaliśmy to, co zapisaliśmy
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain(value);
    }
}
