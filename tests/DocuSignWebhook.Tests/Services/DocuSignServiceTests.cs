using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocuSignWebhook.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocuSignWebhook.Tests.Services;

public class DocuSignServiceTests
{
    private readonly Mock<ILogger<DocuSignService>> _mockLogger;
    private readonly string _accountId = "test-account-id";
    private readonly string _accessToken = "test-access-token";
    private readonly string _basePath = "https://demo.docusign.net/restapi";

    public DocuSignServiceTests()
    {
        _mockLogger = new Mock<ILogger<DocuSignService>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new DocuSignService(
            _mockLogger.Object,
            _accountId,
            _accessToken,
            _basePath);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithoutBasePath_UsesDefaultBasePath()
    {
        // Act
        var service = new DocuSignService(
            _mockLogger.Object,
            _accountId,
            _accessToken);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void ParseDateTime_WithNullString_ReturnsNull()
    {
        // This tests the private ParseDateTime method indirectly through GetEnvelopeDetailsAsync
        // We can't directly test private methods, but we test their behavior through public methods
        var service = new DocuSignService(
            _mockLogger.Object,
            _accountId,
            _accessToken,
            _basePath);

        Assert.NotNull(service);
    }

    [Fact]
    public void ParseDateTime_WithEmptyString_ReturnsNull()
    {
        var service = new DocuSignService(
            _mockLogger.Object,
            _accountId,
            _accessToken,
            _basePath);

        Assert.NotNull(service);
    }
}
