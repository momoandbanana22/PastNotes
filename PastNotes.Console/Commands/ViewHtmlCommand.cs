using PastNotes;

namespace PastNotes.Console.Commands;

public class ViewHtmlCommand
{
    private readonly NoteRepository _repository;
    private readonly string _filePath;
    private readonly string _outputDir;
    private readonly bool _openBrowser;

    public ViewHtmlCommand(NoteRepository repository, string filePath = "notes.json", string outputDir = "html_output", bool openBrowser = false)
    {
        _repository = repository;
        _filePath = filePath;
        _outputDir = outputDir;
        _openBrowser = openBrowser;
    }

    public int Execute()
    {
        var notes = _repository.LoadFromFileAsync(_filePath).GetAwaiter().GetResult();
        
        if (notes == null || !notes.Any())
        {
            System.Console.WriteLine("No notes found. Run 'fetch' command first.");
            return 1;
        }

        if (!Directory.Exists(_outputDir))
        {
            Directory.CreateDirectory(_outputDir);
        }

        var generator = new NoteHtmlGenerator();
        foreach (var note in notes)
        {
            var outputPath = Path.Combine(_outputDir, $"{note.Id}.html");
            generator.GenerateHtml(note, outputPath);
        }

        System.Console.WriteLine($"Generated {notes.Count()} HTML files in {_outputDir}");

        if (_openBrowser && notes.Any())
        {
            var firstHtmlFile = Path.Combine(_outputDir, $"{notes.First().Id}.html");
            generator.OpenInBrowser(firstHtmlFile);
            System.Console.WriteLine($"Opened {firstHtmlFile} in browser");
        }

        return 0;
    }
}
