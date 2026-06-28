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
        var utcNow = DateTime.UtcNow;
        var convertedStartDate = utcNow.AddDays(-days);
        var convertedEndDate = utcNow;

        var jstNow = utcNow.AddHours(9);
        System.Console.WriteLine($"Fetching notes from {jstNow.AddDays(-days):yyyy-MM-dd} to {jstNow:yyyy-MM-dd} (JST)...");

        return await FetchAndSaveAsync(convertedStartDate, convertedEndDate);
    }

    public async Task<int> ExecuteAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        var convertedStartDate = startDate.AddHours(-9);
        var convertedEndDate = endDate.AddHours(-9);

        System.Console.WriteLine($"Fetching notes from {startDate:yyyy-MM-dd HH:mm:ss} (JST) to {endDate:yyyy-MM-dd HH:mm:ss} (JST)...");

        return await FetchAndSaveAsync(convertedStartDate, convertedEndDate);
    }

    private async Task<int> FetchAndSaveAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
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
        catch (UnauthorizedException ex)
        {
            System.Console.Error.WriteLine($"Error: Unauthorized - {ex.Message}");
            return 1;
        }
    }
}
