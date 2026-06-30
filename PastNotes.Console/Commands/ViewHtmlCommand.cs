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
        
        if (!notes.Any())
        {
            System.Console.WriteLine("No notes found. Run 'fetch' command first.");
            return 1;
        }

        if (!Directory.Exists(_outputDir))
        {
            Directory.CreateDirectory(_outputDir);
        }

        var generator = new NoteHtmlGenerator();
        var outputPath = Path.Combine(_outputDir, "notes.html");
        generator.GenerateHtmlForAllNotes(notes, outputPath);

        System.Console.WriteLine($"Generated HTML file: {outputPath}");

        if (_openBrowser)
        {
            generator.OpenInBrowser(outputPath);
            System.Console.WriteLine($"Opened {outputPath} in browser");
        }

        return 0;
    }
}
