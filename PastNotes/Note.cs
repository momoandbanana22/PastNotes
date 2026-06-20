namespace PastNotes;

public class Note
{
    public DateTime CreatedAt { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
