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
