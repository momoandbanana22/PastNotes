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

    // TDD: TST-10 - トークンなしでexit 1とエラーメッセージ
    // TDD: fetch --append --start ... --end ... の順序でも正しく解析されること
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenAppendPrecedesStartEnd_ParsesArgsCorrectly()
    {
        // Arrange
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            // --append を --start/--end より前に置く（バグ再現）
            var args = new[]
            {
                "fetch", "--append",
                "--start", "2024-01-01",
                "--end", "2024-01-31",
                "--token", "faketoken",
                "--instance-url", "http://localhost:1"
            };

            // Act
            var result = await Program.Main(args);

            System.Console.SetOut(originalOutput);

            // Assert: 引数解析は成功するので「Usage:」ではなく network error になること
            Assert.DoesNotContain("Usage: PastNotes.Console fetch --days", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
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

            System.Console.SetOut(originalOutput);

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
}
