using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Tests.Commands;

public class SearchCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_WhenCalledWithValidKeyword_ReturnsSuccess()
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
        repository.SaveToFileAsync(testNotes, testFilePath);

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
}
