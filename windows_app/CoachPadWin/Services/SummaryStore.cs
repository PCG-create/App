namespace CoachPadWin.Services;

public sealed class SummaryStore
{
    private readonly string _filePath;

    public SummaryStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoachPadWin");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "summary.txt");
    }

    public void Save(string text)
    {
        File.WriteAllText(_filePath, text);
    }

    public string? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }
        return File.ReadAllText(_filePath);
    }
}
