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
            It.Is<DateTime>(d => d == endDate.AddHours(-9))),
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
            It.Is<DateTime>(d => d == jstEndDate.AddHours(-9))),
            Times.Once);
    }

    // TDD: TST-5 / BUG-10 - endDateちょうどのノートが含まれることを検証
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WithDateRange_ShouldNotAddExtraSecondToEndDate()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);

        var testNotes = new List<Note> { new Note { Id = "1", Text = "Test", CreatedAt = DateTime.UtcNow } };
        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(testNotes);

        var jstStartDate = new DateTime(2024, 1, 1, 0, 0, 0);
        var jstEndDate = new DateTime(2024, 1, 31, 23, 59, 59);

        // Act
        await command.ExecuteAsync(jstStartDate, jstEndDate);

        // Assert: endDateはJST→UTC変換のみ（+1秒不要・削除済みAPIパラメータの残骸）
        mockApiClient.Verify(x => x.GetNotesAsync(
            It.Is<DateTime>(d => d == jstStartDate.AddHours(-9)),
            It.Is<DateTime>(d => d == jstEndDate.AddHours(-9))),
            Times.Once);
    }

    // TDD: TST-2 / BUG-9 - --daysパスもUTCを渡すことを検証
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WithDays_ShouldPassUtcTimesToApi()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);

        DateTime? capturedStartDate = null;
        DateTime? capturedEndDate = null;

        var testNotes = new List<Note> { new Note { Id = "1", Text = "Test", CreatedAt = DateTime.UtcNow } };
        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<DateTime, DateTime>((s, e) => { capturedStartDate = s; capturedEndDate = e; })
            .ReturnsAsync(testNotes);

        var beforeUtc = DateTime.UtcNow;

        // Act
        await command.ExecuteAsync(30);

        var afterUtc = DateTime.UtcNow;

        // Assert: APIにはUTC時刻が渡されるべき（DateTime.Nowはタイムゾーン依存で最大±14h ずれる）
        Assert.NotNull(capturedEndDate);
        Assert.NotNull(capturedStartDate);
        Assert.True(
            capturedEndDate >= beforeUtc.AddMinutes(-1) && capturedEndDate <= afterUtc.AddMinutes(1),
            $"endDateはUTC nowであるべきだが {capturedEndDate} が渡された（UTC: {beforeUtc}〜{afterUtc}）");
        Assert.True(
            capturedStartDate >= beforeUtc.AddDays(-30).AddMinutes(-1) && capturedStartDate <= afterUtc.AddDays(-30).AddMinutes(1),
            $"startDateはUTC 30日前であるべきだが {capturedStartDate} が渡された");
    }

    // TDD: TST-3 - 0件の場合のFetchCommand動作
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WithDateRange_WhenNoNotesFound_ReturnsZeroAndPrintsMessage()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);

        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Note>());

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        // Act
        var result = await command.ExecuteAsync(startDate, endDate);

        System.Console.SetOut(originalOutput);

        // Assert
        Assert.Equal(0, result);
        Assert.Contains("No notes found.", stringWriter.ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WithDays_WhenNoNotesFound_ReturnsZeroAndPrintsMessage()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);

        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Note>());

        var originalOutput = System.Console.Out;
        using var stringWriter = new StringWriter();
        System.Console.SetOut(stringWriter);

        // Act
        var result = await command.ExecuteAsync(30);

        System.Console.SetOut(originalOutput);

        // Assert
        Assert.Equal(0, result);
        Assert.Contains("No notes found.", stringWriter.ToString());
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

    // TDD: FEAT-2 - --appendモードで既存ノートにマージ
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenAppendMode_MergesWithExistingNotes()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var testFilePath = $"test_append_{Guid.NewGuid()}.json";

        // 既存ファイルを作成（note A）
        var existingNotes = new List<Note>
        {
            new Note { Id = "existing-1", Text = "Existing note", CreatedAt = DateTime.UtcNow.AddDays(-5) }
        };
        await repository.SaveToFileAsync(existingNotes, testFilePath);

        // 新規取得ノート（note B）
        var newNotes = new List<Note>
        {
            new Note { Id = "new-1", Text = "New note", CreatedAt = DateTime.UtcNow }
        };
        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(newNotes);

        var command = new FetchCommand(mockApiClient.Object, repository, testFilePath, append: true);

        // Act
        await command.ExecuteAsync(30);

        // Assert: 既存1件 + 新規1件 = 2件
        var loaded = (await repository.LoadFromFileAsync(testFilePath)).ToList();
        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, n => n.Id == "existing-1");
        Assert.Contains(loaded, n => n.Id == "new-1");

        // Cleanup
        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenAppendMode_DeduplicatesByNoteId()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var testFilePath = $"test_dedup_{Guid.NewGuid()}.json";

        // 既存ファイル（重複IDを含む）
        var existingNotes = new List<Note>
        {
            new Note { Id = "dup-1", Text = "Old version", CreatedAt = DateTime.UtcNow.AddDays(-5) }
        };
        await repository.SaveToFileAsync(existingNotes, testFilePath);

        // 新規取得に同じIDが含まれる
        var newNotes = new List<Note>
        {
            new Note { Id = "dup-1", Text = "New version", CreatedAt = DateTime.UtcNow },
            new Note { Id = "new-2", Text = "Another note", CreatedAt = DateTime.UtcNow }
        };
        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(newNotes);

        var command = new FetchCommand(mockApiClient.Object, repository, testFilePath, append: true);

        // Act
        await command.ExecuteAsync(30);

        // Assert: 重複IDは新しい方のみ残る → 2件
        var loaded = (await repository.LoadFromFileAsync(testFilePath)).ToList();
        Assert.Equal(2, loaded.Count);
        Assert.DoesNotContain(loaded, n => n.Text == "Old version");

        // Cleanup
        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    // TDD: TST-13 - fetch 2回実行で notes.json が上書きされること
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenCalledTwice_OverwritesExistingNotesFile()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var testFilePath = $"test_overwrite_{Guid.NewGuid()}.json";
        var command = new FetchCommand(mockApiClient.Object, repository, testFilePath);

        var firstNotes = new List<Note>
        {
            new Note { Id = "first-1", Text = "First fetch note", CreatedAt = DateTime.UtcNow }
        };
        var secondNotes = new List<Note>
        {
            new Note { Id = "second-1", Text = "Second fetch note", CreatedAt = DateTime.UtcNow }
        };

        mockApiClient
            .SetupSequence(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(firstNotes)
            .ReturnsAsync(secondNotes);

        // Act
        await command.ExecuteAsync(30);
        await command.ExecuteAsync(30);

        // Assert: 2回目のfetchで上書きされ、1回目のノートは消えている
        var loaded = (await repository.LoadFromFileAsync(testFilePath)).ToList();
        Assert.Single(loaded);
        Assert.Equal("second-1", loaded[0].Id);

        // Cleanup
        if (File.Exists(testFilePath)) File.Delete(testFilePath);
    }

    // TDD: TST-8 - JST日付変更またぎの変換確認
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenJstNewYear_PassesCorrectUtcToPreviousDay()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);

        DateTime? capturedStart = null;
        DateTime? capturedEnd = null;

        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<DateTime, DateTime>((s, e) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync(new List<Note>());

        // JST 2024-01-01 00:00:00 = UTC 2023-12-31 15:00:00（日付またぎ）
        var jstStart = new DateTime(2024, 1, 1, 0, 0, 0);
        var jstEnd   = new DateTime(2024, 1, 31, 23, 59, 59);

        // Act
        await command.ExecuteAsync(jstStart, jstEnd);

        // Assert: JST→UTC で前年/前月/前日になることを明示的に検証
        Assert.NotNull(capturedStart);
        Assert.Equal(new DateTime(2023, 12, 31, 15, 0, 0), capturedStart);
        Assert.Equal(new DateTime(2024, 1, 31, 14, 59, 59), capturedEnd);
    }

    // TDD: TST-7 - 401 Unauthorized でexit 1とエラーメッセージ
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenApiReturns401_ReturnsOneAndPrintsError()
    {
        // Arrange
        var mockApiClient = new Mock<IMisskeyApiClient>();
        var repository = new NoteRepository();
        var command = new FetchCommand(mockApiClient.Object, repository);

        mockApiClient
            .Setup(x => x.GetNotesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new UnauthorizedException("Unauthorized access"));

        var originalError = System.Console.Error;
        using var stringWriter = new StringWriter();
        System.Console.SetError(stringWriter);

        // Act
        var result = await command.ExecuteAsync(30);

        System.Console.SetError(originalError);

        // Assert
        Assert.Equal(1, result);
        var output = stringWriter.ToString();
        Assert.Contains("Unauthorized", output, StringComparison.OrdinalIgnoreCase);
    }
}
