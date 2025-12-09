using System.Net.Http.Json;
using CoreVault.API;
using CoreVault.Infrastructure;
using CoreVault.API.Modules.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Xunit;

#nullable disable

namespace CoreVault.Tests;

[Trait("Category", "Security")]
public class VaultSecurityTests : IAsyncLifetime
{
    private HttpClient _client;
    private WebApplicationFactory<Program> _factory;
    private CoreVaultContext _dbContext;
    private static SqliteConnection _sharedConnection;
    private const string TestPassphrase = "test-secure-passphrase-123";

    public async Task InitializeAsync()
    {
        _sharedConnection = new SqliteConnection("DataSource=:memory:");
        await _sharedConnection.OpenAsync();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CoreVaultContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<CoreVaultContext>(options =>
                    {
                        options.UseSqlite(_sharedConnection);
                    });
                });
                
                builder.UseEnvironment("Testing");
            });

        using var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<CoreVaultContext>();
        _dbContext.Database.EnsureCreated();
        
        var testApiKey = new ApiKey
        {
            Key = "vault-test-key",
            Role = ApiKeyRole.Admin,
            AllowedNamespaces = "*",
            Description = "Test API key for vault security tests",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ApiKeys.Add(testApiKey);
        await _dbContext.SaveChangesAsync();

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", "vault-test-key");
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _dbContext?.Dispose();
        if (_sharedConnection != null)
        {
            await _sharedConnection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Vault_SetSecret_WithoutPassphrase_ShouldReturnBadRequest()
    {
        var request = new { Key = "test-key", Value = "test-value" };
        
        var response = await _client.PostAsJsonAsync("/api/security/vault", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("X-Vault-Passphrase header is required");
    }

    [Fact]
    public async Task Vault_GetSecret_WithoutPassphrase_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/security/vault/test-key");
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("X-Vault-Passphrase header is required");
    }

    [Fact]
    public async Task Vault_SetAndGetSecret_WithCorrectPassphrase_ShouldWork()
    {
        var request = new { Key = "integration-test-key", Value = "super-secret-data" };
        _client.DefaultRequestHeaders.Add("X-Vault-Passphrase", TestPassphrase);
        
        var setResponse = await _client.PostAsJsonAsync("/api/security/vault", request);
        setResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var getResponse = await _client.GetAsync("/api/security/vault/integration-test-key");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var result = await getResponse.Content.ReadFromJsonAsync<object>();
        result.Should().NotBeNull();
        
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain("super-secret-data");
    }

    [Fact]
    public async Task Vault_GetSecret_WithWrongPassphrase_ShouldReturnUnauthorized()
    {
        var setRequest = new { Key = "wrong-pass-test", Value = "secret-data" };
        _client.DefaultRequestHeaders.Add("X-Vault-Passphrase", TestPassphrase);
        
        var setResponse = await _client.PostAsJsonAsync("/api/security/vault", setRequest);
        setResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        _client.DefaultRequestHeaders.Remove("X-Vault-Passphrase");
        _client.DefaultRequestHeaders.Add("X-Vault-Passphrase", "wrong-passphrase");
        
        var getResponse = await _client.GetAsync("/api/security/vault/wrong-pass-test");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid passphrase or data integrity check failed");
    }

    [Fact]
    public async Task Vault_GetNonExistentSecret_ShouldReturnNotFound()
    {
        _client.DefaultRequestHeaders.Add("X-Vault-Passphrase", TestPassphrase);
        
        var response = await _client.GetAsync("/api/security/vault/nonexistent-key");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task Vault_DeleteSecret_ShouldWork()
    {
        var request = new { Key = "delete-test-key", Value = "to-be-deleted" };
        _client.DefaultRequestHeaders.Add("X-Vault-Passphrase", TestPassphrase);
        
        var setResponse = await _client.PostAsJsonAsync("/api/security/vault", request);
        setResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var deleteResponse = await _client.DeleteAsync("/api/security/vault/delete-test-key");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var getResponse = await _client.GetAsync("/api/security/vault/delete-test-key");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Vault_CheckSecretExists_ShouldWork()
    {
        var request = new { Key = "exists-test-key", Value = "test-data" };
        _client.DefaultRequestHeaders.Add("X-Vault-Passphrase", TestPassphrase);
        
        var existsBefore = await _client.GetAsync("/api/security/vault/exists-test-key/exists");
        existsBefore.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var beforeContent = await existsBefore.Content.ReadAsStringAsync();
        beforeContent.Should().Contain("\"exists\":false");
        
        var setResponse = await _client.PostAsJsonAsync("/api/security/vault", request);
        setResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var existsAfter = await _client.GetAsync("/api/security/vault/exists-test-key/exists");
        existsAfter.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var afterContent = await existsAfter.Content.ReadAsStringAsync();
        afterContent.Should().Contain("\"exists\":true");
    }
}
