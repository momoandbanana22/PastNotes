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
            System.Console.WriteLine("  fetch --days <days>                    Fetch notes from the last N days");
            System.Console.WriteLine("  fetch --start <date> --end <date>      Fetch notes within date range (JST)");
            System.Console.WriteLine("  search <keyword>                       Search notes by keyword");
            System.Console.WriteLine("  view [--show-id]                       View all notes (use --show-id to display note IDs)");
            System.Console.WriteLine("  view-html [--open]                      Generate HTML files for notes (use --open to open in browser)");
            System.Console.WriteLine("Date format: yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
            return 1;
        }

        var command = args[0];

        if (command == "fetch")
        {
            var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
            var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

            if (string.IsNullOrEmpty(apiToken))
            {
                System.Console.WriteLine("Error: MISSKEY_API_TOKEN environment variable is required");
                return 1;
            }

            var apiClient = new MisskeyApiClient(instanceUrl, apiToken, _sharedHttpClient);
            var repository = new NoteRepository();
            var append = args.Contains("--append");
            var fetchCommand = new FetchCommand(apiClient, repository, append: append);

            try
            {
                // Check if using --days option
                if (args.Length >= 3 && args[1] == "--days")
                {
                    if (!int.TryParse(args[2], out int days))
                    {
                        System.Console.WriteLine("Error: days must be a number");
                        return 1;
                    }

                    var result = await fetchCommand.ExecuteAsync(days);
                    return result;
                }
                // Check if using --start and --end options
                else if (args.Length >= 5 && args[1] == "--start" && args[3] == "--end")
                {
                    if (!DateTime.TryParse(args[2], out DateTime startDate))
                    {
                        System.Console.WriteLine("Error: Invalid start date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                        return 1;
                    }

                    if (!DateTime.TryParse(args[4], out DateTime endDate))
                    {
                        System.Console.WriteLine("Error: Invalid end date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                        return 1;
                    }

                    var result = await fetchCommand.ExecuteAsync(startDate, endDate);
                    return result;
                }
                else
                {
                    System.Console.WriteLine("Usage: PastNotes.Console fetch --days <days>");
                    System.Console.WriteLine("   or: PastNotes.Console fetch --start <date> --end <date>");
                    System.Console.WriteLine("Date format: yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                    System.Console.WriteLine("Note: Date ranges are treated as JST (Japan Standard Time)");
                    return 1;
                }
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
            DateTime? searchStart = null, searchEnd = null;
            var sStartIdx = Array.IndexOf(args, "--start");
            var sEndIdx   = Array.IndexOf(args, "--end");
            if (sStartIdx >= 0 && sStartIdx + 1 < args.Length && DateTime.TryParse(args[sStartIdx + 1], out var ss)) searchStart = ss;
            if (sEndIdx   >= 0 && sEndIdx   + 1 < args.Length && DateTime.TryParse(args[sEndIdx   + 1], out var se)) searchEnd   = se;
            var searchCommand = new SearchCommand(repository, startDate: searchStart, endDate: searchEnd);
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
            DateTime? viewStart = null, viewEnd = null;
            var startIdx = Array.IndexOf(args, "--start");
            var endIdx   = Array.IndexOf(args, "--end");
            if (startIdx >= 0 && startIdx + 1 < args.Length && DateTime.TryParse(args[startIdx + 1], out var vs)) viewStart = vs;
            if (endIdx   >= 0 && endIdx   + 1 < args.Length && DateTime.TryParse(args[endIdx   + 1], out var ve)) viewEnd   = ve;
            var viewCommand = new ViewCommand(repository, showId: showId, startDate: viewStart, endDate: viewEnd);

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

        if (command == "view-html")
        {
            var repository = new NoteRepository();
            var openBrowser = args.Contains("--open");
            var viewHtmlCommand = new ViewHtmlCommand(repository, openBrowser: openBrowser);

            try
            {
                var result = viewHtmlCommand.Execute();
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
