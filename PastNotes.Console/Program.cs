using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console;

public class Program
{
    private static readonly HttpClient _sharedHttpClient = new HttpClient();

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            System.Console.WriteLine("Usage: PastNotes.Console <command> [options]");
            System.Console.WriteLine("Commands:");
            System.Console.WriteLine("  fetch --days <days>  Fetch notes from the last N days");
            System.Console.WriteLine("  search <keyword>     Search notes by keyword");
            System.Console.WriteLine("  view [--show-id]     View all notes (use --show-id to display note IDs)");
            return 1;
        }

        var command = args[0];

        if (command == "fetch")
        {
            if (args.Length < 3 || args[1] != "--days")
            {
                System.Console.WriteLine("Usage: PastNotes.Console fetch --days <days>");
                return 1;
            }

            if (!int.TryParse(args[2], out int days))
            {
                System.Console.WriteLine("Error: days must be a number");
                return 1;
            }

            var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
            var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

            if (string.IsNullOrEmpty(apiToken))
            {
                System.Console.WriteLine("Error: MISSKEY_API_TOKEN environment variable is required");
                return 1;
            }

            var apiClient = new MisskeyApiClient(instanceUrl, apiToken, _sharedHttpClient);
            var repository = new NoteRepository();
            var fetchCommand = new FetchCommand(apiClient, repository);

            try
            {
                var result = await fetchCommand.ExecuteAsync(days);
                return result;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        if (command == "search")
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("Usage: PastNotes.Console search <keyword>");
                return 1;
            }

            var repository = new NoteRepository();
            var searchCommand = new SearchCommand(repository);
            var keyword = args[1];

            try
            {
                var result = await searchCommand.ExecuteAsync(keyword);
                return result;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        if (command == "view")
        {
            var repository = new NoteRepository();
            var showId = args.Contains("--show-id");
            var viewCommand = new ViewCommand(repository, showId: showId);

            try
            {
                var result = await viewCommand.ExecuteAsync();
                return result;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        System.Console.WriteLine($"Unknown command: {command}");
        return 1;
    }
}
