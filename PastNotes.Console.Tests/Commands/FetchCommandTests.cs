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

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledWithDateRange_ReturnsSuccess()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note", CreatedAt = DateTime.Now }
        };
        
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        
        mockApiClient.Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                   .ReturnsAsync(testNotes);

        // Act
        var result = await command.ExecuteAsync(startDate, endDate);

        // Assert
        Assert.Equal(0, result);
        // Verify that dates were converted from JST to UTC
        mockApiClient.Verify(x => x.GetNotesAsync(
            It.Is<DateTime>(d => d == startDate.AddHours(-9)), 
            It.Is<DateTime>(d => d == endDate.AddHours(-9).AddSeconds(1))), 
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledWithDateRange_ConvertsJstToUtcByDefault()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note", CreatedAt = DateTime.Now }
        };
        
        // Input dates are treated as JST (UTC+9)
        var jstStartDate = new DateTime(2024, 1, 1, 0, 0, 0);
        var jstEndDate = new DateTime(2024, 1, 31, 23, 59, 59);
        
        mockApiClient.Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                   .ReturnsAsync(testNotes);

        // Act
        var result = await command.ExecuteAsync(jstStartDate, jstEndDate);

        // Assert
        Assert.Equal(0, result);
        // Verify that the dates were converted from JST to UTC by default
        mockApiClient.Verify(x => x.GetNotesAsync(
            It.Is<DateTime>(d => d == jstStartDate.AddHours(-9)), 
            It.Is<DateTime>(d => d == jstEndDate.AddHours(-9).AddSeconds(1))), 
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenStartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);
        
        var startDate = new DateTime(2024, 1, 31);
        var endDate = new DateTime(2024, 1, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => command.ExecuteAsync(startDate, endDate));
    }
}
