using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Tests.Commands;

public class SearchCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledWithValidKeyword_ReturnsSuccess()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new SearchCommand(repository, testFilePath);

        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Hello world", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Goodbye world", CreatedAt = DateTime.Now }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var result = await command.ExecuteAsync("world");

        // Assert
        Assert.Equal(0, result);

        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    // TDD: FEAT-3 - 日付フィルタリングが適用されること
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenDateRangeSpecified_ShowsOnlyNotesInRange()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var testNotes = new List<Note>
        {
            new Note { Id = "jan", Text = "January note", CreatedAt = new DateTime(2024, 1, 15, 1, 0, 0, DateTimeKind.Utc) },
            new Note { Id = "feb", Text = "February note", CreatedAt = new DateTime(2024, 2, 15, 1, 0, 0, DateTimeKind.Utc) }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // JST 2024-01-01〜2024-01-31 を指定
        var jstStart = new DateTime(2024, 1, 1);
        var jstEnd   = new DateTime(2024, 1, 31, 23, 59, 59);
        var command = new SearchCommand(repository, testFilePath, startDate: jstStart, endDate: jstEnd);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        int result;
        try
        {
            result = await command.ExecuteAsync("note");
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        // Assert
        Assert.Equal(0, result);
        var output = stringWriter.ToString();
        Assert.Contains("January note", output);
        Assert.DoesNotContain("February note", output);

        // Cleanup
        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    // TDD: TST-12 - notes.jsonなし vs. 0件ヒットの区別
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenNotesFileDoesNotExist_ReturnsOne()
    {
        // Arrange
        var repository = new NoteRepository();
        var nonExistentPath = $"non_existent_{Guid.NewGuid()}.json";
        var command = new SearchCommand(repository, nonExistentPath);

        // Act
        var result = await command.ExecuteAsync("keyword");

        // Assert: notes.jsonなし → exit 1
        Assert.Equal(1, result);
    }

    // TDD: BUG-20 - UTC を JST に変換して表示するか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledWithUtcDateTime_ConvertsToJst()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new SearchCommand(repository, testFilePath);

        // UTC 10:30:45 → JST 19:30:45
        var utcDateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note", CreatedAt = utcDateTime }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            await command.ExecuteAsync("Test");
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        var output = stringWriter.ToString();
        Assert.Contains("19:30:45", output);
        Assert.DoesNotContain("10:30:45", output);

        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    // TDD: BUG-20 - 秒数まで表示するか（HH:mm:ss）
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledWithExistingNotes_DisplaysDateTimeWithSeconds()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new SearchCommand(repository, testFilePath);

        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note", CreatedAt = DateTime.UtcNow }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            await command.ExecuteAsync("Test");
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        var output = stringWriter.ToString();
        Assert.Matches(@"\d{2}:\d{2}:\d{2}", output);

        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    // TDD: BUG-26 - "Found N" の件数と実際に出力されるノート行数が一致するか（二重列挙がないことの証明）
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_FoundCountMatchesActualOutputLines()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new SearchCommand(repository, testFilePath);

        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Hello world", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Note { Id = "2", Text = "Goodbye world", CreatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new Note { Id = "3", Text = "No match here", CreatedAt = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc) }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        try
        {
            await command.ExecuteAsync("world");
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        // Assert: "Found 2" が出力され、ノート行も2行あること
        var output = stringWriter.ToString();
        Assert.Contains("Found 2", output);
        Assert.Contains("Hello world", output);
        Assert.Contains("Goodbye world", output);
        Assert.DoesNotContain("No match here", output);

        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenNotesExistButNoMatch_ReturnsZero()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new SearchCommand(repository, testFilePath);

        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Hello world", CreatedAt = DateTime.Now }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        int result;
        try
        {
            result = await command.ExecuteAsync("xyznotfound");
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        // Assert: ヒット0件 → exit 0（ファイルなしとは異なる）
        Assert.Equal(0, result);
        Assert.Contains("Found 0", stringWriter.ToString());

        // Cleanup
        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    // TDD: TST-27 - 破損 JSON で InvalidDataException が伝播するか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCorruptedJson_ThrowsInvalidDataException()
    {
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        await File.WriteAllTextAsync(testFilePath, "{ not valid json }}");
        var command = new SearchCommand(repository, testFilePath);

        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => command.ExecuteAsync("keyword"));
        }
        finally
        {
            if (File.Exists(testFilePath)) File.Delete(testFilePath);
        }
    }
}
