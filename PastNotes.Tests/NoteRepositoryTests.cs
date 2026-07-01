namespace PastNotes.Tests;

public class NoteRepositoryTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveToFileAsync_WhenCalledWithNotes_SavesNotesToFile()
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
        await repository.SaveToFileAsync(notes, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        File.Delete(filePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoadFromFileAsync_WhenCalledWithValidFile_ReturnsNotes()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        var filePath = "test-notes-load.json";
        var repository = new NoteRepository();
        await repository.SaveToFileAsync(notes, filePath);

        // Act
        var loadedNotes = await repository.LoadFromFileAsync(filePath);

        // Assert
        Assert.NotNull(loadedNotes);
        Assert.Equal(2, loadedNotes.Count());
        File.Delete(filePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoadFromFileAsync_WhenCalledWithInvalidFile_ReturnsEmptyList()
    {
        // Arrange
        var filePath = "non-existent-file.json";
        var repository = new NoteRepository();

        // Act
        var loadedNotes = await repository.LoadFromFileAsync(filePath);

        // Assert
        Assert.NotNull(loadedNotes);
        Assert.Empty(loadedNotes);
    }

    [Fact]
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
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

    // TDD: TST-25 - startDate == endDate（同一日時の1点フィルタ）の境界値テスト
    [Fact]
    [Trait("Category", "Unit")]
    public void FilterByDateRange_WhenStartDateEqualsEndDate_ReturnsOnlyExactMatchingNote()
    {
        // Arrange
        var targetDate = new DateTime(2024, 1, 15, 12, 0, 0);
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "1秒前", CreatedAt = targetDate.AddSeconds(-1) },
            new Note { Id = "2", Text = "ちょうど", CreatedAt = targetDate },
            new Note { Id = "3", Text = "1秒後", CreatedAt = targetDate.AddSeconds(1) }
        };
        var repository = new NoteRepository();

        // Act
        var results = repository.FilterByDateRange(notes, targetDate, targetDate).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("2", results[0].Id);
    }

    // TDD: 非同期バージョンのテスト
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveToFileAsync_WhenCalledWithNotes_SavesNotesToFileAsync()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        var filePath = "test-notes-async.json";
        var repository = new NoteRepository();

        // Act
        await repository.SaveToFileAsync(notes, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        File.Delete(filePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoadFromFileAsync_WhenCalledWithValidFile_ReturnsNotesAsync()
    {
        // Arrange
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        var filePath = "test-notes-load-async.json";
        var repository = new NoteRepository();
        await repository.SaveToFileAsync(notes, filePath);

        // Act
        var loadedNotes = await repository.LoadFromFileAsync(filePath);

        // Assert
        Assert.NotNull(loadedNotes);
        Assert.Equal(2, loadedNotes.Count());
        File.Delete(filePath);
    }

    // TDD: TST-4 - DateTimeKindがsave/loadで保持されるか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAndLoad_WhenCreatedAtIsUtc_PreservesDateTimeKind()
    {
        // Arrange
        var utcTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Test", CreatedAt = utcTime }
        };
        var filePath = $"test_datetimekind_{Guid.NewGuid()}.json";
        var repository = new NoteRepository();

        // Act
        await repository.SaveToFileAsync(notes, filePath);
        var loaded = (await repository.LoadFromFileAsync(filePath)).ToList();

        // Assert
        Assert.Single(loaded);
        Assert.Equal(DateTimeKind.Utc, loaded[0].CreatedAt.Kind);
        Assert.Equal(utcTime, loaded[0].CreatedAt);

        // Cleanup
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoadFromFileAsync_WhenCalledWithInvalidFile_ReturnsEmptyListAsync()
    {
        // Arrange
        var filePath = "non-existent-file-async.json";
        var repository = new NoteRepository();

        // Act
        var loadedNotes = await repository.LoadFromFileAsync(filePath);

        // Assert
        Assert.NotNull(loadedNotes);
        Assert.Empty(loadedNotes);
    }

    // TDD: TST-6 - 壊れたJSONファイルの読み込み
    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoadFromFileAsync_WhenJsonIsCorrupted_ThrowsInvalidDataException()
    {
        // Arrange
        var filePath = $"corrupted_{Guid.NewGuid()}.json";
        await File.WriteAllTextAsync(filePath, "{ this is not valid json [[[");
        var repository = new NoteRepository();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidDataException>(
            () => repository.LoadFromFileAsync(filePath));
        Assert.Contains(filePath, ex.Message);

        // Cleanup
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
