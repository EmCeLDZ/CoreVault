using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using CoreKV.Data;
using System.Net.Http.Json;
using CoreKV.Domain.Entities;
using CoreKV.Application.Interfaces;
using CoreKV.Controllers;
using Xunit;

namespace CoreKV.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testApiKey;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CoreKVContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<CoreKVContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });

        _client = _factory.CreateClient();
        _testApiKey = "test-api-key";
    }

    [Fact]
    public async Task SetAndGetKeyValue_ShouldWorkCorrectly()
    {
        // Arrange
        var setValue = "test-value";
        var setRequest = new
        {
            Key = "test-key",
            Value = setValue,
            Namespace = "test",
            TTL = 3600
        };

        // Act - Set
        var setResponse = await _client.PostAsJsonAsync("/api/keyvalue/set", setRequest);
        setResponse.EnsureSuccessStatusCode();

        // Act - Get
        _client.DefaultRequestHeaders.Add("X-API-Key", _testApiKey);
        var getResponse = await _client.GetAsync("/api/keyvalue/get?key=test-key&namespace=test");
        
        // Assert
        getResponse.EnsureSuccessStatusCode();
        var getValue = await getResponse.Content.ReadFromJsonAsync<KeyValue>();
        getValue.Should().NotBeNull();
        getValue!.Value.Should().Be(setValue);
        getKey.Key.Should().Be("test-key");
        getKey.Namespace.Should().Be("test");
    }

    [Fact]
    public async Task DeleteKeyValue_ShouldWorkCorrectly()
    {
        // Arrange
        var setRequest = new
        {
            Key = "delete-test-key",
            Value = "test-value",
            Namespace = "test",
            TTL = 3600
        };

        // Set up key first
        await _client.PostAsJsonAsync("/api/keyvalue/set", setRequest);
        _client.DefaultRequestHeaders.Add("X-API-Key", _testApiKey);

        // Act - Delete
        var deleteResponse = await _client.DeleteAsync("/api/keyvalue/delete?key=delete-test-key&namespace=test");
        
        // Assert
        deleteResponse.EnsureSuccessStatusCode();

        // Verify it's gone
        var getResponse = await _client.GetAsync("/api/keyvalue/get?key=delete-test-key&namespace=test");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListKeys_ShouldReturnCorrectKeys()
    {
        // Arrange
        var keys = new[] { "list-key1", "list-key2", "list-key3" };
        var @namespace = "list-test";

        // Set up keys
        foreach (var key in keys)
        {
            var setRequest = new
            {
                Key = key,
                Value = $"value-for-{key}",
                Namespace = @namespace,
                TTL = 3600
            };
            await _client.PostAsJsonAsync("/api/keyvalue/set", setRequest);
        }

        _client.DefaultRequestHeaders.Add("X-API-Key", _testApiKey);

        // Act
        var listResponse = await _client.GetAsync($"/api/keyvalue/list?namespace={@namespace}");
        
        // Assert
        listResponse.EnsureSuccessStatusCode();
        var listResult = await listResponse.Content.ReadFromJsonAsync<ListResponse>();
        listResult.Should().NotBeNull();
        listResult!.Keys.Should().HaveCount(3);
        listResult.Keys.Should().BeEquivalentTo(keys);
    }

    [Fact]
    public async Task UnauthorizedRequest_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/keyvalue/get?key=test&namespace=test");
        
        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GlobalExceptionHandling_ShouldReturnProblemDetails()
    {
        // Arrange - Send invalid request that might cause exception
        var invalidRequest = new
        {
            Key = "", // Empty key should cause validation error
            Value = "test",
            Namespace = "test",
            TTL = -1 // Negative TTL
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/keyvalue/set", invalidRequest);
        
        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Extensions.Should().ContainKey("correlationId");
    }
}
