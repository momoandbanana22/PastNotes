using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Tests.Commands;

public class SearchCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCalledWithValidKeyword_ReturnsSuccess()
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
        var result = command.Execute("world");

        // Assert
        Assert.Equal(0, result);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    // TDD: 非同期バージョンのテスト
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

    // TDD: Execute（同期版）にも日付フィルタリングが適用されること
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenDateRangeSpecified_ShowsOnlyNotesInRange()
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

        // Act
        var result = command.Execute("note");

        System.Console.SetOut(originalOutput);

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

        // Act
        var result = await command.ExecuteAsync("xyznotfound");

        System.Console.SetOut(originalOutput);

        // Assert: ヒット0件 → exit 0（ファイルなしとは異なる）
        Assert.Equal(0, result);
        Assert.Contains("Found 0", stringWriter.ToString());

        // Cleanup
        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }
}
