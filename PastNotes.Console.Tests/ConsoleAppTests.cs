namespace PastNotes.Console.Tests;

public class ConsoleAppTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenCalledWithInvalidParameters_ReturnsFailure()
    {
        // Arrange
        var args = new[] { "fetch" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenCalledWithInvalidDays_ReturnsFailure()
    {
        // Arrange
        var args = new[] { "fetch", "--days", "invalid" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FetchCommand_WhenCalledWithRealApi_ShouldFetchNotes()
    {
        // Arrange
        var instanceUrl = Environment.GetEnvironmentVariable("MISSKEY_INSTANCE_URL") ?? "https://misskey.io";
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");

        if (string.IsNullOrEmpty(apiToken))
        {
            Assert.Fail("統合テストを実行するには環境変数を設定してください。");
        }

        // Cleanup any existing notes.json
        if (File.Exists("notes.json"))
        {
            File.Delete("notes.json");
        }

        var args = new[] { "fetch", "--days", "30" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.Equal(0, result);
        
        // Verify that notes were saved
        var repository = new PastNotes.NoteRepository();
        var notes = await repository.LoadFromFileAsync("notes.json");
        Assert.NotNull(notes);
        Assert.True(notes.Any(), "Expected at least one note to be fetched");
        
        // Cleanup
        if (File.Exists("notes.json"))
        {
            File.Delete("notes.json");
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenCalledWithInvalidDateFormat_ReturnsFailure()
    {
        // Arrange
        var args = new[] { "fetch", "--start", "invalid-date", "--end", "2024-01-31" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    // TDD: BUG-36 - --token に値なしで渡した場合はエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenTokenFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--token" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--token", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenInstanceUrlFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--instance-url" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--instance-url", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TDD: TST-10 - トークンなしでexit 1とエラーメッセージ
    // TDD: fetch --append --start ... --end ... の順序でも正しく解析されること (TST-21)
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenAppendPrecedesStartEnd_ParsesArgsCorrectly()
    {
        // --token を渡さず env var もクリアすることでトークン検証エラーで早期リターンさせ
        // ネットワーク呼び出しを回避する
        var originalToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        Environment.SetEnvironmentVariable("MISSKEY_API_TOKEN", null);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            // --append を --start/--end より前に置く（引数順序バグの再現）
            var args = new[]
            {
                "fetch", "--append",
                "--start", "2024-01-01",
                "--end", "2024-01-31"
            };

            // Act
            var result = await Program.Main(args);

            // Assert: 引数解析は成功するので「Usage:」ではなくトークン不足エラーになること
            Assert.Equal(1, result);
            Assert.DoesNotContain("Usage: PastNotes.Console fetch --days", stringWriter.ToString());
            Assert.Contains("MISSKEY_API_TOKEN", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            Environment.SetEnvironmentVariable("MISSKEY_API_TOKEN", originalToken);
        }
    }

    // TDD: BUG-34 - search/view で --start/--end に値を渡さないとエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenStartFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "search", "keyword", "--start" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--start", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenEndFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "view", "--start", "2024-01-01", "--end" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--end", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TDD: BUG-32 - search/view で不正な日付を指定するとエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenInvalidStartDate_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "search", "keyword", "--start", "not-a-date", "--end", "2024-01-31" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid start date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenInvalidEndDate_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "view", "--start", "2024-01-01", "--end", "not-a-date" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid end date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenApiTokenMissing_ReturnsOneAndPrintsError()
    {
        // Arrange: 環境変数を一時的にクリア
        var originalToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        Environment.SetEnvironmentVariable("MISSKEY_API_TOKEN", null);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30" };

            // Act
            var result = await Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.Contains("MISSKEY_API_TOKEN", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            Environment.SetEnvironmentVariable("MISSKEY_API_TOKEN", originalToken);
        }
    }

    // TDD: DOC-4 - --max-retries がusageメッセージに記載されていること
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenCalledWithNoArgs_UsageContainsMaxRetries()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var result = await Program.Main(Array.Empty<string>());

            Assert.Equal(1, result);
            Assert.Contains("--max-retries", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TDD: BUG-37 - --max-retries に値なしで渡した場合はエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenMaxRetriesFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--max-retries" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--max-retries", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TDD: BUG-37 - --max-retries に数値以外の値を渡した場合はエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenMaxRetriesIsNotANumber_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--max-retries", "abc" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--max-retries", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: search にキーワードなし → Usage 表示パス
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenNoKeyword_ReturnsOneAndPrintsUsage()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var result = await Program.Main(new[] { "search" });
            Assert.Equal(1, result);
            Assert.Contains("Usage:", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: search --end に無効な日付
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenInvalidEndDate_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "search", "keyword", "--start", "2024-01-01", "--end", "not-a-date" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid end date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: view --start に値なし
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenStartFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "view", "--start" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--start", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: view --start に無効な日付
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenInvalidStartDate_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "view", "--start", "not-a-date" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid start date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: Unknown command パスのカバレッジ
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenCalledWithUnknownCommand_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var result = await Program.Main(new[] { "bogus-command" });
            Assert.Equal(1, result);
            Assert.Contains("Unknown command", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: fetch --start のみ（--end なし）→ Usage 表示パス
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenOnlyStartDateProvided_ReturnsOneAndPrintsUsage()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var args = new[] { "fetch", "--token", "dummy-token", "--start", "2024-01-01" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Usage:", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: view-html コマンド（ノートなし）パス
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenViewHtmlWithNoNotes_ReturnsOneAndPrintsMessage()
    {
        if (File.Exists("notes.json"))
            File.Delete("notes.json");

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var result = await Program.Main(new[] { "view-html" });
            Assert.Equal(1, result);
            Assert.Contains("No notes found", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }
    }

    // TST-24: view-html コマンド（JSON 破損）→ catch パス
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenViewHtmlWithCorruptedJson_ReturnsOneAndPrintsError()
    {
        await File.WriteAllTextAsync("notes.json", "{ not valid json }}");

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var result = await Program.Main(new[] { "view-html" });
            Assert.Equal(1, result);
            Assert.Contains("Error:", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            if (File.Exists("notes.json"))
                File.Delete("notes.json");
            if (Directory.Exists("html_output"))
                Directory.Delete("html_output", recursive: true);
        }
    }
}
