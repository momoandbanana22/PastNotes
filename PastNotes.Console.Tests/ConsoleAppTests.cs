namespace PastNotes.Console.Tests;

public class ConsoleAppTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void FetchCommand_WhenCalledWithInvalidParameters_ReturnsFailure()
    {
        // Arrange
        var args = new[] { "fetch" };

        // Act
        var result = Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FetchCommand_WhenCalledWithInvalidDays_ReturnsFailure()
    {
        // Arrange
        var args = new[] { "fetch", "--days", "invalid" };

        // Act
        var result = Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }
}
