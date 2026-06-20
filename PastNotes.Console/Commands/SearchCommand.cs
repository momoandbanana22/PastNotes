using PastNotes;

namespace PastNotes.Console.Commands;

public class SearchCommand
{
    private readonly NoteRepository _repository;
    private readonly string _filePath;

    public SearchCommand(NoteRepository repository, string filePath = "notes.json")
    {
        _repository = repository;
        _filePath = filePath;
    }

    public int Execute(string keyword)
    {
        var notes = _repository.LoadFromFileAsync(_filePath);
        
        if (notes == null || !notes.Any())
        {
            System.Console.WriteLine("No notes found. Run 'fetch' command first.");
            return 1;
        }

        var results = _repository.SearchByKeyword(notes, keyword);
        
        System.Console.WriteLine($"Found {results.Count()} notes matching '{keyword}':");
        foreach (var note in results)
        {
            System.Console.WriteLine($"[{note.CreatedAt:yyyy-MM-dd HH:mm}] {note.Text}");
        }

        return 0;
    }
}
