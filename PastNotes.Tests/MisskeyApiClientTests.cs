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

    private bool _simulateAllOlderNotes = false;

    public void SimulateAllOlderNotes(bool simulate)
    {
        _simulateAllOlderNotes = simulate;
    }

    private bool _simulateNetworkFailureOnSecondPage = false;

    public void SimulateNetworkFailureOnSecondPage(bool simulate)
    {
        _simulateNetworkFailureOnSecondPage = simulate;
    }

    private bool _simulateNetworkFailureOnFirstNotesCall = false;

    public void SimulateNetworkFailureOnFirstNotesCall(bool simulate)
    {
        _simulateNetworkFailureOnFirstNotesCall = simulate;
    }

    private bool _simulateNetworkFailureAlways = false;

    public void SimulateNetworkFailureAlways(bool simulate)
    {
        _simulateNetworkFailureAlways = simulate;
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

            // 2ページ目でネットワーク断をシミュレート（TST-9）
            if (_simulateNetworkFailureOnSecondPage && _notesCallCount >= 2)
            {
                throw new HttpRequestException("Network failure during pagination");
            }

            // 1回目のノート取得でネットワーク断をシミュレート（BUG-28テスト用）
            if (_simulateNetworkFailureOnFirstNotesCall && _notesCallCount == 1)
            {
                throw new HttpRequestException("Simulated network failure");
            }

            // 毎回ネットワーク断をシミュレート（BUG-28テスト用）
            if (_simulateNetworkFailureAlways)
            {
                throw new HttpRequestException("Simulated network failure");
            }
        }

        string jsonResponse;

        // 全ノートが対象期間より古いシミュレーション（TST-1）
        if (_simulateAllOlderNotes)
        {
            if (_notesCallCount == 1)
            {
                // 100件の古いノート（2020年）を返す → Count<100にならず日付で終了判定
                var notes = new List<string>();
                for (int i = 0; i < 100; i++)
                {
                    notes.Add($@"{{
                        ""id"": ""old-id-{i}"",
                        ""text"": ""Old note {i}"",
                        ""createdAt"": ""2020-01-15T10:00:00.000Z""
                    }}");
                }
                jsonResponse = "[" + string.Join(",", notes) + "]";
            }
            else
            {
                jsonResponse = "[]";
            }
        }
        // 対象期間より新しいノートが先に来るシミュレーション（バグ#8の再現）
        else if (_simulateNewerNotesFirst)
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
        // ページネーション中ネットワーク断シミュレーション: 1ページ目は100件返してページネーションを継続させる
        else if (_simulateNetworkFailureOnSecondPage)
        {
            var notes = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                notes.Add($@"{{
                    ""id"": ""page1-id-{i}"",
                    ""text"": ""Note {i}"",
                    ""createdAt"": ""2024-01-15T10:00:00.000Z""
                }}");
            }
            jsonResponse = "[" + string.Join(",", notes) + "]";
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
    public async Task AuthenticateAsync_WhenTokenNameHappensToBeInvalidToken_IsNotSpecialCased()
    {
        // "invalid-token" は本番コードで特別扱いされるべきでない。
        // 任意の非空文字列トークンで HttpClient なし認証は同じ結果（true）を返すべき。
        // Arrange
        var client = new MisskeyApiClient("https://misskey.io", "invalid-token");

        // Act
        var result = await client.AuthenticateAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesAsync_WhenCalledWithoutHttpClient_ThrowsInvalidOperationException()
    {
        // HttpClient なしで API メソッドを呼ぶことは設計上できないはず。
        // ダミーデータを返す特殊ケースは本番コードに存在すべきでない（BUG-19）。
        // Arrange
        var client = new MisskeyApiClient("https://misskey.io", "valid-token");
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetNotesAsync(startDate, endDate));
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

    // TDD: BUG-35 - 進捗コールバックが呼ばれること、Console には書かれないこと
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesWithRetry_WhenProgressProvided_InvokesCallback()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulatePagination(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);

        var progressMessages = new List<string>();
        Action<string> progress = msg => progressMessages.Add(msg);

        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate   = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        await client.GetNotesWithRetry(startDate, endDate, maxRetries: 3, progress: progress);

        // Assert
        Assert.NotEmpty(progressMessages);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesWithRetry_WhenNoProgressProvided_WritesNothingToConsole()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulatePagination(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);

        var originalOut = System.Console.Out;
        using var sw = new StringWriter();
        System.Console.SetOut(sw);

        try
        {
            var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate   = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            // Act: progress なしで呼ぶ
            await client.GetNotesWithRetry(startDate, endDate, maxRetries: 3);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }

        // Assert: ライブラリ側から Console への書き込みがないこと
        Assert.Empty(sw.ToString());
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
    [Trait("Category", "Integration")]
    public async Task GetNotesAsync_WhenCalledWithRealApi_ShouldFetchMoreThan100Notes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(apiToken))
        {
            Assert.Fail("統合テストを実行するには環境変数を設定してください。");
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
            Assert.Fail("統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
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
            Assert.Fail("統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
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
            Assert.Fail("統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
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
            Assert.Fail("統合テストを実行するには環境変数を設定してください。'dotnet test --filter \"Category=Integration\"' を使用して統合テストのみを実行してください。");
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
            Assert.Fail("統合テストを実行するには環境変数を設定してください。");
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

    // TDD: FEAT-1 / BUG-35 - GetNotesAsync はコールバックなしで呼ぶため Console に何も書かない
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesAsync_WhenPaginating_WritesNothingToConsole()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulatePagination(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            // Act
            await client.GetNotesAsync(
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc));
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        // Assert: ライブラリは Console に書かない（BUG-35 修正後の正しい動作）
        Assert.Empty(stringWriter.ToString());
    }

    // TDD: TST-1 - 全ノートが対象期間より古い場合に0件で終了する
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesAsync_WhenAllNotesAreOlderThanRange_ShouldReturnEmpty()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulateAllOlderNotes(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);

        // 2024年を指定するが、モックは2020年のノートを返す
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var notes = await client.GetNotesAsync(startDate, endDate);

        // Assert: 対象期間内にノートがないので0件
        Assert.Empty(notes);
        // 2ページ目を取りに行っていないこと（認証1回 + ノート1回 = 2リクエスト）
        Assert.Equal(2, mockHandler.RequestsSent);
    }

    // TDD: TST-9 - ページネーション中のネットワーク断
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesAsync_WhenNetworkFailsDuringPagination_PropagatesHttpRequestException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulateNetworkFailureOnSecondPage(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);

        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act & Assert: リトライなしで例外がそのまま伝播することを確認
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetNotesAsync(startDate, endDate));
    }

    // TDD: BUG-22 - GetNotesWithRetry がページネーションで全件取得するか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesWithRetry_WhenApiReturnsMultiplePages_ShouldFetchAllNotes()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulatePagination(true); // 1ページ目100件、2ページ目100件、3ページ目空
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate   = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var notes = await client.GetNotesWithRetry(startDate, endDate, maxRetries: 3);

        // Assert: ページネーションが動くなら 2ページ × 100件 = 200件
        Assert.Equal(200, notes.Count());
    }

    // TDD: BUG-23 - リトライ上限到達時に "Max retries exceeded" を投げるか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesWithRetry_WhenMaxRetriesExceeded_ThrowsMaxRetriesExceededMessage()
    {
        // Arrange: 常に429を返すモックで全リトライを使い果たさせる
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetErrorResponse(new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("Rate limit exceeded", System.Text.Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act & Assert: リトライを使い果たした後は元の例外ではなく "Max retries exceeded" を投げるべき
        var ex = await Assert.ThrowsAsync<RateLimitExceededException>(
            () => client.GetNotesWithRetry(startDate, endDate, maxRetries: 1));
        Assert.Equal("Max retries exceeded", ex.Message);
    }

    // TDD: BUG-28 - HttpRequestException 発生時もリトライして成功するか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesWithRetry_WhenHttpRequestExceptionThenSucceeds_RetriesAndReturnsNotes()
    {
        // Arrange: 1回目の /api/users/notes は HttpRequestException、2回目は成功
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulateNetworkFailureOnFirstNotesCall(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act: maxRetries=3 なので1回失敗してもリトライして完了するはず
        var notes = await client.GetNotesWithRetry(startDate, endDate, maxRetries: 3);

        // Assert: 例外を投げずに完了し、リトライのためリクエスト数は 認証+失敗+リトライ = 3 以上
        Assert.NotNull(notes);
        Assert.True(mockHandler.RequestsSent >= 3);
    }

    // TDD: BUG-28 - HttpRequestException でリトライ上限到達時に "Max retries exceeded" を投げるか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNotesWithRetry_WhenHttpRequestExceptionExhaustsMaxRetries_ThrowsMaxRetriesExceededMessage()
    {
        // Arrange: 常に HttpRequestException を投げるモックで全リトライを使い果たさせる
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulateNetworkFailureAlways(true);
        var httpClient = new HttpClient(mockHandler);
        var client = new MisskeyApiClient("https://misskey.io", "valid-token", httpClient);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act & Assert: リトライ上限後は "Max retries exceeded" を投げるべき
        var ex = await Assert.ThrowsAsync<RateLimitExceededException>(
            () => client.GetNotesWithRetry(startDate, endDate, maxRetries: 1));
        Assert.Equal("Max retries exceeded", ex.Message);
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
