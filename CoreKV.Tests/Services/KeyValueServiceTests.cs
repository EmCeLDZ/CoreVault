using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using CoreKV.Data;
using CoreKV.Domain.Entities;
using CoreKV.Domain.Interfaces;
using CoreKV.Domain.Services;
using CoreKV.Application.Services;
using CoreKV.Application.Interfaces;
using Xunit;

namespace CoreKV.Tests.Services;

public class KeyValueServiceTests
{
    private readonly Mock<IKeyValueRepository> _mockRepository;
    private readonly Mock<IAuthorizationService> _mockAuthService;
    private readonly Mock<ILogger<KeyValueService>> _mockLogger;
    private readonly IKeyValueService _keyValueService;

    public KeyValueServiceTests()
    {
        _mockRepository = new Mock<IKeyValueRepository>();
        _mockAuthService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<KeyValueService>>();
        
        _keyValueService = new KeyValueService(
            _mockRepository.Object,
            _mockAuthService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SetAsync_WithValidKeyAndNamespace_ShouldCreateKeyValue()
    {
        // Arrange
        var apiKey = new ApiKey { Key = "test-key", Role = ApiKeyRole.User, AllowedNamespaces = "test" };
        var key = "test-key";
        var value = "test-value";
        var @namespace = "test";
        var ttl = 3600;

        _mockAuthService.Setup(x => x.CanAccessNamespace(apiKey, @namespace))
            .Returns(true);

        // Act
        var result = await _keyValueService.SetAsync(apiKey, key, value, @namespace, ttl);

        // Assert
        result.Should().NotBeNull();
        result.Key.Should().Be(key);
        result.Value.Should().Be(value);
        result.Namespace.Should().Be(@namespace);
        result.TTL.Should().Be(ttl);
        
        _mockRepository.Verify(x => x.CreateAsync(It.IsAny<KeyValue>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithUnauthorizedNamespace_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var apiKey = new ApiKey { Key = "test-key", Role = ApiKeyRole.User, AllowedNamespaces = "other" };
        var key = "test-key";
        var value = "test-value";
        var @namespace = "test";

        _mockAuthService.Setup(x => x.CanAccessNamespace(apiKey, @namespace))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _keyValueService.SetAsync(apiKey, key, value, @namespace));
    }

    [Fact]
    public async Task GetAsync_WithValidKeyAndNamespace_ShouldReturnValue()
    {
        // Arrange
        var apiKey = new ApiKey { Key = "test-key", Role = ApiKeyRole.User, AllowedNamespaces = "test" };
        var key = "test-key";
        var @namespace = "test";
        var expectedKeyValue = new KeyValue
        {
            Key = key,
            Value = "test-value",
            Namespace = @namespace,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockAuthService.Setup(x => x.CanAccessNamespace(apiKey, @namespace))
            .Returns(true);

        _mockRepository.Setup(x => x.GetAsync(key, @namespace))
            .ReturnsAsync(expectedKeyValue);

        // Act
        var result = await _keyValueService.GetAsync(apiKey, key, @namespace);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(key);
        result.Value.Should().Be("test-value");
        
        _mockRepository.Verify(x => x.GetAsync(key, @namespace), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithExpiredKey_ShouldReturnNull()
    {
        // Arrange
        var apiKey = new ApiKey { Key = "test-key", Role = ApiKeyRole.User, AllowedNamespaces = "test" };
        var key = "test-key";
        var @namespace = "test";
        var expiredKeyValue = new KeyValue
        {
            Key = key,
            Value = "test-value",
            Namespace = @namespace,
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired
        };

        _mockAuthService.Setup(x => x.CanAccessNamespace(apiKey, @namespace))
            .Returns(true);

        _mockRepository.Setup(x => x.GetAsync(key, @namespace))
            .ReturnsAsync(expiredKeyValue);

        // Act
        var result = await _keyValueService.GetAsync(apiKey, key, @namespace);

        // Assert
        result.Should().BeNull();
        
        _mockRepository.Verify(x => x.DeleteAsync(key, @namespace), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidKeyAndNamespace_ShouldDeleteKey()
    {
        // Arrange
        var apiKey = new ApiKey { Key = "test-key", Role = ApiKeyRole.User, AllowedNamespaces = "test" };
        var key = "test-key";
        var @namespace = "test";
        var existingKeyValue = new KeyValue
        {
            Key = key,
            Value = "test-value",
            Namespace = @namespace,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockAuthService.Setup(x => x.CanAccessNamespace(apiKey, @namespace))
            .Returns(true);

        _mockRepository.Setup(x => x.GetAsync(key, @namespace))
            .ReturnsAsync(existingKeyValue);

        // Act
        var result = await _keyValueService.DeleteAsync(apiKey, key, @namespace);

        // Assert
        result.Should().BeTrue();
        
        _mockRepository.Verify(x => x.DeleteAsync(key, @namespace), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithValidNamespace_ShouldReturnKeys()
    {
        // Arrange
        var apiKey = new ApiKey { Key = "test-key", Role = ApiKeyRole.User, AllowedNamespaces = "test" };
        var @namespace = "test";
        var expectedKeys = new List<string> { "key1", "key2", "key3" };

        _mockAuthService.Setup(x => x.CanAccessNamespace(apiKey, @namespace))
            .Returns(true);

        _mockRepository.Setup(x => x.ListKeysAsync(@namespace))
            .ReturnsAsync(expectedKeys);

        // Act
        var result = await _keyValueService.ListAsync(apiKey, @namespace);

        // Assert
        result.Should().NotBeNull();
        result.Keys.Should().HaveCount(3);
        result.Keys.Should().BeEquivalentTo(expectedKeys);
        
        _mockRepository.Verify(x => x.ListKeysAsync(@namespace), Times.Once);
    }
}
