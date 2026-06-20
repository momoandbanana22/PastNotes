using System.Text.Json;

namespace PastNotes;

public class NoteRepository
{
    public void SaveToFileAsync(IEnumerable<Note> notes, string filePath)
    {
        var json = JsonSerializer.Serialize(notes);
        File.WriteAllText(filePath, json);
    }

    public IEnumerable<Note> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Enumerable.Empty<Note>();
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<IEnumerable<Note>>(json) ?? Enumerable.Empty<Note>();
    }

    public IEnumerable<Note> SearchByKeyword(IEnumerable<Note> notes, string keyword)
    {
        return notes.Where(note => note.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Note> FilterByDateRange(IEnumerable<Note> notes, DateTime startDate, DateTime endDate)
    {
        return notes.Where(note => note.CreatedAt >= startDate && note.CreatedAt <= endDate);
    }
}
