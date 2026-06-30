namespace PastNotes;

public class TimeZoneHelperTests
{
    // BUG-13: Windows専用タイムゾーンIDの代わりにOS判定で取得したゾーンがUTC+9であることを確認
    [Fact]
    [Trait("Category", "Unit")]
    public void Jst_ShouldBeUtcPlus9()
    {
        Assert.Equal(TimeSpan.FromHours(9), TimeZoneHelper.Jst.BaseUtcOffset);
    }

    // TDD: BUG-27 - ConvertToUtc が正しく JST→UTC 変換するか
    [Fact]
    [Trait("Category", "Unit")]
    public void ConvertToUtc_WhenJstNewYearMidnight_ReturnsPreviousDayUtc()
    {
        // JST 2024-01-01 00:00:00 → UTC 2023-12-31 15:00:00
        var jst = new DateTime(2024, 1, 1, 0, 0, 0);
        var utc = TimeZoneHelper.ConvertToUtc(jst);
        Assert.Equal(new DateTime(2023, 12, 31, 15, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ConvertToUtc_WhenJstNoon_ReturnsUtcMorning()
    {
        // JST 2024-06-15 12:00:00 → UTC 2024-06-15 03:00:00
        var jst = new DateTime(2024, 6, 15, 12, 0, 0);
        var utc = TimeZoneHelper.ConvertToUtc(jst);
        Assert.Equal(new DateTime(2024, 6, 15, 3, 0, 0, DateTimeKind.Utc), utc);
    }
}

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
