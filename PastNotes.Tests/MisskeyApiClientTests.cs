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

    [Fact]
    public void GetNotesAsync_WhenCalledWithHttpClient_SendsRequestToCorrectEndpoint()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var httpClient = new HttpClient();
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes = client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
    }

    [Fact]
    public void ParseApiResponse_WhenCalledWithValidJson_ReturnsNoteObjects()
    {
        // Arrange
        var jsonResponse = @"[
            {
                ""id"": ""test-id-1"",
                ""text"": ""Test note 1"",
                ""createdAt"": ""2024-01-15T10:30:00.000Z""
            },
            {
                ""id"": ""test-id-2"",
                ""text"": ""Test note 2"",
                ""createdAt"": ""2024-01-20T14:45:00.000Z""
            }
        ]";

        // Act
        var notes = MisskeyApiClient.ParseApiResponse(jsonResponse);

        // Assert
        Assert.NotNull(notes);
        Assert.Equal(2, notes.Count());
        Assert.Equal("test-id-1", notes.First().Id);
        Assert.Equal("Test note 1", notes.First().Text);
    }

    [Fact]
    public void GetAuthorizationHeader_WhenCalled_ReturnsCorrectHeaderValue()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "test-token-123";
        var client = new MisskeyApiClient(instanceUrl, apiToken);

        // Act
        var authHeader = client.GetAuthorizationHeader();

        // Assert
        Assert.Equal("Bearer test-token-123", authHeader);
    }

    [Fact]
    public void HandleErrorResponse_WhenStatusCode404_ThrowsNotFoundException()
    {
        // Arrange
        var statusCode = 404;
        var client = new MisskeyApiClient("https://misskey.io", "valid-token");

        // Act & Assert
        Assert.Throws<NotFoundException>(() => client.HandleErrorResponse(statusCode, "Not Found"));
    }

    [Fact]
    public void HandleErrorResponse_WhenStatusCode429_ThrowsRateLimitExceededException()
    {
        // Arrange
        var statusCode = 429;
        var client = new MisskeyApiClient("https://misskey.io", "valid-token");

        // Act & Assert
        Assert.Throws<RateLimitExceededException>(() => client.HandleErrorResponse(statusCode, "Rate limit exceeded"));
    }

    [Fact]
    public void GetNotesWithPagination_WhenCalledWithPagination_ReturnsAllPages()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes = client.GetNotesWithPagination(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0);
    }

    [Fact(Skip = "統合テスト: 実際のAPIエンドポイントが必要")]
    public void IntegrationTest_WhenCalledWithRealApi_ReturnsActualNotes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        
        if (string.IsNullOrEmpty(apiToken))
        {
            return; // 環境変数がない場合はテストをスキップ
        }

        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var notes = client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0);
    }

    [Fact]
    public void GetNotesWithRetry_WhenRateLimitExceeded_RetriesWithBackoff()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var maxRetries = 3;

        // Act
        var notes = client.GetNotesWithRetry(startDate, endDate, maxRetries);

        // Assert
        Assert.NotNull(notes);
    }
}
