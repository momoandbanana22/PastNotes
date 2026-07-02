using System.Diagnostics;
using System.Text;

namespace PastNotes.Console.Tests;

public class EndToEndProcessTests
{
    // TDD: TST-38 - dotnet test 実行時の文字化けは Program.Main を経由しないテストプロセス自身の
    // コンソールエンコーディングに起因する現象であり、実際にビルドされた .exe (apphost) を
    // 別プロセスとして起動した場合は Console.OutputEncoding = UTF8 (BUG-40) が機能し
    // 文字化けしないことを検証する。
    [Fact]
    [Trait("Category", "Unit")]
    public async Task RealProcess_WhenViewCommandOutputsJapaneseText_StandardOutputIsValidUtf8()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_e2e_process_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var notesJson = """
            [
              {
                "CreatedAt": "2024-01-01T00:00:00Z",
                "Id": "e2e-1",
                "Text": "テスト投稿です",
                "Files": [
                  { "Id": "f1", "Url": "https://example.com/image.png", "Type": "image/png", "Name": "画像ファイル.png" }
                ]
              }
            ]
            """;
            await File.WriteAllTextAsync(Path.Combine(tempDir, "notes.json"), notesJson, new UTF8Encoding(false));

            var exePath = Path.Combine(AppContext.BaseDirectory, "PastNotes.exe");
            Assert.True(File.Exists(exePath), $"ビルドされた exe が見つかりません: {exePath}。'dotnet build' を先に実行してください。");

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "view --show-id",
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            Assert.Equal(0, process.ExitCode);
            Assert.Contains("テスト投稿です", output);
            Assert.Contains("添付ファイル:", output);
            Assert.Contains("画像ファイル.png", output);
            Assert.DoesNotContain('�', output);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
