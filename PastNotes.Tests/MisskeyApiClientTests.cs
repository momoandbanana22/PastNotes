namespace PastNotes.Tests;

public class MisskeyApiClientTests
{
    [Fact]
    public void Initialize_WhenCalledWithValidParameters_ReturnsInitializedClient()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "test-token";

        // Act
        var client = new MisskeyApiClient(instanceUrl, apiToken);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(instanceUrl, client.InstanceUrl);
        Assert.Equal(apiToken, client.ApiToken);
    }

    [Fact]
    public void AuthenticateAsync_WhenCalledWithValidToken_ReturnsSuccess()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);

        // Act
        var result = client.AuthenticateAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AuthenticateAsync_WhenCalledWithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "invalid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);

        // Act
        var result = client.AuthenticateAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetNotesAsync_WhenCalledWithValidDateRange_ReturnsNotesWithinRange()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes = client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.All(notes, note => 
        {
            Assert.True(note.CreatedAt >= startDate);
            Assert.True(note.CreatedAt <= endDate);
        });
    }

    [Fact]
    public void GetNotesAsync_WhenApiCallFails_ThrowsApiException()
    {
        // Arrange
        var instanceUrl = "https://invalid-instance.example.com";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act & Assert
        Assert.Throws<ApiException>(() => client.GetNotesAsync(startDate, endDate));
    }

    [Fact]
    public void GetNotesAsync_WhenStartDateIsAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 2, 1);
        var endDate = new DateTime(2024, 1, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => client.GetNotesAsync(startDate, endDate));
    }
}
