namespace PastNotes;

public static class TimeZoneHelper
{
    public static readonly TimeZoneInfo Jst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

    public static DateTime ConvertToUtc(DateTime jstTime) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(jstTime, DateTimeKind.Unspecified), Jst);
}
