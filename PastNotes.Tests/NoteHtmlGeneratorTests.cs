namespace PastNotes;

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
