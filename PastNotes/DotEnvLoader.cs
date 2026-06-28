namespace PastNotes;

public static class DotEnvLoader
{
    public static void Load(string filePath = ".env")
    {
        if (!File.Exists(filePath))
            return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key   = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            // 既存の環境変数は上書きしない
            if (Environment.GetEnvironmentVariable(key) == null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
