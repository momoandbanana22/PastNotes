namespace PastNotes;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public int RequestsSent { get; private set; }
    private int _callCount = 0;
    private bool _simulateRateLimit = false;

    public void SimulateRateLimit(bool simulate)
    {
        _simulateRateLimit = simulate;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestsSent++;
        _callCount++;

        // レート制限シミュレーション
        if (_simulateRateLimit && _callCount == 1)
        {
            var rateLimitResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("Rate limit exceeded", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(rateLimitResponse);
        }

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

    [Fact]
    public async Task IntegrationTest_WhenCalledWithRealApi_ReturnsActualNotes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        
        if (string.IsNullOrEmpty(apiToken))
        {
            return; // 環境変数がない場合はテストをスキップ
        }

        var httpClient = new HttpClient();
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0);

        // 実際のAPI呼び出しを確認するために、2回目の呼び出しも行う
        var notes2 = await client.GetNotesAsync(startDate, endDate);
        Assert.NotNull(notes2);
        Assert.Equal(notes.Count(), notes2.Count()); // キャッシュが効いているはず
    }

    [Fact]
    public async Task DebugIntegrationTest_VerifyActualApiCall()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL");
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        
        // 環境変数が設定されていることを確認
        Assert.False(string.IsNullOrEmpty(apiToken), "MISSKEY_API_TOKEN環境変数が設定されていません");
        Assert.False(string.IsNullOrEmpty(instanceUrl), "MISSKEY_INSTANCE_URL環境変数が設定されていません");

        var httpClient = new HttpClient();
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var notes = await client.GetNotesAsync(startDate, endDate);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(notes);
        // 実際のAPI呼び出しであれば、実行時間は100ms以上であるはず
        Assert.True(stopwatch.ElapsedMilliseconds > 100, $"実行時間が短すぎます: {stopwatch.ElapsedMilliseconds}ms (実際のAPI呼び出しであれば100ms以上)");
    }

    [Fact]
    public async Task EndToEndTest_FetchSaveAndSearchNotes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL");
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        
        if (string.IsNullOrEmpty(apiToken) || string.IsNullOrEmpty(instanceUrl))
        {
            return; // 環境変数がない場合はテストをスキップ
        }

        var httpClient = new HttpClient();
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var repository = new NoteRepository();
        var testFilePath = "test_notes.json";
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);
        repository.SaveToFileAsync(notes, testFilePath);
        var loadedNotes = repository.LoadFromFileAsync(testFilePath);
        var searchResults = repository.SearchByKeyword(loadedNotes, "test");

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0);
        Assert.NotNull(loadedNotes);
        Assert.Equal(notes.Count(), loadedNotes.Count());
        Assert.NotNull(searchResults);

        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    public async Task VerifyActualNoteData_ValidateNoteFields()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL");
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        
        if (string.IsNullOrEmpty(apiToken) || string.IsNullOrEmpty(instanceUrl))
        {
            return; // 環境変数がない場合はテストをスキップ
        }

        var httpClient = new HttpClient();
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0);

        // 各ノートのフィールドを検証
        foreach (var note in notes)
        {
            Assert.False(string.IsNullOrEmpty(note.Id), "Note ID should not be empty");
            Assert.True(note.CreatedAt > DateTime.MinValue, "Note CreatedAt should be valid");
        }
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

    [Fact]
    public async Task GetNotesWithRetry_WhenRateLimitExceeded_RetriesWithBackoff()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.SimulateRateLimit(true);
        var httpClient = new HttpClient(mockHttpMessageHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var maxRetries = 3;

        // Act
        var notes = await client.GetNotesWithRetry(startDate, endDate, maxRetries);

        // Assert
        Assert.NotNull(notes);
        Assert.True(mockHttpMessageHandler.RequestsSent > 1); // リトライが発生したことを確認
    }

    [Fact]
    public void Constructor_WhenInvalidInstanceUrl_ThrowsArgumentException()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MisskeyApiClient(invalidUrl, "valid-token"));
    }

    [Fact]
    public void Constructor_WhenEmptyApiToken_ThrowsArgumentException()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var emptyToken = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MisskeyApiClient(instanceUrl, emptyToken));
    }

    [Fact]
    public async Task GetNotesAsync_WhenCalledTwiceWithSameParameters_UsesCache()
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
        var notes1 = await client.GetNotesAsync(startDate, endDate);
        var firstRequestCount = mockHttpMessageHandler.RequestsSent;
        var notes2 = await client.GetNotesAsync(startDate, endDate);
        var secondRequestCount = mockHttpMessageHandler.RequestsSent;

        // Assert
        Assert.NotNull(notes1);
        Assert.NotNull(notes2);
        Assert.Equal(firstRequestCount, secondRequestCount); // キャッシュが有効ならリクエスト数が変わらない
    }
}
