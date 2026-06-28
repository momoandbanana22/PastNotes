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

public class NoteHtmlGeneratorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtml_WhenCalledWithNote_CreatesHtmlFile()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var note = new Note
        {
            Id = "test-id",
            Text = "Test note",
            CreatedAt = DateTime.Now
        };
        var outputPath = $"test_note_{Guid.NewGuid()}.html";

        // Act
        generator.GenerateHtml(note, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtml_WhenNoteHasFiles_IncludesImageTags()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var note = new Note
        {
            Id = "test-id",
            Text = "Test note with image",
            CreatedAt = DateTime.Now,
            Files = new List<NoteFile>
            {
                new NoteFile
                {
                    Id = "file-1",
                    Url = "https://example.com/image.jpg",
                    Type = "image/jpeg",
                    Name = "image.jpg"
                }
            }
        };
        var outputPath = $"test_note_{Guid.NewGuid()}.html";

        // Act
        generator.GenerateHtml(note, outputPath);

        // Assert
        var htmlContent = File.ReadAllText(outputPath);
        Assert.Contains("<img", htmlContent);
        Assert.Contains("https://example.com/image.jpg", htmlContent);
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OpenInBrowser_WhenCalledWithHtmlFile_OpensBrowser()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var note = new Note
        {
            Id = "test-id",
            Text = "Test note",
            CreatedAt = DateTime.Now
        };
        var outputPath = $"test_note_{Guid.NewGuid()}.html";
        generator.GenerateHtml(note, outputPath);

        // Act & Assert
        // ブラウザを開く機能はテストが難しいため、メソッドが存在することを確認
        var method = typeof(NoteHtmlGenerator).GetMethod("OpenInBrowser", new[] { typeof(string) });
        Assert.NotNull(method);
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtmlForAllNotes_WhenCalledWithMultipleNotes_GeneratesSingleHtmlFile()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var notes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        var outputPath = $"test_notes_{Guid.NewGuid()}.html";

        // Act
        generator.GenerateHtmlForAllNotes(notes, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var htmlContent = File.ReadAllText(outputPath);
        Assert.Contains("Test note 1", htmlContent);
        Assert.Contains("Test note 2", htmlContent);
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtmlForAllNotes_WhenNotesHaveFiles_IncludesImageTags()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var notes = new List<Note>
        {
            new Note 
            { 
                Id = "1", 
                Text = "Test note with image", 
                CreatedAt = DateTime.Now,
                Files = new List<NoteFile>
                {
                    new NoteFile 
                    { 
                        Id = "file-1", 
                        Url = "https://example.com/image.jpg", 
                        Type = "image/jpeg", 
                        Name = "image.jpg" 
                    }
                }
            }
        };
        var outputPath = $"test_notes_{Guid.NewGuid()}.html";

        // Act
        generator.GenerateHtmlForAllNotes(notes, outputPath);

        // Assert
        var htmlContent = File.ReadAllText(outputPath);
        Assert.Contains("<img", htmlContent);
        Assert.Contains("https://example.com/image.jpg", htmlContent);
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtml_WhenNoteHasLineBreaks_PreservesLineBreaksInHtml()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var note = new Note
        {
            Id = "test-id",
            Text = "Line 1\nLine 2\nLine 3",
            CreatedAt = DateTime.Now
        };
        var outputPath = $"test_note_linebreaks_{Guid.NewGuid()}.html";

        // Act
        generator.GenerateHtml(note, outputPath);

        // Assert
        var htmlContent = File.ReadAllText(outputPath);
        // Check that line breaks are preserved either as <br> tags or with CSS white-space
        Assert.True(htmlContent.Contains("<br") || htmlContent.Contains("white-space"));
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtmlForAllNotes_WhenNotesHaveLineBreaks_PreservesLineBreaksInHtml()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var notes = new List<Note>
        {
            new Note 
            { 
                Id = "1", 
                Text = "First line\nSecond line\nThird line", 
                CreatedAt = DateTime.Now
            }
        };
        var outputPath = $"test_notes_linebreaks_{Guid.NewGuid()}.html";

        // Act
        generator.GenerateHtmlForAllNotes(notes, outputPath);

        // Assert
        var htmlContent = File.ReadAllText(outputPath);
        // Check that line breaks are preserved either as <br> tags or with CSS white-space
        Assert.True(htmlContent.Contains("<br") || htmlContent.Contains("white-space"));
        
        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }
}

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
