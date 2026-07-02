namespace PastNotes.Console.Tests;

public class TestAssemblyInitializerTests
{
    // TDD: BUG-45 - dotnet test 実行中、テストプロセス(testhost.exe)自体のコンソールエンコーディングは
    // Program.Main を経由しないため既定値(Windowsの場合CP932)のままになる。統合テストが直接呼び出す
    // MisskeyApiClient・ViewCommand 等の日本語メッセージがログ上で文字化けし、目視でも自動検証でも
    // 問題解消の有無を判別できなくなるため、テストプロセス全体にも UTF-8 を適用する。
    [Fact]
    [Trait("Category", "Unit")]
    public void TestAssembly_WhenLoaded_SetsConsoleOutputEncodingToUtf8()
    {
        Assert.Equal("utf-8", System.Console.OutputEncoding.WebName);
    }
}
