namespace PastNotes;

public interface IMisskeyApiClient
{
    Task<IEnumerable<Note>> GetNotesAsync(DateTime startDate, DateTime endDate);
}
