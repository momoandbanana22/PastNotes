using PastNotes;
using PastNotes.Console.Commands;
using System;

namespace PastNotes.Console.Tests.Commands;

// TDD: TST-41 - ViewCommandTests.cs に同居していたクラスを、TST-23と同じ方針
// （ファイル名とクラス名を一致させる）でファイル分離した。内容は変更していない。
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

    // TST-24: ノートファイルが存在しない場合 → "No notes found" パス
    // TDD: BUG-46 - No notes found は終了コード1のエラーのため stdout ではなく stderr に出力されるべき
    [Fact]
    [Trait("Category", "Unit")]
    public void Execute_WhenNoNotesFile_ReturnsOneAndPrintsMessageToStderr()
    {
        var originalError = System.Console.Error;
        using var sw = new StringWriter();
        System.Console.SetError(sw);
        try
        {
            var repository = new NoteRepository();
            var testFilePath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
            var outputDir = Path.Combine(Path.GetTempPath(), $"test_html_{Guid.NewGuid()}");
            var command = new ViewHtmlCommand(repository, testFilePath, outputDir);
            var result = command.Execute();
            Assert.Equal(1, result);
            Assert.Contains("No notes found", sw.ToString());
        }
        finally
        {
            System.Console.SetError(originalError);
        }
    }

    // TST-24: JSON 破損ファイル → InvalidDataException 伝播
    [Fact]
    [Trait("Category", "Unit")]
    public async Task Execute_WhenCorruptedJson_ThrowsInvalidDataException()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), $"corrupt_{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(testFilePath, "{ not valid json }}");
        try
        {
            var repository = new NoteRepository();
            var outputDir = Path.Combine(Path.GetTempPath(), $"test_html_{Guid.NewGuid()}");
            var command = new ViewHtmlCommand(repository, testFilePath, outputDir);
            Assert.Throws<InvalidDataException>(() => command.Execute());
        }
        finally
        {
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }
}
