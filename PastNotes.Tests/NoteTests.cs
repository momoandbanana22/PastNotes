namespace PastNotes.Tests;

public class NoteTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Note_WhenCreated_HasRequiredProperties()
    {
        // Arrange
        var now = DateTime.Now;
        var note = new Note
        {
            Id = "test-id",
            Text = "test content",
            CreatedAt = now
        };

        // Act & Assert
        Assert.Equal("test-id", note.Id);
        Assert.Equal("test content", note.Text);
        Assert.Equal(now, note.CreatedAt);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Note_WhenCreatedWithDefaultValues_HasEmptyStrings()
    {
        // Arrange
        var note = new Note();

        // Act & Assert
        Assert.Equal(string.Empty, note.Id);
        Assert.Equal(string.Empty, note.Text);
        Assert.Equal(default(DateTime), note.CreatedAt);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Note_WhenCreatedWithFiles_HasFilesProperty()
    {
        // Arrange
        var note = new Note();

        // Act & Assert
        Assert.NotNull(note.Files);
        Assert.Empty(note.Files);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NoteFile_WhenCreated_HasRequiredProperties()
    {
        // Arrange
        var file = new NoteFile
        {
            Id = "file-id",
            Url = "https://example.com/image.jpg",
            Type = "image/jpeg",
            Name = "image.jpg"
        };

        // Act & Assert
        Assert.Equal("file-id", file.Id);
        Assert.Equal("https://example.com/image.jpg", file.Url);
        Assert.Equal("image/jpeg", file.Type);
        Assert.Equal("image.jpg", file.Name);
    }
}
