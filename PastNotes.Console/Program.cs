using PastNotes;
using PastNotes.Console.Cli;

namespace PastNotes.Console;

public class Program
{
    private static readonly HttpClient _sharedHttpClient = new HttpClient();

    public static async Task<int> Main(string[] args)
    {
        // 日本語出力が環境依存のデフォルトエンコーディング(Windowsの場合CP932)と衝突して
        // 文字化けするのを防ぐため、標準出力のエンコーディングをUTF-8に固定する
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;

        // .envファイルをロード（環境変数が未設定の場合のみ）
        PastNotes.DotEnvLoader.Load(".env");

        if (args.Length == 0)
        {
            System.Console.WriteLine("Usage: PastNotes.Console <command> [options]");
            System.Console.WriteLine("Commands:");
            System.Console.WriteLine("  fetch --days <days> [--append] [--token <token>] [--instance-url <url>] [--max-retries <n>]");
            System.Console.WriteLine("  fetch --start <date> --end <date> [--append] [--token <token>] [--instance-url <url>] [--max-retries <n>]");
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
            return await FetchCommandHandler.RunAsync(args, _sharedHttpClient);
        }

        if (command == "search")
        {
            return await SearchCommandHandler.RunAsync(args);
        }

        if (command == "view")
        {
            return await ViewCommandHandler.RunAsync(args);
        }

        if (command == "view-html")
        {
            return await ViewHtmlCommandHandler.RunAsync(args);
        }

        System.Console.Error.WriteLine($"Unknown command: {command}");
        return 1;
    }
}
