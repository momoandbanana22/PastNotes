namespace PastNotes;

public class NoteHtmlGenerator
{
    public void GenerateHtml(Note note, string outputPath)
    {
        var jstTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(note.CreatedAt, DateTimeKind.Utc), 
            TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
        
        var html = $@"<!DOCTYPE html>
<html lang=""ja"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Note - {note.Id}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            line-height: 1.6;
        }}
        .note-text {{
            background-color: #f5f5f5;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
        }}
        .note-date {{
            color: #666;
            font-size: 0.9em;
            margin-bottom: 10px;
        }}
        .images {{
            margin-top: 20px;
        }}
        .images img {{
            max-width: 100%;
            height: auto;
            margin: 10px 0;
            border-radius: 5px;
        }}
    </style>
</head>
<body>
    <div class=""note-date"">{jstTime:yyyy-MM-dd HH:mm:ss}</div>
    <div class=""note-text"">{note.Text}</div>";
        
        if (note.Files.Any())
        {
            html += @"    <div class=""images"">";
            foreach (var file in note.Files)
            {
                html += $@"        <img src=""{file.Url}"" alt=""{file.Name}"">";
            }
            html += @"    </div>";
        }
        
        html += @"
</body>
</html>";
        
        File.WriteAllText(outputPath, html);
    }

    public void GenerateHtmlForAllNotes(IEnumerable<Note> notes, string outputPath)
    {
        var html = @"<!DOCTYPE html>
<html lang=""ja"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Notes</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            line-height: 1.6;
        }
        .note {
            background-color: #f5f5f5;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
        }
        .note-date {
            color: #666;
            font-size: 0.9em;
            margin-bottom: 10px;
        }
        .note-text {
            margin-bottom: 10px;
        }
        .images {
            margin-top: 20px;
        }
        .images img {
            max-width: 100%;
            height: auto;
            margin: 10px 0;
            border-radius: 5px;
        }
    </style>
</head>
<body>
    <h1>Notes</h1>";
        
        foreach (var note in notes)
        {
            var jstTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(note.CreatedAt, DateTimeKind.Utc), 
                TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
            
            html += $@"
    <div class=""note"">
        <div class=""note-date"">{jstTime:yyyy-MM-dd HH:mm:ss}</div>
        <div class=""note-text"">{note.Text}</div>";
            
            if (note.Files.Any())
            {
                html += @"        <div class=""images"">";
                foreach (var file in note.Files)
                {
                    html += $@"            <img src=""{file.Url}"" alt=""{file.Name}"">";
                }
                html += @"        </div>";
            }
            
            html += @"    </div>";
        }
        
        html += @"
</body>
</html>";
        
        File.WriteAllText(outputPath, html);
    }

    public void OpenInBrowser(string htmlFilePath)
    {
        var fullPath = Path.GetFullPath(htmlFilePath);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
    }
}
