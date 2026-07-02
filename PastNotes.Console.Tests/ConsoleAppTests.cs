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
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--token" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--token", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenInstanceUrlFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--instance-url" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--instance-url", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
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
        var originalError = System.Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        System.Console.SetOut(outWriter);
        System.Console.SetError(errWriter);

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
            Assert.DoesNotContain("Usage: PastNotes.Console fetch --days", outWriter.ToString());
            Assert.Contains("MISSKEY_API_TOKEN", errWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            System.Console.SetError(originalError);
            Environment.SetEnvironmentVariable("MISSKEY_API_TOKEN", originalToken);
        }
    }

    // TDD: BUG-34 - search/view で --start/--end に値を渡さないとエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenStartFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "search", "keyword", "--start" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--start", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TDD: TST-35 - BUG-34 の横展開漏れ（search --end に値なしのケースが未検証だった）
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenEndFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "search", "keyword", "--end" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--end", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenEndFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "view", "--start", "2024-01-01", "--end" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--end", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TDD: BUG-32 - search/view で不正な日付を指定するとエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenInvalidStartDate_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "search", "keyword", "--start", "not-a-date", "--end", "2024-01-31" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid start date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenInvalidEndDate_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "view", "--start", "2024-01-01", "--end", "not-a-date" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid end date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenApiTokenMissing_ReturnsOneAndPrintsError()
    {
        // Arrange: 環境変数を一時的にクリア
        var originalToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        Environment.SetEnvironmentVariable("MISSKEY_API_TOKEN", null);

        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

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
            System.Console.SetError(originalError);
            Environment.SetEnvironmentVariable("MISSKEY_API_TOKEN", originalToken);
        }
    }

    // TDD: BUG-40 / TST-30 - 起動時に Console.OutputEncoding が UTF-8 に設定されているか
    // (日本語出力が Windows のデフォルトエンコーディング(CP932)と衝突して文字化けする問題への対処)
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenStarted_SetsConsoleOutputEncodingToUtf8()
    {
        await Program.Main(Array.Empty<string>());

        Assert.Equal("utf-8", System.Console.OutputEncoding.WebName);
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
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--max-retries" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--max-retries", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TDD: BUG-37 - --max-retries に数値以外の値を渡した場合はエラーを返すか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenMaxRetriesIsNotANumber_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--max-retries", "abc" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--max-retries", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TDD: BUG-47 - --days に値なしで渡した場合、他フラグ(--token/--instance-url/--max-retries)と
    // 同様に専用のエラーをstderrに返すか（現状はUsageにフォールスルーしてしまう）
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenDaysFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "fetch", "--token", "dummy", "--days" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--days", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TDD: BUG-48 - --days に負の値を渡すと、原因(--days)を名指ししたエラーになるか。
    // 修正前は ValidateDateRange の "Start date must be before end date" が投げられるだけで、
    // --days が原因だと分からない上に "Fetching notes from..." がstdoutに出てしまっていた。
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenDaysIsNegative_ReturnsOneAndPrintsError()
    {
        var originalOutput = System.Console.Out;
        var originalError = System.Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        System.Console.SetOut(outWriter);
        System.Console.SetError(errWriter);

        try
        {
            var args = new[] { "fetch", "--token", "dummy", "--days", "-5" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--days", errWriter.ToString());
            Assert.DoesNotContain("Fetching notes from", outWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            System.Console.SetError(originalError);
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

    // TDD: BUG-43 - キーワードを省略し --start から書き始めると、フラグ名がそのままキーワード扱いされてしまう
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchCommand_WhenKeywordOmittedButStartFlagGiven_ReturnsOneAndPrintsUsage()
    {
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            var result = await Program.Main(new[] { "search", "--start", "2024-01-01" });
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
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "search", "keyword", "--start", "2024-01-01", "--end", "not-a-date" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid end date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TST-24: view --start に値なし
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenStartFlagHasNoValue_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "view", "--start" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--start", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TST-24: view --start に無効な日付
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ViewCommand_WhenInvalidStartDate_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "view", "--start", "not-a-date" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("Invalid start date format", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TST-24: Unknown command パスのカバレッジ
    // TDD: BUG-46 - Unknown command は終了コード1のエラーのため stdout ではなく stderr に出力されるべき
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenCalledWithUnknownCommand_ReturnsOneAndPrintsErrorToStderr()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var result = await Program.Main(new[] { "bogus-command" });
            Assert.Equal(1, result);
            Assert.Contains("Unknown command", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
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

    // TDD: BUG-44 - --days と --start/--end を同時指定すると --start/--end が無言で無視されるべきではない
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenDaysAndStartEndBothProvided_ReturnsOneAndPrintsError()
    {
        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var args = new[] { "fetch", "--days", "30", "--start", "2024-01-01", "--end", "2024-01-31", "--token", "dummy-token" };
            var result = await Program.Main(args);
            Assert.Equal(1, result);
            Assert.Contains("--days", stringWriter.ToString());
            Assert.Contains("--start", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TST-24: view-html コマンド（ノートなし）パス
    // TDD: BUG-46 - No notes found は終了コード1のエラーのため stdout ではなく stderr に出力されるべき
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenViewHtmlWithNoNotes_ReturnsOneAndPrintsMessageToStderr()
    {
        if (File.Exists("notes.json"))
            File.Delete("notes.json");

        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        try
        {
            var result = await Program.Main(new[] { "view-html" });
            Assert.Equal(1, result);
            Assert.Contains("No notes found", stringWriter.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TDD: DESIGN-3 - view-html コマンド（JSON 破損）→ catch パスのエラーは stderr に出力されるべき
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenViewHtmlWithCorruptedJson_ReturnsOneAndPrintsErrorToStderr()
    {
        await File.WriteAllTextAsync("notes.json", "{ not valid json }}");

        var originalOutput = System.Console.Out;
        var originalError = System.Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        System.Console.SetOut(outWriter);
        System.Console.SetError(errWriter);

        try
        {
            var result = await Program.Main(new[] { "view-html" });
            Assert.Equal(1, result);
            Assert.Contains("Error:", errWriter.ToString());
            Assert.DoesNotContain("Error:", outWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            System.Console.SetError(originalError);
            if (File.Exists("notes.json"))
                File.Delete("notes.json");
            if (Directory.Exists("html_output"))
                Directory.Delete("html_output", recursive: true);
        }
    }

    // TDD: DESIGN-3 - search コマンドの例外（破損JSON）は stderr に出力されるべき
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenSearchWithCorruptedJson_ReturnsOneAndPrintsErrorToStderr()
    {
        await File.WriteAllTextAsync("notes.json", "{ not valid json }}");

        var originalOutput = System.Console.Out;
        var originalError = System.Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        System.Console.SetOut(outWriter);
        System.Console.SetError(errWriter);

        try
        {
            var result = await Program.Main(new[] { "search", "keyword" });
            Assert.Equal(1, result);
            Assert.Contains("Error:", errWriter.ToString());
            Assert.DoesNotContain("Error:", outWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            System.Console.SetError(originalError);
            if (File.Exists("notes.json"))
                File.Delete("notes.json");
        }
    }

    // TDD: DESIGN-3 - view コマンドの例外（破損JSON）は stderr に出力されるべき
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Main_WhenViewWithCorruptedJson_ReturnsOneAndPrintsErrorToStderr()
    {
        await File.WriteAllTextAsync("notes.json", "{ not valid json }}");

        var originalOutput = System.Console.Out;
        var originalError = System.Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        System.Console.SetOut(outWriter);
        System.Console.SetError(errWriter);

        try
        {
            var result = await Program.Main(new[] { "view" });
            Assert.Equal(1, result);
            Assert.Contains("Error:", errWriter.ToString());
            Assert.DoesNotContain("Error:", outWriter.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOutput);
            System.Console.SetError(originalError);
            if (File.Exists("notes.json"))
                File.Delete("notes.json");
        }
    }

    // TDD: TST-31 - fetch → search・view・view-html の一連のユーザーシナリオを通しで確認するE2Eテスト
    [Fact]
    [Trait("Category", "Integration")]
    public async Task EndToEndScenario_FetchThenSearchViewViewHtml_AllCommandsSucceedConsistently()
    {
        var apiToken = Environment.GetEnvironmentVariable("MISSKEY_API_TOKEN");
        if (string.IsNullOrEmpty(apiToken))
        {
            Assert.Fail("統合テストを実行するには環境変数を設定してください。");
        }

        if (File.Exists("notes.json"))
            File.Delete("notes.json");
        if (Directory.Exists("html_output"))
            Directory.Delete("html_output", recursive: true);

        try
        {
            // Act 1: fetch — 実APIから取得して notes.json に保存
            var fetchResult = await Program.Main(new[] { "fetch", "--days", "30" });
            Assert.Equal(0, fetchResult);

            var repository = new PastNotes.NoteRepository();
            var fetchedNotes = (await repository.LoadFromFileAsync("notes.json")).ToList();
            Assert.True(fetchedNotes.Any(), "Expected at least one note to be fetched");

            // Act 2: search — fetch で保存したノートに対して、存在しないキーワードで検索(0件・exit 0)
            var originalOutput = System.Console.Out;
            using var searchWriter = new StringWriter();
            System.Console.SetOut(searchWriter);
            int searchResult;
            try
            {
                searchResult = await Program.Main(new[] { "search", $"no-such-keyword-{Guid.NewGuid()}" });
            }
            finally
            {
                System.Console.SetOut(originalOutput);
            }
            Assert.Equal(0, searchResult);
            Assert.Contains("Found 0 notes matching", searchWriter.ToString());

            // Act 3: view — fetch で保存したノート件数と一致した件数が表示されること
            using var viewWriter = new StringWriter();
            System.Console.SetOut(viewWriter);
            int viewResult;
            try
            {
                viewResult = await Program.Main(new[] { "view" });
            }
            finally
            {
                System.Console.SetOut(originalOutput);
            }
            Assert.Equal(0, viewResult);
            Assert.Contains($"Total notes: {fetchedNotes.Count}", viewWriter.ToString());

            // Act 4: view-html — fetch で保存したノート件数分の <div class="note"> を含む HTML が生成されること
            var viewHtmlResult = await Program.Main(new[] { "view-html" });
            Assert.Equal(0, viewHtmlResult);
            Assert.True(File.Exists(Path.Combine("html_output", "notes.html")));
            var html = await File.ReadAllTextAsync(Path.Combine("html_output", "notes.html"));
            Assert.Contains("<!DOCTYPE html>", html);
            var noteDivCount = System.Text.RegularExpressions.Regex.Matches(html, "<div class=\"note\">").Count;
            Assert.Equal(fetchedNotes.Count, noteDivCount);
        }
        finally
        {
            if (File.Exists("notes.json"))
                File.Delete("notes.json");
            if (Directory.Exists("html_output"))
                Directory.Delete("html_output", recursive: true);
        }
    }

    // TDD: TST-32 - notes.json の有無による search/view の状態遷移テスト
    // (notes.json なし → search/view はエラー → notes.json 作成 → search/view は正常、の一連の状態変化を確認)
    // TDD: BUG-46 - No notes found は終了コード1のエラーのため stdout ではなく stderr に出力されるべき
    [Fact]
    [Trait("Category", "Unit")]
    public async Task StateTransition_NotesFileAbsentThenPresent_SearchAndViewBehaveAccordingly()
    {
        if (File.Exists("notes.json"))
            File.Delete("notes.json");

        var originalOutput = System.Console.Out;
        var originalError = System.Console.Error;

        try
        {
            // 状態1: notes.json が存在しない → search/view はエラー(exit 1)
            using (var searchWriter = new StringWriter())
            {
                System.Console.SetError(searchWriter);
                int searchResultBeforeFetch;
                try
                {
                    searchResultBeforeFetch = await Program.Main(new[] { "search", "keyword" });
                }
                finally
                {
                    System.Console.SetError(originalError);
                }
                Assert.Equal(1, searchResultBeforeFetch);
                Assert.Contains("No notes found. Run 'fetch' command first.", searchWriter.ToString());
            }

            using (var viewWriter = new StringWriter())
            {
                System.Console.SetError(viewWriter);
                int viewResultBeforeFetch;
                try
                {
                    viewResultBeforeFetch = await Program.Main(new[] { "view" });
                }
                finally
                {
                    System.Console.SetError(originalError);
                }
                Assert.Equal(1, viewResultBeforeFetch);
                Assert.Contains("No notes found. Run 'fetch' command first.", viewWriter.ToString());
            }

            // 状態遷移: notes.json を作成(fetch でノートが保存された状態に相当)
            var repository = new PastNotes.NoteRepository();
            var notes = new List<PastNotes.Note>
            {
                new PastNotes.Note { Id = "1", Text = "keyword note", CreatedAt = DateTime.Now }
            };
            await repository.SaveToFileAsync(notes, "notes.json");

            // 状態2: notes.json が存在する → search/view は正常(exit 0)
            using (var searchWriter = new StringWriter())
            {
                System.Console.SetOut(searchWriter);
                int searchResultAfterFetch;
                try
                {
                    searchResultAfterFetch = await Program.Main(new[] { "search", "keyword" });
                }
                finally
                {
                    System.Console.SetOut(originalOutput);
                }
                Assert.Equal(0, searchResultAfterFetch);
                Assert.Contains("Found 1", searchWriter.ToString());
            }

            using (var viewWriter = new StringWriter())
            {
                System.Console.SetOut(viewWriter);
                int viewResultAfterFetch;
                try
                {
                    viewResultAfterFetch = await Program.Main(new[] { "view" });
                }
                finally
                {
                    System.Console.SetOut(originalOutput);
                }
                Assert.Equal(0, viewResultAfterFetch);
                Assert.Contains("Total notes: 1", viewWriter.ToString());
            }
        }
        finally
        {
            if (File.Exists("notes.json"))
                File.Delete("notes.json");
        }
    }
}
