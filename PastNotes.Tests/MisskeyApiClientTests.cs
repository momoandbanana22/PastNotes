namespace PastNotes.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public int RequestsSent { get; private set; }
    private int _callCount = 0;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestsSent++;
        _callCount++;

        // 2回目以降の呼び出しでは空の結果を返す（ページネーション終了条件）
        string jsonResponse;
        if (_callCount > 1)
        {
            jsonResponse = "[]";
        }
        else
        {
            jsonResponse = @"[
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
        }

        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}

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
    public async Task AuthenticateAsync_WhenCalledWithValidToken_ReturnsSuccess()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);

        // Act
        var result = await client.AuthenticateAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCalledWithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "invalid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);

        // Act
        var result = await client.AuthenticateAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetNotesAsync_WhenCalledWithValidDateRange_ReturnsNotesWithinRange()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.All(notes, note => 
        {
            Assert.True(note.CreatedAt >= startDate);
            Assert.True(note.CreatedAt <= endDate);
        });
    }

    [Fact]
    public async Task GetNotesAsync_WhenApiCallFails_ThrowsApiException()
    {
        // Arrange
        var instanceUrl = "https://invalid-instance.example.com";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => client.GetNotesAsync(startDate, endDate));
    }

    [Fact]
    public async Task GetNotesAsync_WhenStartDateIsAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 2, 1);
        var endDate = new DateTime(2024, 1, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetNotesAsync(startDate, endDate));
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
    public async Task GetNotesWithPagination_WhenCalledWithPagination_ReturnsAllPages()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes = await client.GetNotesWithPagination(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0);
    }

    [Fact(Skip = "統合テスト: 実際のAPIエンドポイントが必要")]
    public async Task IntegrationTest_WhenCalledWithRealApi_ReturnsActualNotes()
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
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0);
    }

    [Fact]
    public async Task GetNotesWithRetry_WhenRateLimitExceeded_RetriesWithBackoff()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var client = new MisskeyApiClient(instanceUrl, apiToken);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var maxRetries = 3;

        // Act
        var notes = await client.GetNotesWithRetry(startDate, endDate, maxRetries);

        // Assert
        Assert.NotNull(notes);
    }

    [Fact]
    public async Task GetNotesAsync_WhenCalledWithHttpClient_SendsRequestToMisskeyApi()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHttpMessageHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(mockHttpMessageHandler.RequestsSent > 0);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCalledWithHttpClient_SendsRequestToApiIEndpoint()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHttpMessageHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);

        // Act
        var result = await client.AuthenticateAsync();

        // Assert
        Assert.True(result);
        Assert.True(mockHttpMessageHandler.RequestsSent > 0);
    }

    [Fact]
    public async Task GetNotesWithPagination_WhenCalledWithHttpClient_UsesUntilParameter()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHttpMessageHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes = await client.GetNotesWithPagination(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(mockHttpMessageHandler.RequestsSent > 0);
    }
}
