using PastNotes;

namespace PastNotes.Console.Commands;

public class FetchCommand
{
    private readonly IMisskeyApiClient _apiClient;
    private readonly NoteRepository _repository;

    public FetchCommand(IMisskeyApiClient apiClient, NoteRepository repository)
    {
        _apiClient = apiClient;
        _repository = repository;
    }

    public async Task<int> ExecuteAsync(int days)
    {
        var startDate = DateTime.Now.AddDays(-days);
        var endDate = DateTime.Now;

        System.Console.WriteLine($"Fetching notes from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}...");

        var notes = await _apiClient.GetNotesAsync(startDate, endDate);
        
        if (notes == null || !notes.Any())
        {
            System.Console.WriteLine("No notes found.");
            return 0;
        }

        await _repository.SaveToFileAsync(notes, "notes.json");
        System.Console.WriteLine($"Saved {notes.Count()} notes to notes.json");

        return 0;
    }
}
