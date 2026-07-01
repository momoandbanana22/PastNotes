using System.Text.Json;

namespace PastNotes;

public class MisskeyApiClient : IMisskeyApiClient
{
    public string InstanceUrl { get; }
    public string ApiToken { get; }
    private HttpClient? _httpClient;
    private Dictionary<string, (IEnumerable<Note> Notes, DateTime Timestamp)> _cache = new();
    private TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private string? _userId;

    public MisskeyApiClient(string instanceUrl, string apiToken)
    {
        if (string.IsNullOrWhiteSpace(instanceUrl) || !Uri.TryCreate(instanceUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid instance URL format");
        }

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            throw new ArgumentException("API token is required");
        }

        InstanceUrl = instanceUrl;
        ApiToken = apiToken;
    }

    public MisskeyApiClient(string instanceUrl, string apiToken, HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(instanceUrl) || !Uri.TryCreate(instanceUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid instance URL format");
        }

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            throw new ArgumentException("API token is required");
        }

        InstanceUrl = instanceUrl;
        ApiToken = apiToken;
        _httpClient = httpClient;
    }

    // テスト用：キャッシュ有効期限を設定できるコンストラクタ
    public MisskeyApiClient(string instanceUrl, string apiToken, HttpClient httpClient, TimeSpan cacheExpiration)
    {
        if (string.IsNullOrWhiteSpace(instanceUrl) || !Uri.TryCreate(instanceUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid instance URL format");
        }

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            throw new ArgumentException("API token is required");
        }

        InstanceUrl = instanceUrl;
        ApiToken = apiToken;
        _httpClient = httpClient;
        _cacheExpiration = cacheExpiration;
    }

    public async Task<bool> AuthenticateAsync()
    {
        // HttpClientが提供されている場合は実際のAPI認証を実行
        if (_httpClient != null)
        {
            return await AuthenticateWithApiAsync();
        }

        // HttpClientが提供されていない場合はトークンの有無のみで判定（コンストラクタ検証済み）
        return !string.IsNullOrEmpty(ApiToken);
    }

    private async Task<bool> AuthenticateWithApiAsync()
    {
        var requestBody = new
        {
            i = ApiToken
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{InstanceUrl}/api/i")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _httpClient!.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return false;
        }

        // エラーハンドリング
        if (!response.IsSuccessStatusCode)
        {
            HandleErrorResponse(response);
        }

        // ユーザーIDを取得
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var userData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jsonResponse);
        if (userData.TryGetProperty("id", out var idElement))
        {
            _userId = idElement.GetString();
        }

        return true;
    }

    private void HandleErrorResponse(HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;
        
        switch (statusCode)
        {
            case System.Net.HttpStatusCode.NotFound:
                throw new NotFoundException("Resource not found");
            case System.Net.HttpStatusCode.Unauthorized:
                throw new UnauthorizedException("Unauthorized access");
            case System.Net.HttpStatusCode.TooManyRequests:
                throw new RateLimitExceededException("Rate limit exceeded");
            case System.Net.HttpStatusCode.InternalServerError:
            case System.Net.HttpStatusCode.BadGateway:
            case System.Net.HttpStatusCode.ServiceUnavailable:
                throw new ServerErrorException($"Server error: {statusCode}");
            default:
                throw new ApiException($"HTTP error: {statusCode}");
        }
    }

    private async Task<IEnumerable<Note>> GetNotesFromApiAsync(DateTime startDate, DateTime endDate, string? untilId = null)
    {
        // 認証してユーザーIDを取得
        if (_userId == null && _httpClient != null)
        {
            await AuthenticateWithApiAsync();
        }

        if (_userId == null)
        {
            throw new ApiException("Failed to authenticate and get user ID");
        }

        // 日付範囲を指定せずに、すべてのノートを取得してからクライアント側でフィルタリング
        Dictionary<string, object> requestBody = new Dictionary<string, object>
        {
            { "userId", _userId },
            { "limit", 100 }
        };

        if (!string.IsNullOrEmpty(untilId))
        {
            requestBody["untilId"] = untilId;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{InstanceUrl}/api/users/notes")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _httpClient!.SendAsync(request);
        
        // エラーハンドリング
        if (!response.IsSuccessStatusCode)
        {
            HandleErrorResponse(response);
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return ParseApiResponse(jsonResponse);
    }

    public async Task<IEnumerable<Note>> GetNotesWithCache(DateTime startDate, DateTime endDate)
    {
        // 日付範囲のバリデーション
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        // キャッシュキーを生成
        var cacheKey = $"notes_{startDate:o}_{endDate:o}";

        // キャッシュをチェック（有効期限も確認）
        if (_cache.TryGetValue(cacheKey, out var cachedData))
        {
            var (cachedNotes, timestamp) = cachedData;
            if (DateTime.Now - timestamp < _cacheExpiration)
            {
                return cachedNotes;
            }
            // 有効期限切れの場合はキャッシュを削除
            _cache.Remove(cacheKey);
        }

        if (_httpClient == null)
        {
            throw new InvalidOperationException("HttpClient is required to call the Misskey API");
        }

        var notes = await GetNotesWithPaginationFromApiAsync(startDate, endDate);

        // キャッシュに保存（タイムスタンプ付き）
        _cache[cacheKey] = (notes, DateTime.Now);

        return notes;
    }

    public static IEnumerable<Note> ParseApiResponse(string jsonResponse)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var apiResponses = JsonSerializer.Deserialize<List<JsonElement>>(jsonResponse, options);
        
        if (apiResponses == null)
        {
            return Enumerable.Empty<Note>();
        }
        
        return apiResponses.Select(api => new Note
        {
            Id = api.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty,
            Text = api.TryGetProperty("text", out var textElement) ? textElement.GetString() ?? string.Empty : string.Empty,
            CreatedAt = api.TryGetProperty("createdAt", out var createdAtElement) ? createdAtElement.GetDateTime() : DateTime.MinValue,
            Files = api.TryGetProperty("files", out var filesElement) && filesElement.ValueKind == JsonValueKind.Array 
                ? filesElement.EnumerateArray().Select(file => new NoteFile
                {
                    Id = file.TryGetProperty("id", out var fileId) ? fileId.GetString() ?? string.Empty : string.Empty,
                    Url = file.TryGetProperty("url", out var fileUrl) ? fileUrl.GetString() ?? string.Empty : string.Empty,
                    Type = file.TryGetProperty("type", out var fileType) ? fileType.GetString() ?? string.Empty : string.Empty,
                    Name = file.TryGetProperty("name", out var fileName) ? fileName.GetString() ?? string.Empty : string.Empty
                }).ToList()
                : new List<NoteFile>()
        });
    }

    public async Task<IEnumerable<Note>> GetNotesWithPagination(DateTime startDate, DateTime endDate, Action<string>? progress = null)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HttpClient is required to call the Misskey API");
        }

        return await GetNotesWithPaginationFromApiAsync(startDate, endDate, progress);
    }

    private async Task<IEnumerable<Note>> GetNotesWithPaginationFromApiAsync(DateTime startDate, DateTime endDate, Action<string>? progress = null)
    {
        var allNotes = new List<Note>();
        var untilId = (string?)null;
        var hasMoreNotes = true;

        while (hasMoreNotes)
        {
            var notes = (await GetNotesFromApiAsync(startDate, endDate, untilId)).ToList();

            if (notes.Count == 0)
            {
                hasMoreNotes = false;
            }
            else
            {
                var filteredNotes = notes.Where(note => note.CreatedAt >= startDate && note.CreatedAt <= endDate);
                allNotes.AddRange(filteredNotes);

                progress?.Invoke($"  取得中... {allNotes.Count} 件");

                untilId = notes.Last().Id;

                if (notes.Count < 100)
                {
                    hasMoreNotes = false;
                }

                if (notes.Last().CreatedAt < startDate)
                {
                    hasMoreNotes = false;
                }
            }
        }

        return allNotes;
    }

    public async Task<IEnumerable<Note>> GetNotesWithRetry(DateTime startDate, DateTime endDate, int maxRetries, Action<string>? progress = null)
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("HttpClient is required to call the Misskey API");
        }

        return await GetNotesWithRetryFromApiAsync(startDate, endDate, maxRetries, progress);
    }

    private async Task<IEnumerable<Note>> GetNotesWithRetryFromApiAsync(DateTime startDate, DateTime endDate, int maxRetries, Action<string>? progress = null)
    {
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        while (true)
        {
            try
            {
                return await GetNotesWithPaginationFromApiAsync(startDate, endDate, progress);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is RateLimitExceededException)
            {
                if (retryCount < maxRetries)
                {
                    retryCount++;
                    await Task.Delay(delay);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // 指数バックオフ
                }
                else
                {
                    throw new RateLimitExceededException(retryCount == 0 ? ex.Message : "Max retries exceeded");
                }
            }
        }
    }
}
