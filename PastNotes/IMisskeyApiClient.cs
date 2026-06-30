namespace PastNotes;

public interface IMisskeyApiClient
{
    Task<IEnumerable<Note>> GetNotesAsync(DateTime startDate, DateTime endDate);
    Task<bool> AuthenticateAsync();
    Task<IEnumerable<Note>> GetNotesWithPagination(DateTime startDate, DateTime endDate, Action<string>? progress = null);
    Task<IEnumerable<Note>> GetNotesWithRetry(DateTime startDate, DateTime endDate, int maxRetries, Action<string>? progress = null);
}
