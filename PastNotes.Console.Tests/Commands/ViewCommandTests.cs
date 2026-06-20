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
}
