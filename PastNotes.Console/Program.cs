using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console;

public class Program
{
    private static readonly HttpClient _sharedHttpClient = new HttpClient();

    public static async Task<int> Main(string[] args)
    {
        // .envファイルをロード（環境変数が未設定の場合のみ）
        PastNotes.DotEnvLoader.Load(".env");

        if (args.Length == 0)
        {
            System.Console.WriteLine("Usage: PastNotes.Console <command> [options]");
            System.Console.WriteLine("Commands:");
            System.Console.WriteLine("  fetch --days <days> [--append] [--token <token>] [--instance-url <url>]");
            System.Console.WriteLine("  fetch --start <date> --end <date> [--append] [--token <token>] [--instance-url <url>]");
            System.Console.WriteLine("  search <keyword> [--start <date>] [--end <date>]");
            System.Console.WriteLine("  view [--show-id] [--start <date>] [--end <date>]");
            System.Console.WriteLine("  view-html [--open]");
            System.Console.WriteLine("Date format: yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (JST)");
            System.Console.WriteLine("Auth: set MISSKEY_API_TOKEN env var, use --token arg, or create .env file");
            return 1;
        }

        var command = args[0];

        if (command == "fetch")
        {
            // --instance-url / --token 引数を優先、なければ環境変数
            var instanceUrlIdx = Array.IndexOf(args, "--instance-url");
            var tokenIdx       = Array.IndexOf(args, "--token");
            var instanceUrl = (instanceUrlIdx >= 0 && instanceUrlIdx + 1 < args.Length)
                ? args[instanceUrlIdx + 1]
                : Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
            var apiToken = (tokenIdx >= 0 && tokenIdx + 1 < args.Length)
                ? args[tokenIdx + 1]
                : Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

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
                var daysIdx  = Array.IndexOf(args, "--days");
                var sIdx     = Array.IndexOf(args, "--start");
                var eIdx     = Array.IndexOf(args, "--end");

                if (daysIdx >= 0 && daysIdx + 1 < args.Length)
                {
                    if (!int.TryParse(args[daysIdx + 1], out int days))
                    {
                        System.Console.WriteLine("Error: days must be a number");
                        return 1;
                    }

                    var result = await fetchCommand.ExecuteAsync(days);
                    return result;
                }
                else if (sIdx >= 0 && sIdx + 1 < args.Length && eIdx >= 0 && eIdx + 1 < args.Length)
                {
                    if (!DateTime.TryParse(args[sIdx + 1], out DateTime startDate))
                    {
                        System.Console.WriteLine("Error: Invalid start date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                        return 1;
                    }

                    if (!DateTime.TryParse(args[eIdx + 1], out DateTime endDate))
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
            if (sStartIdx >= 0 && sStartIdx + 1 < args.Length)
            {
                if (!DateTime.TryParse(args[sStartIdx + 1], out var ss))
                {
                    System.Console.WriteLine("Error: Invalid start date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                    return 1;
                }
                searchStart = ss;
            }
            if (sEndIdx >= 0 && sEndIdx + 1 < args.Length)
            {
                if (!DateTime.TryParse(args[sEndIdx + 1], out var se))
                {
                    System.Console.WriteLine("Error: Invalid end date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                    return 1;
                }
                searchEnd = se;
            }
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
            if (startIdx >= 0 && startIdx + 1 < args.Length)
            {
                if (!DateTime.TryParse(args[startIdx + 1], out var vs))
                {
                    System.Console.WriteLine("Error: Invalid start date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                    return 1;
                }
                viewStart = vs;
            }
            if (endIdx >= 0 && endIdx + 1 < args.Length)
            {
                if (!DateTime.TryParse(args[endIdx + 1], out var ve))
                {
                    System.Console.WriteLine("Error: Invalid end date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                    return 1;
                }
                viewEnd = ve;
            }
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
