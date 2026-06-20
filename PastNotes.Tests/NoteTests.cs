namespace PastNotes.Tests;

public class NoteTests
{
    [Fact]
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
    public void Note_WhenCreatedWithDefaultValues_HasEmptyStrings()
    {
        // Arrange
        var note = new Note();

        // Act & Assert
        Assert.Equal(string.Empty, note.Id);
        Assert.Equal(string.Empty, note.Text);
        Assert.Equal(default(DateTime), note.CreatedAt);
    }
}

public class NoteRepositoryTests
{
    [Fact]
    public void SaveToFileAsync_WhenCalledWithNotes_SavesNotesToFile()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        var filePath = "test-notes.json";
        var repository = new NoteRepository();

        // Act
        repository.SaveToFileAsync(notes, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        File.Delete(filePath);
    }

    [Fact]
    public void LoadFromFileAsync_WhenCalledWithValidFile_ReturnsNotes()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        var filePath = "test-notes-load.json";
        var repository = new NoteRepository();
        repository.SaveToFileAsync(notes, filePath);

        // Act
        var loadedNotes = repository.LoadFromFileAsync(filePath);

        // Assert
        Assert.NotNull(loadedNotes);
        Assert.Equal(2, loadedNotes.Count());
        File.Delete(filePath);
    }

    [Fact]
    public void LoadFromFileAsync_WhenCalledWithInvalidFile_ReturnsEmptyList()
    {
        // Arrange
        var filePath = "non-existent-file.json";
        var repository = new NoteRepository();

        // Act
        var loadedNotes = repository.LoadFromFileAsync(filePath);

        // Assert
        Assert.NotNull(loadedNotes);
        Assert.Empty(loadedNotes);
    }

    [Fact]
    public void SearchByKeyword_WhenCalledWithValidKeyword_ReturnsMatchingNotes()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Hello world", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Goodbye world", CreatedAt = DateTime.Now },
            new Note { Id = "3", Text = "Test message", CreatedAt = DateTime.Now }
        };
        var repository = new NoteRepository();

        // Act
        var results = repository.SearchByKeyword(notes, "world");

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, note => Assert.Contains("world", note.Text));
    }

    [Fact]
    public void SearchByKeyword_WhenCalledWithCaseInsensitive_ReturnsMatchingNotes()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Hello World", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "hello world", CreatedAt = DateTime.Now }
        };
        var repository = new NoteRepository();

        // Act
        var results = repository.SearchByKeyword(notes, "WORLD");

        // Assert
        Assert.Equal(2, results.Count());
    }

    [Fact]
    public void FilterByDateRange_WhenCalledWithValidRange_ReturnsNotesInRange()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Note 1", CreatedAt = new DateTime(2024, 1, 15) },
            new Note { Id = "2", Text = "Note 2", CreatedAt = new DateTime(2024, 1, 20) },
            new Note { Id = "3", Text = "Note 3", CreatedAt = new DateTime(2024, 2, 1) }
        };
        var repository = new NoteRepository();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var results = repository.FilterByDateRange(notes, startDate, endDate);

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, note => 
        {
            Assert.True(note.CreatedAt >= startDate);
            Assert.True(note.CreatedAt <= endDate);
        });
    }

    [Fact]
    public void FilterByDateRange_WhenCalledWithBoundaryDates_ReturnsCorrectNotes()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Note 1", CreatedAt = new DateTime(2024, 1, 1) },
            new Note { Id = "2", Text = "Note 2", CreatedAt = new DateTime(2024, 1, 15) },
            new Note { Id = "3", Text = "Note 3", CreatedAt = new DateTime(2024, 1, 31) }
        };
        var repository = new NoteRepository();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var results = repository.FilterByDateRange(notes, startDate, endDate);

        // Assert
        Assert.Equal(3, results.Count());
    }
}
