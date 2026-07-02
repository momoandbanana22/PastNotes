using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Cli;

public static class FetchCommandHandler
{
    public static async Task<int> RunAsync(string[] args, HttpClient httpClient)
    {
        // --instance-url / --token 引数を優先、なければ環境変数
        var instanceUrlIdx = Array.IndexOf(args, "--instance-url");
        var tokenIdx       = Array.IndexOf(args, "--token");

        if (instanceUrlIdx >= 0 && instanceUrlIdx + 1 >= args.Length)
        {
            System.Console.Error.WriteLine("Error: --instance-url requires a URL value");
            return 1;
        }
        if (tokenIdx >= 0 && tokenIdx + 1 >= args.Length)
        {
            System.Console.Error.WriteLine("Error: --token requires a token value");
            return 1;
        }
        var maxRetriesIdx = Array.IndexOf(args, "--max-retries");
        if (maxRetriesIdx >= 0)
        {
            if (maxRetriesIdx + 1 >= args.Length || !int.TryParse(args[maxRetriesIdx + 1], out _))
            {
                System.Console.Error.WriteLine("Error: --max-retries requires a number value");
                return 1;
            }
        }

        var instanceUrl = (instanceUrlIdx >= 0)
            ? args[instanceUrlIdx + 1]
            : Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
        var apiToken = (tokenIdx >= 0)
            ? args[tokenIdx + 1]
            : Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(apiToken))
        {
            System.Console.Error.WriteLine("Error: MISSKEY_API_TOKEN environment variable is required");
            return 1;
        }

        var apiClient = new MisskeyApiClient(instanceUrl, apiToken, httpClient);
        var repository = new NoteRepository();
        var append = args.Contains("--append");
        var maxRetries = (maxRetriesIdx >= 0) ? int.Parse(args[maxRetriesIdx + 1]) : 3;
        var fetchCommand = new FetchCommand(apiClient, repository, append: append, maxRetries: maxRetries);

        try
        {
            var daysIdx  = Array.IndexOf(args, "--days");
            var sIdx     = Array.IndexOf(args, "--start");
            var eIdx     = Array.IndexOf(args, "--end");

            if (daysIdx >= 0 && daysIdx + 1 >= args.Length)
            {
                System.Console.Error.WriteLine("Error: --days requires a number value");
                return 1;
            }

            if (daysIdx >= 0 && (sIdx >= 0 || eIdx >= 0))
            {
                System.Console.Error.WriteLine("Error: --days cannot be used together with --start/--end");
                return 1;
            }

            if (daysIdx >= 0 && daysIdx + 1 < args.Length)
            {
                if (!int.TryParse(args[daysIdx + 1], out int days))
                {
                    System.Console.Error.WriteLine("Error: days must be a number");
                    return 1;
                }

                if (days < 0)
                {
                    System.Console.Error.WriteLine("Error: --days must be a non-negative number");
                    return 1;
                }

                var result = await fetchCommand.ExecuteAsync(days);
                return result;
            }
            else if (sIdx >= 0 && sIdx + 1 < args.Length && eIdx >= 0 && eIdx + 1 < args.Length)
            {
                if (!DateTime.TryParse(args[sIdx + 1], out DateTime startDate))
                {
                    System.Console.Error.WriteLine("Error: Invalid start date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                    return 1;
                }

                if (!DateTime.TryParse(args[eIdx + 1], out DateTime endDate))
                {
                    System.Console.Error.WriteLine("Error: Invalid end date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
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
            System.Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
