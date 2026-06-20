using System.Text.Json;

namespace PastNotes;

public class NoteRepository
{
    public async Task SaveToFileAsync(IEnumerable<Note> notes, string filePath)
    {
        var json = JsonSerializer.Serialize(notes);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<IEnumerable<Note>> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Enumerable.Empty<Note>();
        }

        var json = await File.ReadAllTextAsync(filePath);
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
