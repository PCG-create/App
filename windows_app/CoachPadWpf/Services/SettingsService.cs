using System.Text.Json;

namespace CoachPadWpf;

public sealed class SettingsService
{
    private readonly string _filePath;

    public SettingsService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoachPadWpf");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
    }

    public AppSettings LoadOrCreate()
    {
        if (!File.Exists(_filePath))
        {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }
        var json = File.ReadAllText(_filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json);
        return settings ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_filePath, json);
    }

    public void Delete()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}

public sealed class AppSettings
{
    public string Host { get; set; } = "127.0.0.1:8000";
    public bool IsSystemAudioEnabled { get; set; } = true;
    public bool IsMicEnabled { get; set; } = true;
    public bool HasAudioConsent { get; set; } = false;
    public bool HasCameraConsent { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool FollowActiveWindow { get; set; } = true;
}
