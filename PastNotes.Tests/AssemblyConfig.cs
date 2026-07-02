// Console.SetOut はグローバル状態のため、並列実行するとテスト間で干渉する。
// このアセンブリ内のテストは直列実行する。
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace PastNotes.Tests;

internal static class TestAssemblyInitializer
{
    // BUG-45: dotnet test 実行中、テストプロセス(testhost.exe)自体のコンソールエンコーディングは
    // Program.Main を経由しないため既定値(Windowsの場合CP932)のままになり、統合テストが直接呼び出す
    // 日本語メッセージがログ上で文字化けする。テストプロセス全体にも本番と同じ UTF-8 を適用する。
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void SetConsoleOutputEncodingToUtf8()
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
    }
}
