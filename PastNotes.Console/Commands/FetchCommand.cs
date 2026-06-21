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

    public async Task<int> ExecuteAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        // Input dates are treated as JST (UTC+9), convert to UTC for API
        var convertedStartDate = startDate.AddHours(-9);
        // Add 1 second to end date to make untilDate inclusive (API treats untilDate as exclusive)
        var convertedEndDate = endDate.AddHours(-9).AddSeconds(1);

        System.Console.WriteLine($"Fetching notes from {startDate:yyyy-MM-dd HH:mm:ss} (JST) to {endDate:yyyy-MM-dd HH:mm:ss} (JST)...");

        var notes = await _apiClient.GetNotesAsync(convertedStartDate, convertedEndDate);
        
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
