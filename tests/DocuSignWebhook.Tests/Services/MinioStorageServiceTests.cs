using DocuSignWebhook.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocuSignWebhook.Tests.Services;

public class MinioStorageServiceTests
{
    private readonly Mock<ILogger<MinioStorageService>> _mockLogger;
    private readonly string _endpoint = "localhost:9000";
    private readonly string _accessKey = "test-access-key";
    private readonly string _secretKey = "test-secret-key";

    public MinioStorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<MinioStorageService>>();
    }

    [Fact]
    public void Constructor_WithoutSSL_ShouldInitializeService()
    {
        // Act
        var service = new MinioStorageService(
            _mockLogger.Object,
            _endpoint,
            _accessKey,
            _secretKey,
            useSSL: false);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithSSL_ShouldInitializeService()
    {
        // Act
        var service = new MinioStorageService(
            _mockLogger.Object,
            _endpoint,
            _accessKey,
            _secretKey,
            useSSL: true);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_DefaultSSL_ShouldInitializeService()
    {
        // Act
        var service = new MinioStorageService(
            _mockLogger.Object,
            _endpoint,
            _accessKey,
            _secretKey);

        // Assert
        Assert.NotNull(service);
    }
}
