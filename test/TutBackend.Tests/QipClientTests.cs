using Xunit;
using TutBackend.Services;
using System.Net;
using System.Net.Http.Json;

namespace TutBackend.Tests;

public class QipClientTests
{
    [Fact]
    public void Create_WithValidBaseAddress_ReturnsClient()
    {
        // Arrange & Act
        var client = QipClient.Create("https://example.com");

        // Assert
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidBaseAddress_ThrowsArgumentException(string? baseAddress)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => QipClient.Create(baseAddress!));
    }

    [Fact]
    public async Task RegisterAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await client.RegisterAsync(null!));
    }

    [Fact]
    public async Task LoginAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await client.LoginAsync(null!));
    }

    [Fact]
    public async Task RefreshAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await client.RefreshAsync(null!));
    }

    [Fact]
    public async Task LogoutAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await client.LogoutAsync(null!));
    }

    [Fact]
    public async Task ValidateAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await client.ValidateAsync(null!));
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");
        var request = new ValidateRequest { Token = "" };

        // Act
        var result = await client.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithValidToken_ReturnsValidResponse()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");
        var request = new ValidateRequest { Token = "Bearer:testuser" };

        // Act
        var result = await client.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task ValidateAsync_WithShortToken_ExtractsUsername()
    {
        // Arrange
        var client = QipClient.Create("https://example.com");
        var request = new ValidateRequest { Token = "1234567890123456" };

        // Act
        var result = await client.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("890123456", result.Username);
    }
}

