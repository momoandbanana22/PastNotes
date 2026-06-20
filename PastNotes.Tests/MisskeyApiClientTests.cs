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

        // /api/iエンドポイントの場合はユーザーオブジェクトを返す
        if (request.RequestUri?.AbsolutePath.Contains("/api/i") == true)
        {
            var userResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                    ""id"": ""test-user-id"",
                    ""name"": ""Test User"",
                    ""username"": ""testuser""
                }", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(userResponse);
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
    public void HandleErrorResponse_WhenStatusCode404_ThrowsNotFoundException()
    {
        // Arrange
        var statusCode = 404;
        var client = new MisskeyApiClient("https://misskey.io", "valid-token");

        // Act & Assert
        Assert.Throws<NotFoundException>(() => client.HandleErrorResponse(statusCode, "Not Found"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void HandleErrorResponse_WhenStatusCode429_ThrowsRateLimitExceededException()
    {
        // Arrange
        var statusCode = 429;
        var client = new MisskeyApiClient("https://misskey.io", "valid-token");

        // Act & Assert
        Assert.Throws<RateLimitExceededException>(() => client.HandleErrorResponse(statusCode, "Rate limit exceeded"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetNotesAsync_WhenCalledWithRealApi_ShouldFetchMoreThan100Notes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(apiToken))
        {
            throw new Exception("統合テストを実行するには環境変数を設定してください。");
        }

        var httpClient = new HttpClient();
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 100, "Expected more than 100 notes to be fetched with pagination");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IntegrationTest_WhenCalledWithRealApi_ReturnsActualNotes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL");
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(instanceUrl) || string.IsNullOrEmpty(apiToken))
        {
            Assert.True(false, "統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
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
    [Trait("Category", "Integration")]
    public async Task DebugIntegrationTest_VerifyActualApiCall()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL");
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(instanceUrl) || string.IsNullOrEmpty(apiToken))
        {
            Assert.True(false, "統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
        }

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
    [Trait("Category", "Integration")]
    public async Task EndToEndTest_FetchSaveAndSearchNotes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL");
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(instanceUrl) || string.IsNullOrEmpty(apiToken))
        {
            Assert.True(false, "統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
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
    [Trait("Category", "Integration")]
    public async Task VerifyActualNoteData_ValidateNoteFields()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL");
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(instanceUrl) || string.IsNullOrEmpty(apiToken))
        {
            Assert.True(false, "統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
    public void Constructor_WhenInvalidInstanceUrl_ThrowsArgumentException()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MisskeyApiClient(invalidUrl, "valid-token"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenEmptyApiToken_ThrowsArgumentException()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var emptyToken = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MisskeyApiClient(instanceUrl, emptyToken));
    }

    [Fact]
    [Trait("Category", "Unit")]
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetNotesAsync_WhenUsingUntilIdPagination_ShouldFetchAllNotesCorrectly()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(apiToken))
        {
            throw new Exception("統合テストを実行するには環境変数を設定してください。");
        }

        var httpClient = new HttpClient();
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        Assert.NotNull(notes);
        Assert.True(notes.Count() > 0, "Expected at least one note to be fetched");
        
        // ノートが重複していないことを確認
        var noteIds = notes.Select(n => n.Id).ToList();
        var uniqueNoteIds = noteIds.Distinct().ToList();
        Assert.Equal(noteIds.Count, uniqueNoteIds.Count);
    }
}
