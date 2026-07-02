using PastNotes;
using PastNotes.Console.Commands;
using System;

namespace PastNotes.Console.Tests.Commands;

public class ViewCommandTests
{
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
    public async Task ExecuteAsync_WhenCalledWithExistingNotes_DisplaysDateTimeWithSeconds()
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

        try
        {
            await command.ExecuteAsync();
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        var output = stringWriter.ToString();

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
    public async Task ExecuteAsync_WhenCalledWithExistingNotes_HidesIdByDefault()
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

        try
        {
            await command.ExecuteAsync();
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        var output = stringWriter.ToString();

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
    public async Task ExecuteAsync_WhenCalledWithShowIdOption_DisplaysId()
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

        try
        {
            await command.ExecuteAsync();
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        var output = stringWriter.ToString();

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
    public async Task ExecuteAsync_WhenCalledWithUtcDateTime_ConvertsToJst()
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

        try
        {
            await command.ExecuteAsync();
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        var output = stringWriter.ToString();

        // Assert
        Assert.Contains("19:30:45", output);
        Assert.DoesNotContain("10:30:45", output);

        // Cleanup
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    // TDD: BUG-29 - "Total notes: N" の件数と実際に出力されるノート行数が一致するか（二重列挙がないことの証明）
    // TDD: FEAT-3 - view に日付絞り込みオプション
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_TotalCountMatchesActualDisplayedNotes()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";

        var testNotes = new List<Note>
        {
            new Note { Id = "jan", Text = "January note",  CreatedAt = new DateTime(2024, 1, 15, 1, 0, 0, DateTimeKind.Utc) },
            new Note { Id = "feb", Text = "February note", CreatedAt = new DateTime(2024, 2, 15, 1, 0, 0, DateTimeKind.Utc) },
            new Note { Id = "mar", Text = "March note",    CreatedAt = new DateTime(2024, 3, 15, 1, 0, 0, DateTimeKind.Utc) }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        // JST 2024-01-01〜2024-01-31 を指定（1月のノートのみ表示されるべき）
        var jstStart = new DateTime(2024, 1, 1);
        var jstEnd   = new DateTime(2024, 1, 31, 23, 59, 59);
        var command = new ViewCommand(repository, testFilePath, startDate: jstStart, endDate: jstEnd);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        int result;
        try
        {
            result = await command.ExecuteAsync();
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        // Assert: "Total notes: 1" が出力され、1月のノートのみ表示されること
        Assert.Equal(0, result);
        var output = stringWriter.ToString();
        Assert.Contains("Total notes: 1", output);
        Assert.Contains("January note", output);
        Assert.DoesNotContain("February note", output);
        Assert.DoesNotContain("March note", output);

        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenNoteHasFiles_DisplaysFileInformation()
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

        try
        {
            await command.ExecuteAsync();
        }
        finally
        {
            System.Console.SetOut(originalOutput);
        }

        var output = stringWriter.ToString();

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

    // TDD: BUG-42 - startDate > endDate（逆指定）を検証するか（BUG-41の横展開）
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenStartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        var testNotes = new List<Note>
        {
            new Note { Id = "1", Text = "Test note 1", CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc) }
        };
        await repository.SaveToFileAsync(testNotes, testFilePath);

        var command = new ViewCommand(repository, testFilePath,
            startDate: new DateTime(2024, 2, 1), endDate: new DateTime(2024, 1, 1));

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => command.ExecuteAsync());
        }
        finally
        {
            if (File.Exists(testFilePath)) File.Delete(testFilePath);
        }
    }

    // TDD: TST-27 - 破損 JSON で InvalidDataException が伝播するか
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCorruptedJson_ThrowsInvalidDataException()
    {
        var repository = new NoteRepository();
        var testFilePath = $"test_notes_{Guid.NewGuid()}.json";
        await File.WriteAllTextAsync(testFilePath, "{ not valid json }}");
        var command = new ViewCommand(repository, testFilePath);

        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => command.ExecuteAsync());
        }
        finally
        {
            if (File.Exists(testFilePath)) File.Delete(testFilePath);
        }
    }
}
