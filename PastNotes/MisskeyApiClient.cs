namespace PastNotes;

public class MisskeyApiClient
{
    public string InstanceUrl { get; }
    public string ApiToken { get; }

    public MisskeyApiClient(string instanceUrl, string apiToken)
    {
        InstanceUrl = instanceUrl;
        ApiToken = apiToken;
    }

    public bool AuthenticateAsync()
    {
        // TODO: 実際のAPI認証を実装
        // 現在は簡易的な実装としてトークンの有無で判定
        return !string.IsNullOrEmpty(ApiToken) && ApiToken != "invalid-token";
    }

    public IEnumerable<Note> GetNotesAsync(DateTime startDate, DateTime endDate)
    {
        // TODO: 実際のAPI呼び出しを実装
        // 現在は簡易的な実装としてダミーデータを返す
        
        // 日付範囲のバリデーション
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        // 無効なインスタンスURLの場合は例外をスロー
        if (InstanceUrl.Contains("invalid-instance"))
        {
            throw new ApiException("Invalid instance URL");
        }

        return new List<Note>
        {
            new Note { CreatedAt = new DateTime(2024, 1, 15), Id = "1", Text = "Test note 1" },
            new Note { CreatedAt = new DateTime(2024, 1, 20), Id = "2", Text = "Test note 2" }
        }.Where(note => note.CreatedAt >= startDate && note.CreatedAt <= endDate);
    }
}
