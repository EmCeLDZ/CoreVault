using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using CoreKV.Domain.Entities;
using CoreKV.Application.Interfaces;
using CoreKV.Controllers;

namespace CoreKV.Tests.Controllers;

public class KeyValueControllerTests
{
    private readonly Mock<IKeyValueService> _mockKeyValueService;
    private readonly Mock<ILogger<KeyValueController>> _mockLogger;
    private readonly KeyValueController _controller;

    public KeyValueControllerTests()
    {
        _mockKeyValueService = new Mock<IKeyValueService>();
        _mockLogger = new Mock<ILogger<KeyValueController>>();
        
        _controller = new KeyValueController(_mockKeyValueService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Set_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var request = new SetRequest
        {
            Key = "test-key",
            Value = "test-value",
            Namespace = "test",
            TTL = 3600
        };
        
        var expectedKeyValue = new KeyValue
        {
            Key = request.Key,
            Value = request.Value,
            Namespace = request.Namespace,
            TTL = request.TTL
        };

        _mockKeyValueService.Setup(x => x.SetAsync(
            It.IsAny<ApiKey>(), 
            request.Key, 
            request.Value, 
            request.Namespace, 
            request.TTL))
            .ReturnsAsync(expectedKeyValue);

        // Act
        var result = await _controller.Set(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedKeyValue);
        
        _mockKeyValueService.Verify(x => x.SetAsync(
            It.IsAny<ApiKey>(), 
            request.Key, 
            request.Value, 
            request.Namespace, 
            request.TTL), Times.Once);
    }

    [Fact]
    public async Task Get_WithValidKeyAndNamespace_ShouldReturnOkResult()
    {
        // Arrange
        var key = "test-key";
        var @namespace = "test";
        var expectedKeyValue = new KeyValue
        {
            Key = key,
            Value = "test-value",
            Namespace = @namespace
        };

        _mockKeyValueService.Setup(x => x.GetAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace))
            .ReturnsAsync(expectedKeyValue);

        // Act
        var result = await _controller.Get(key, @namespace);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedKeyValue);
        
        _mockKeyValueService.Verify(x => x.GetAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace), Times.Once);
    }

    [Fact]
    public async Task Get_WithNonExistentKey_ShouldReturnNotFound()
    {
        // Arrange
        var key = "non-existent-key";
        var @namespace = "test";

        _mockKeyValueService.Setup(x => x.GetAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace))
            .ReturnsAsync((KeyValue?)null);

        // Act
        var result = await _controller.Get(key, @namespace);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        
        _mockKeyValueService.Verify(x => x.GetAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace), Times.Once);
    }

    [Fact]
    public async Task Delete_WithValidKeyAndNamespace_ShouldReturnOkResult()
    {
        // Arrange
        var key = "test-key";
        var @namespace = "test";

        _mockKeyValueService.Setup(x => x.DeleteAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(key, @namespace);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { success = true });
        
        _mockKeyValueService.Verify(x => x.DeleteAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentKey_ShouldReturnNotFound()
    {
        // Arrange
        var key = "non-existent-key";
        var @namespace = "test";

        _mockKeyValueService.Setup(x => x.DeleteAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(key, @namespace);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        
        _mockKeyValueService.Verify(x => x.DeleteAsync(
            It.IsAny<ApiKey>(), 
            key, 
            @namespace), Times.Once);
    }

    [Fact]
    public async Task List_WithValidNamespace_ShouldReturnOkResult()
    {
        // Arrange
        var @namespace = "test";
        var expectedKeys = new List<string> { "key1", "key2", "key3" };
        var expectedListResponse = new ListResponse { Keys = expectedKeys };

        _mockKeyValueService.Setup(x => x.ListAsync(
            It.IsAny<ApiKey>(), 
            @namespace))
            .ReturnsAsync(expectedListResponse);

        // Act
        var result = await _controller.List(@namespace);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedListResponse);
        
        _mockKeyValueService.Verify(x => x.ListAsync(
            It.IsAny<ApiKey>(), 
            @namespace), Times.Once);
    }
}
