using PastNotes;

namespace PastNotes.Console.Commands;

public class ViewCommand
{
    private readonly NoteRepository _repository;
    private readonly string _filePath;
    private readonly bool _showId;
    private readonly DateTime? _startDate;
    private readonly DateTime? _endDate;

    public ViewCommand(NoteRepository repository, string filePath = "notes.json", bool showId = false,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        _repository = repository;
        _filePath = filePath;
        _showId = showId;
        // JST→UTC変換して保持
        _startDate = startDate.HasValue ? startDate.Value.AddHours(-9) : null;
        _endDate   = endDate.HasValue   ? endDate.Value.AddHours(-9)   : null;
    }

    public int Execute()
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

        System.Console.WriteLine($"Total notes: {notes.Count()}");
        System.Console.WriteLine();
        
        foreach (var note in notes)
        {
            var jstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(note.CreatedAt, DateTimeKind.Utc), TimeZoneHelper.Jst);
            System.Console.WriteLine($"[{jstTime:yyyy-MM-dd HH:mm:ss}] {note.Text}");
            if (_showId)
            {
                System.Console.WriteLine($"  ID: {note.Id}");
            }
            if (note.Files.Any())
            {
                System.Console.WriteLine("  添付ファイル:");
                foreach (var file in note.Files)
                {
                    System.Console.WriteLine($"    - {file.Name} ({file.Type})");
                    System.Console.WriteLine($"      URL: {file.Url}");
                }
            }
            System.Console.WriteLine();
        }

        return 0;
    }

    public async Task<int> ExecuteAsync()
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

        System.Console.WriteLine($"Total notes: {notes.Count()}");
        System.Console.WriteLine();
        
        foreach (var note in notes)
        {
            var jstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(note.CreatedAt, DateTimeKind.Utc), TimeZoneHelper.Jst);
            System.Console.WriteLine($"[{jstTime:yyyy-MM-dd HH:mm:ss}] {note.Text}");
            if (_showId)
            {
                System.Console.WriteLine($"  ID: {note.Id}");
            }
            if (note.Files.Any())
            {
                System.Console.WriteLine("  添付ファイル:");
                foreach (var file in note.Files)
                {
                    System.Console.WriteLine($"    - {file.Name} ({file.Type})");
                    System.Console.WriteLine($"      URL: {file.Url}");
                }
            }
            System.Console.WriteLine();
        }

        return 0;
    }
}
