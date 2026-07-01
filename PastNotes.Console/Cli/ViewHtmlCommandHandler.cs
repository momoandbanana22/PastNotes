using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Cli;

public static class ViewHtmlCommandHandler
{
    public static Task<int> RunAsync(string[] args)
    {
        var repository = new NoteRepository();
        var openBrowser = args.Contains("--open");
        var viewHtmlCommand = new ViewHtmlCommand(repository, openBrowser: openBrowser);

        try
        {
            var result = viewHtmlCommand.Execute();
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Error: {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
