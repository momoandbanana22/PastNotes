using Moq;
using PastNotes;
using PastNotes.Console.Commands;

namespace PastNotes.Console.Tests.Commands;

public class FetchCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledWithValidDays_ReturnsSuccess()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note", CreatedAt = DateTime.Now }
        };
        
        mockApiClient.Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                   .ReturnsAsync(testNotes);

        // Act
        var result = await command.ExecuteAsync(30);

        // Assert
        Assert.Equal(0, result);
    }
}
