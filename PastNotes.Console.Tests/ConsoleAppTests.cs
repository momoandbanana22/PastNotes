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
            throw new Exception("統合テストを実行するには環境変数を設定してください。");
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
    public async Task FetchCommand_WhenCalledWithDateRange_ReturnsSuccess()
    {
        // Arrange
        var args = new[] { "fetch", "--start", "2024-01-01", "--end", "2024-01-31" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.Equal(0, result);
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

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FetchCommand_WhenCalledWithJstFlag_ReturnsSuccess()
    {
        // Arrange
        var args = new[] { "fetch", "--start", "2024-01-01", "--end", "2024-01-31", "--jst" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.Equal(0, result);
    }
}
