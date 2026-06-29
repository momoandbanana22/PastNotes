using PastNotes;

namespace PastNotes.Console.Commands;

public class SearchCommand
{
    private readonly NoteRepository _repository;
    private readonly string _filePath;
    private readonly DateTime? _startDate;
    private readonly DateTime? _endDate;

    public SearchCommand(NoteRepository repository, string filePath = "notes.json",
        DateTime? startDate = null, DateTime? endDate = null)
    {
        _repository = repository;
        _filePath = filePath;
        _startDate = startDate.HasValue ? startDate.Value.AddHours(-9) : null;
        _endDate   = endDate.HasValue   ? endDate.Value.AddHours(-9)   : null;
    }

    public int Execute(string keyword)
    {
        var notes = _repository.LoadFromFileAsync(_filePath).GetAwaiter().GetResult();
        
        if (notes == null || !notes.Any())
        {
            System.Console.WriteLine("No notes found. Run 'fetch' command first.");
            return 1;
        }

        if (_startDate.HasValue || _endDate.HasValue)
        {
            notes = _repository.FilterByDateRange(notes,
                _startDate ?? DateTime.MinValue,
                _endDate ?? DateTime.MaxValue);
        }

        var results = _repository.SearchByKeyword(notes, keyword).ToList();

        System.Console.WriteLine($"Found {results.Count} notes matching '{keyword}':");
        foreach (var note in results)
        {
            var jstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(note.CreatedAt, DateTimeKind.Utc), TimeZoneHelper.Jst);
            System.Console.WriteLine($"[{jstTime:yyyy-MM-dd HH:mm:ss}] {note.Text}");
        }

        return 0;
    }

    public async Task<int> ExecuteAsync(string keyword)
    {
        var notes = await _repository.LoadFromFileAsync(_filePath);

        if (notes == null || !notes.Any())
        {
            System.Console.WriteLine("No notes found. Run 'fetch' command first.");
            return 1;
        }

        if (_startDate.HasValue || _endDate.HasValue)
        {
            notes = _repository.FilterByDateRange(notes,
                _startDate ?? DateTime.MinValue,
                _endDate ?? DateTime.MaxValue);
        }

        var results = _repository.SearchByKeyword(notes, keyword).ToList();

        System.Console.WriteLine($"Found {results.Count} notes matching '{keyword}':");
        foreach (var note in results)
        {
            var jstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(note.CreatedAt, DateTimeKind.Utc), TimeZoneHelper.Jst);
            System.Console.WriteLine($"[{jstTime:yyyy-MM-dd HH:mm:ss}] {note.Text}");
        }

        return 0;
    }
}
