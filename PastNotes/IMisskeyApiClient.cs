namespace PastNotes;

public interface IMisskeyApiClient
{
    Task<IEnumerable<Note>> GetNotesWithRetry(DateTime startDate, DateTime endDate, int maxRetries, Action<string>? progress = null);
}
