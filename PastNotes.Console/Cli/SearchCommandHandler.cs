using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Cli;

public static class SearchCommandHandler
{
    public static async Task<int> RunAsync(string[] args)
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
        if (sStartIdx >= 0)
        {
            if (sStartIdx + 1 >= args.Length)
            {
                System.Console.Error.WriteLine("Error: --start requires a date value. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                return 1;
            }
            if (!DateTime.TryParse(args[sStartIdx + 1], out var ss))
            {
                System.Console.Error.WriteLine("Error: Invalid start date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                return 1;
            }
            searchStart = ss;
        }
        if (sEndIdx >= 0)
        {
            if (sEndIdx + 1 >= args.Length)
            {
                System.Console.Error.WriteLine("Error: --end requires a date value. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                return 1;
            }
            if (!DateTime.TryParse(args[sEndIdx + 1], out var se))
            {
                System.Console.Error.WriteLine("Error: Invalid end date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
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
            System.Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
