namespace PastNotes.Tests;

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
