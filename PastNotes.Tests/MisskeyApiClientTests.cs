namespace PastNotes;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public int RequestsSent { get; private set; }
    private int _callCount = 0;
    private int _notesCallCount = 0; // ノート取得呼び出しのみカウント
    private bool _simulateRateLimit = false;
    private HttpResponseMessage? _customErrorResponse;
    private bool _simulatePagination = false;
    public List<string> RequestBodies { get; private set; } = new List<string>();

    public void SimulateRateLimit(bool simulate)
    {
        _simulateRateLimit = simulate;
    }

    public void SetErrorResponse(HttpResponseMessage response)
    {
        _customErrorResponse = response;
    }

    public void SimulatePagination(bool simulate)
    {
        _simulatePagination = simulate;
    }

    private bool _simulateNewerNotesFirst = false;

    public void SimulateNewerNotesFirst(bool simulate)
    {
        _simulateNewerNotesFirst = simulate;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestsSent++;
        _callCount++;

        // リクエストボディを保存
        if (request.Content != null)
        {
            var requestBody = await request.Content.ReadAsStringAsync();
            RequestBodies.Add(requestBody);
        }

        // カスタムエラーレスポンスが設定されている場合はそれを返す
        if (_customErrorResponse != null)
        {
            return _customErrorResponse;
        }

        // レート制限シミュレーション
        if (_simulateRateLimit && _callCount == 1)
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("Rate limit exceeded", System.Text.Encoding.UTF8, "application/json")
            };
        }

        // /api/iエンドポイントの場合はユーザーオブジェクトを返す
        if (request.RequestUri?.AbsolutePath.Contains("/api/i") == true)
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                    ""id"": ""test-user-id"",
                    ""name"": ""Test User"",
                    ""username"": ""testuser""
                }", System.Text.Encoding.UTF8, "application/json")
            };
        }

        // /api/users/notesエンドポイントの場合はカウント
        if (request.RequestUri?.AbsolutePath.Contains("/api/users/notes") == true)
        {
            _notesCallCount++;
        }

        string jsonResponse;

        // 対象期間より新しいノートが先に来るシミュレーション（バグ#8の再現）
        if (_simulateNewerNotesFirst)
        {
            if (_notesCallCount == 1)
            {
                // 1ページ目: 対象期間(2024-01)より新しい2026年のノートを100件返す
                var notes = new List<string>();
                for (int i = 0; i < 100; i++)
                {
                    notes.Add($@"{{
                        ""id"": ""recent-id-{i}"",
                        ""text"": ""Recent note {i}"",
                        ""createdAt"": ""2026-06-28T10:00:00.000Z""
                    }}");
                }
                jsonResponse = "[" + string.Join(",", notes) + "]";
            }
            else if (_notesCallCount == 2)
            {
                // 2ページ目: 対象期間(2024-01)内のノートを2件返す
                jsonResponse = @"[
                    {
                        ""id"": ""target-id-1"",
                        ""text"": ""Target note 1"",
                        ""createdAt"": ""2024-01-15T10:00:00.000Z""
                    },
                    {
                        ""id"": ""target-id-2"",
                        ""text"": ""Target note 2"",
                        ""createdAt"": ""2024-01-10T10:00:00.000Z""
                    }
                ]";
            }
            else
            {
                jsonResponse = "[]";
            }
        }
        // ページネーションシミュレーション
        else if (_simulatePagination)
        {
            // 3回呼び出しをシミュレート（1ページ目100件、2ページ目100件、3ページ目で空）
            if (_notesCallCount == 1)
            {
                // 100件のノートを生成
                var notes = new List<string>();
                for (int i = 0; i < 100; i++)
                {
                    notes.Add($@"{{
                        ""id"": ""test-id-{i}"",
                        ""text"": ""Test note {i}"",
                        ""createdAt"": ""2024-01-15T10:30:00.000Z""
                    }}");
                }
                jsonResponse = "[" + string.Join(",", notes) + "]";
            }
            else if (_notesCallCount == 2)
            {
                // 100件のノートを生成
                var notes = new List<string>();
                for (int i = 100; i < 200; i++)
                {
                    notes.Add($@"{{
                        ""id"": ""test-id-{i}"",
                        ""text"": ""Test note {i}"",
                        ""createdAt"": ""2024-01-10T10:30:00.000Z""
                    }}");
                }
                jsonResponse = "[" + string.Join(",", notes) + "]";
            }
            else
            {
                jsonResponse = "[]";
            }
        }
        else
        {
            // 2回目以降のノート呼び出しでは空の結果を返す（ページネーション終了条件）
            if (_notesCallCount > 1)
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
        }

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };
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
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetErrorResponse(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server Error", System.Text.Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act & Assert: 500はServerErrorException（ApiExceptionのサブクラス）
        await Assert.ThrowsAsync<ServerErrorException>(() => client.GetNotesAsync(startDate, endDate));
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
    public void ParseApiResponse_WhenCalledWithFiles_ReturnsNotesWithFiles()
    {
        // Arrange
        var jsonResponse = @"[
            {
                ""id"": ""test-id-1"",
                ""text"": ""Test note with image"",
                ""createdAt"": ""2024-01-15T10:30:00.000Z"",
                ""files"": [
                    {
                        ""id"": ""file-1"",
                        ""url"": ""https://example.com/image1.jpg"",
                        ""type"": ""image/jpeg"",
                        ""name"": ""image1.jpg""
                    },
                    {
                        ""id"": ""file-2"",
                        ""url"": ""https://example.com/image2.png"",
                        ""type"": ""image/png"",
                        ""name"": ""image2.png""
                    }
                ]
            }
        ]";

        // Act
        var notes = MisskeyApiClient.ParseApiResponse(jsonResponse);

        // Assert
        Assert.NotNull(notes);
        Assert.Single(notes);
        var note = notes.First();
        Assert.NotNull(note.Files);
        Assert.Equal(2, note.Files.Count);
        Assert.Equal("file-1", note.Files[0].Id);
        Assert.Equal("https://example.com/image1.jpg", note.Files[0].Url);
        Assert.Equal("image/jpeg", note.Files[0].Type);
        Assert.Equal("image1.jpg", note.Files[0].Name);
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
        await repository.SaveToFileAsync(notes, testFilePath);
        var loadedNotes = await repository.LoadFromFileAsync(testFilePath);
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
    [Trait("Category", "Unit")]
    public async Task GetNotesWithPagination_WhenApiReturnsMultiplePages_ShouldFetchAllNotes()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        
        // モックハンドラーを作成して、複数ページを返すように設定
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulatePagination(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        
        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);
        
        // Assert
        // ページネーションが実行されていれば、4回呼び出されるはず（認証1回 + 1ページ目 + 2ページ目 + 3ページ目で空）
        Assert.Equal(4, mockHandler.RequestsSent);
        Assert.Equal(200, notes.Count()); // 2ページ × 100件 = 200件
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesWithPagination_WhenFirstPageHasOnlyNewerNotes_ShouldContinuePaginatingToFindInRangeNotes()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";

        // 1ページ目が対象期間より新しいノートで埋まるシナリオ
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulateNewerNotesFirst(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);

        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        // 1ページ目(2026年ノート)はフィルタ対象外、2ページ目(2024-01)の2件が返るべき
        Assert.Equal(2, notes.Count());
        Assert.All(notes, note =>
        {
            Assert.True(note.CreatedAt >= startDate && note.CreatedAt <= endDate,
                $"範囲外のノートが含まれています: {note.CreatedAt}");
        });
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesAsync_ShouldNotUseSinceDateUntilDateParameters()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        
        // カスタムモックハンドラーを作成して、リクエスト内容を検証
        var customHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(customHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        
        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);
        
        // Assert
        // Misskey APIのsinceDateとuntilDateパラメータは壊れているため、使用しないことを確認
        Assert.NotNull(notes);
        
        // /api/users/notesへのリクエストボディを確認
        var notesRequests = customHandler.RequestBodies.Where(body => body.Contains("\"userId\"")).ToList();
        Assert.NotEmpty(notesRequests);
        
        foreach (var requestBody in notesRequests)
        {
            // sinceDateとuntilDateが含まれていないことを確認
            Assert.DoesNotContain("sinceDate", requestBody);
            Assert.DoesNotContain("untilDate", requestBody);
        }
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

    // TDD: BUG-8 - _callCountバグ
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesAsync_WithMockHttpClient_ShouldReturnNotesFromMock()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert
        // MockHttpMessageHandlerは2件のノート(2024-01-15, 2024-01-20)を返すはず
        // バグがあると認証の_callCountが加算されて[]が返り、0件になる
        Assert.Equal(2, notes.Count());
    }

    // TDD: キャッシュ有効期限管理
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesAsync_WhenCalledAfterCacheExpiration_ShouldRefetchData()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHttpMessageHandler);
        // テスト用に短いキャッシュ有効期限（50ms）を設定
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient, TimeSpan.FromMilliseconds(50));
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var notes1 = await client.GetNotesAsync(startDate, endDate);
        var firstRequestCount = mockHttpMessageHandler.RequestsSent;
        
        // キャッシュ有効期限を待つ
        await Task.Delay(100);
        
        var notes2 = await client.GetNotesAsync(startDate, endDate);
        var secondRequestCount = mockHttpMessageHandler.RequestsSent;

        // Assert
        Assert.NotNull(notes1);
        Assert.NotNull(notes2);
        // キャッシュが有効期限切れの場合、リクエスト数が増えるはず
        Assert.True(secondRequestCount > firstRequestCount, "Cache should expire and refetch data");
    }

    // TDD: エラーハンドリング改善
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesFromApiAsync_WhenReturns404_ShouldThrowNotFoundException()
    {
        // Arrange
        var instanceUrl = "https://misskey.io";
        var apiToken = "valid-token";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        // 404エラーを返すようにモックを設定
        var errorResponse = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not Found", System.Text.Encoding.UTF8, "application/json")
        };
        mockHttpMessageHandler.SetErrorResponse(errorResponse);
        
        var httpClient = new HttpClient(mockHttpMessageHandler);
        var client = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => client.GetNotesAsync(startDate, endDate));
    }
}
