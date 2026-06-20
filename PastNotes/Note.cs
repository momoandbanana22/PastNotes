namespace PastNotes;

public class Note
{
    public DateTime CreatedAt { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<NoteFile> Files { get; set; } = new();
}

public class NoteFile
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
