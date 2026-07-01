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
        try
        {
            return JsonSerializer.Deserialize<IEnumerable<Note>>(json) ?? Enumerable.Empty<Note>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"'{filePath}' は有効な JSON ではありません。", ex);
        }
    }

    public IEnumerable<Note> SearchByKeyword(IEnumerable<Note> notes, string keyword)
    {
        return notes.Where(note => note.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Note> FilterByDateRange(IEnumerable<Note> notes, DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        return notes.Where(note => note.CreatedAt >= startDate && note.CreatedAt <= endDate);
    }
}
