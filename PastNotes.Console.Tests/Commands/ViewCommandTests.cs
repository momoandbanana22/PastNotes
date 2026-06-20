using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Tests.Commands;

public class ViewCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_WhenCalledWithExistingNotes_ReturnsSuccess()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_WhenCalledWithNoNotes_ReturnsFailure()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath);

        // Act
        var result = command.Execute();

        // Assert
        Assert.NotEqual(0, result);
    }
}
