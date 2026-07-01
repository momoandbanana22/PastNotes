using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Cli;

public static class ViewCommandHandler
{
    public static async Task<int> RunAsync(string[] args)
    {
        var repository = new NoteRepository();
        var showId = args.Contains("--show-id");
        DateTime? viewStart = null, viewEnd = null;
        var startIdx = Array.IndexOf(args, "--start");
        var endIdx   = Array.IndexOf(args, "--end");
        if (startIdx >= 0)
        {
            if (startIdx + 1 >= args.Length)
            {
                System.Console.WriteLine("Error: --start requires a date value. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                return 1;
            }
            if (!DateTime.TryParse(args[startIdx + 1], out var vs))
            {
                System.Console.WriteLine("Error: Invalid start date format. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                return 1;
            }
            viewStart = vs;
        }
        if (endIdx >= 0)
        {
            if (endIdx + 1 >= args.Length)
            {
                System.Console.WriteLine("Error: --end requires a date value. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                return 1;
            }
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
            System.Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
