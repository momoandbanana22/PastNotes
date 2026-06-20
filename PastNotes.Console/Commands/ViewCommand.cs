using PastNotes;

namespace PastNotes.Console.Commands;

public class ViewCommand
{
    private readonly NoteRepository _repository;
    private readonly string _filePath;

    public ViewCommand(NoteRepository repository, string filePath = "notes.json")
    {
        _repository = repository;
        _filePath = filePath;
    }

    public int Execute()
    {
        var notes = _repository.LoadFromFileAsync(_filePath);
        
        if (notes == null || !notes.Any())
        {
            System.Console.WriteLine("No notes found. Run 'fetch' command first.");
            return 1;
        }

        System.Console.WriteLine($"Total notes: {notes.Count()}");
        System.Console.WriteLine();
        
        foreach (var note in notes)
        {
            System.Console.WriteLine($"[{note.CreatedAt:yyyy-MM-dd HH:mm}] {note.Text}");
            System.Console.WriteLine($"  ID: {note.Id}");
            System.Console.WriteLine();
        }

        return 0;
    }
}
