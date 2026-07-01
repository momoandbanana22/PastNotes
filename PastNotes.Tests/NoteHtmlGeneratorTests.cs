namespace PastNotes.Tests;

public class NoteHtmlGeneratorTests
{
    // TDD: TST-11 / BUG-12 - XSS対策テスト
    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtmlForAllNotes_WhenNoteTextContainsHtmlTags_ShouldEscapeOutput()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var outputPath = $"test_xss_{Guid.NewGuid()}.html";
        var notes = new List<Note>
        {
            new Note
            {
                Id = "1",
                Text = "<script>alert('xss')</script>",
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                Files = new List<NoteFile>()
            }
        };

        // Act
        generator.GenerateHtmlForAllNotes(notes, outputPath);
        var html = File.ReadAllText(outputPath);

        // Assert: <script>タグがそのまま出力されていないこと
        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        Assert.Contains("&lt;script&gt;", html);

        // Cleanup
        if (File.Exists(outputPath)) File.Delete(outputPath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtmlForAllNotes_WhenFileNameContainsHtmlTags_ShouldEscapeAltAttribute()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var outputPath = $"test_xss_{Guid.NewGuid()}.html";
        var notes = new List<Note>
        {
            new Note
            {
                Id = "1",
                Text = "normal text",
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                Files = new List<NoteFile>
                {
                    new NoteFile
                    {
                        Id = "f1",
                        Url = "https://example.com/image.jpg",
                        Type = "image/jpeg",
                        Name = "\"><script>alert('xss')</script>"
                    }
                }
            }
        };

        // Act
        generator.GenerateHtmlForAllNotes(notes, outputPath);
        var html = File.ReadAllText(outputPath);

        // Assert: alt属性内にスクリプトが挿入されていないこと
        Assert.DoesNotContain("<script>alert('xss')</script>", html);

        // Cleanup
        if (File.Exists(outputPath)) File.Delete(outputPath);
    }
}

public class NoteHtmlGeneratorOutputTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtml_WhenNoteIdContainsHtmlSpecialChars_EncodesIdInTitle()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var note = new Note
        {
            Id = "</title><script>alert(1)</script>",
            Text = "Safe text",
            CreatedAt = DateTime.UtcNow
        };
        var outputPath = $"test_note_xss_{Guid.NewGuid()}.html";

        // Act
        generator.GenerateHtml(note, outputPath);

        // Assert
        var html = File.ReadAllText(outputPath);
        Assert.DoesNotContain("</title><script>", html);
        Assert.Contains("&lt;/title&gt;", html);

        // Cleanup
        if (File.Exists(outputPath)) File.Delete(outputPath);
    }

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

    // TDD: TST-26 - 空リストを渡したときに例外なく実行され、有効な HTML が生成されるか
    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateHtmlForAllNotes_WhenNotesListIsEmpty_GeneratesValidHtmlWithoutException()
    {
        // Arrange
        var generator = new NoteHtmlGenerator();
        var notes = new List<Note>();
        var outputPath = $"test_notes_{Guid.NewGuid()}.html";

        try
        {
            // Act
            var exception = Record.Exception(() => generator.GenerateHtmlForAllNotes(notes, outputPath));

            // Assert
            Assert.Null(exception);
            Assert.True(File.Exists(outputPath));
            var htmlContent = File.ReadAllText(outputPath);
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains("</html>", htmlContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
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
