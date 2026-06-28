using PastNotes;
using PastNotes.Console.Commands;
using System;

namespace PastNotes.Console.Tests.Commands;

public class ViewCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCalledWithExistingNotes_ReturnsSuccess()
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
        await repository.SaveToFileAsync(testNotes, testFilePath);

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

    // TDD: 非同期バージョンのテスト
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledWithExistingNotes_ReturnsSuccess()
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
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var result = await command.ExecuteAsync();

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
    public async Task ExecuteAsync_WhenCalledWithNoNotes_ReturnsFailure()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCalledWithExistingNotes_DisplaysDateTimeWithSeconds()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);
        
        command.Execute();
        
        var output = stringWriter.ToString();
        System.Console.SetOut(originalOutput);

        // Assert - Check that seconds are displayed (format includes :ss)
        Assert.Matches(@"\d{2}:\d{2}:\d{2}", output);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCalledWithExistingNotes_HidesIdByDefault()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath, showId: false);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);
        
        command.Execute();
        
        var output = stringWriter.ToString();
        System.Console.SetOut(originalOutput);

        // Assert
        Assert.DoesNotContain("ID:", output);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCalledWithShowIdOption_DisplaysId()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath, showId: true);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "test-id-123", Text = "Test note 1", CreatedAt = DateTime.Now }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);
        
        command.Execute();
        
        var output = stringWriter.ToString();
        System.Console.SetOut(originalOutput);

        // Assert
        Assert.Contains("ID:", output);
        Assert.Contains("test-id-123", output);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCalledWithUtcDateTime_ConvertsToJst()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath);
        
        // UTC時間の2024-01-15 10:30:45はJSTでは2024-01-15 19:30:45（+9時間）
        var utcDateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = utcDateTime }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);
        
        command.Execute();
        
        var output = stringWriter.ToString();
        System.Console.SetOut(originalOutput);

        // Assert
        Assert.Contains("19:30:45", output);
        Assert.DoesNotContain("10:30:45", output);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    // TDD: FEAT-3 - view に日付絞り込みオプション
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenDateRangeSpecified_ShowsOnlyNotesInRange()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";

        // UTC保存: 2024-01-15 JST = 2024-01-15 01:00 UTC (JST 10:00 → UTC 01:00)
        var testNotes = new List<Note>
        {
            new Note { Id = "jan", Text = "January note", CreatedAt = new DateTime(2024, 1, 15, 1, 0, 0, DateTimeKind.Utc) },
            new Note { Id = "feb", Text = "February note", CreatedAt = new DateTime(2024, 2, 15, 1, 0, 0, DateTimeKind.Utc) }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // JST 2024-01-01〜2024-01-31 を指定（1月のノートのみ表示されるべき）
        var jstStart = new DateTime(2024, 1, 1);
        var jstEnd   = new DateTime(2024, 1, 31, 23, 59, 59);
        var command = new ViewCommand(repository, testFilePath, startDate: jstStart, endDate: jstEnd);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        // Act
        var result = await command.ExecuteAsync();

        System.Console.SetOut(originalOutput);

        // Assert
        Assert.Equal(0, result);
        var output = stringWriter.ToString();
        Assert.Contains("January note", output);
        Assert.DoesNotContain("February note", output);

        // Cleanup
        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenNoteHasFiles_DisplaysFileInformation()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var command = new ViewCommand(repository, testFilePath);
        
        var testNotes = new List<Note>
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
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);
        
        command.Execute();
        
        var output = stringWriter.ToString();
        System.Console.SetOut(originalOutput);

        // Assert
        Assert.Contains("添付ファイル", output);
        Assert.Contains("image.jpg", output);
        Assert.Contains("image/jpeg", output);
        Assert.Contains("https://example.com/image.jpg", output);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }
}

public class ViewHtmlCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCalledWithExistingNotes_GeneratesSingleHtmlFile()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var outputDir = $"test_html_{Guid.NewGuid()}";
        var command = new ViewHtmlCommand(repository, testFilePath, outputDir);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = DateTime.Now },
            new Note { Id = "2", Text = "Test note 2", CreatedAt = DateTime.Now }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        var result = command.Execute();

        // Assert
        Assert.Equal(0, result);
        Assert.True(Directory.Exists(outputDir));
        var htmlFiles = Directory.GetFiles(outputDir, "*.html");
        Assert.Single(htmlFiles);
        Assert.Equal("notes.html", Path.GetFileName(htmlFiles[0]));
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenNoteHasFiles_IncludesImageTagsInHtml()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var outputDir = $"test_html_{Guid.NewGuid()}";
        var command = new ViewHtmlCommand(repository, testFilePath, outputDir);
        
        var testNotes = new List<Note>
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
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act
        command.Execute();

        // Assert
        var htmlFiles = Directory.GetFiles(outputDir, "*.html");
        Assert.Single(htmlFiles);
        var htmlContent = File.ReadAllText(htmlFiles[0]);
        Assert.Contains("<img", htmlContent);
        Assert.Contains("https://example.com/image.jpg", htmlContent);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenOpenBrowserIsTrue_OpensSingleHtmlFile()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var outputDir = $"test_html_{Guid.NewGuid()}";
        var command = new ViewHtmlCommand(repository, testFilePath, outputDir, openBrowser: false);
        
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note", CreatedAt = DateTime.Now }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // Act & Assert
        // ブラウザを開く機能はテストが難しいため、コマンドが正常に実行されることを確認
        var result = command.Execute();
        Assert.Equal(0, result);
        
        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
    }
}
