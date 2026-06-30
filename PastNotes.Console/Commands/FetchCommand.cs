using PastNotes;

namespace PastNotes.Console.Commands;

public class FetchCommand
{
    private readonly IMisskeyApiClient _apiClient;
    private readonly NoteRepository _repository;
    private readonly string _filePath;
    private readonly bool _append;

    public FetchCommand(IMisskeyApiClient apiClient, NoteRepository repository, string filePath = "notes.json", bool append = false)
    {
        _apiClient = apiClient;
        _repository = repository;
        _filePath = filePath;
        _append = append;
    }

    public async Task<int> ExecuteAsync(int days)
    {
        var utcNow = DateTime.UtcNow;
        var convertedStartDate = utcNow.AddDays(-days);
        var convertedEndDate = utcNow;

        var jstNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneHelper.Jst);
        System.Console.WriteLine($"Fetching notes from {jstNow.AddDays(-days):yyyy-MM-dd} to {jstNow:yyyy-MM-dd} (JST)...");

        return await FetchAndSaveAsync(convertedStartDate, convertedEndDate);
    }

    public async Task<int> ExecuteAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        var convertedStartDate = TimeZoneHelper.ConvertToUtc(startDate);
        var convertedEndDate = TimeZoneHelper.ConvertToUtc(endDate);

        System.Console.WriteLine($"Fetching notes from {startDate:yyyy-MM-dd HH:mm:ss} (JST) to {endDate:yyyy-MM-dd HH:mm:ss} (JST)...");

        return await FetchAndSaveAsync(convertedStartDate, convertedEndDate);
    }

    private async Task<int> FetchAndSaveAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var notes = await _apiClient.GetNotesWithRetry(startDate, endDate, maxRetries: 3,
                progress: msg => System.Console.WriteLine(msg));

            if (notes == null || !notes.Any())
            {
                System.Console.WriteLine("No notes found.");
                return 0;
            }

            List<Note> toSave;
            if (_append)
            {
                var existing = await _repository.LoadFromFileAsync(_filePath);
                toSave = notes.Concat(existing)
                    .GroupBy(n => n.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            else
            {
                toSave = notes.ToList();
            }

            await _repository.SaveToFileAsync(toSave, _filePath);
            System.Console.WriteLine($"Saved {toSave.Count} notes to {_filePath}");
            return 0;
        }
        catch (UnauthorizedException ex)
        {
            System.Console.Error.WriteLine($"Error: Unauthorized - {ex.Message}");
            return 1;
        }
    }
}
