using PastNotes;

namespace PastNotes.Console.Commands;

public class ViewCommand
{
    private readonly NoteRepository _repository;
    private readonly string _filePath;
    private readonly bool _showId;

    public ViewCommand(NoteRepository repository, string filePath = "notes.json", bool showId = false)
    {
        _repository = repository;
        _filePath = filePath;
        _showId = showId;
    }

    public int Execute()
    {
        var notes = _repository.LoadFromFileAsync(_filePath).GetAwaiter().GetResult();
        
        if (notes == null || !notes.Any())
        {
            System.Console.WriteLine("No notes found. Run 'fetch' command first.");
            return 1;
        }

        System.Console.WriteLine($"Total notes: {notes.Count()}");
        System.Console.WriteLine();
        
        foreach (var note in notes)
        {
            System.Console.WriteLine($"[{note.CreatedAt:yyyy-MM-dd HH:mm:ss}] {note.Text}");
            if (_showId)
            {
                System.Console.WriteLine($"  ID: {note.Id}");
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

        System.Console.WriteLine($"Total notes: {notes.Count()}");
        System.Console.WriteLine();
        
        foreach (var note in notes)
        {
            System.Console.WriteLine($"[{note.CreatedAt:yyyy-MM-dd HH:mm:ss}] {note.Text}");
            if (_showId)
            {
                System.Console.WriteLine($"  ID: {note.Id}");
            }
            System.Console.WriteLine();
        }

        return 0;
    }
}
