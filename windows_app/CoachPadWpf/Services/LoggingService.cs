using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace CoachPadWpf;

public sealed class LoggingService
{
    private readonly string _filePath;

    public LoggingService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoachPadWpf");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "logs.dat");
    }

    public void LogInfo(string message)
    {
        WriteEntry("INFO", message);
    }

    public void LogError(string message)
    {
        WriteEntry("ERROR", message);
    }

    private void WriteEntry(string level, string message)
    {
        var line = $"{DateTimeOffset.UtcNow:o} {level} {message}\n";
        var existing = ReadRaw();
        var combined = existing + line;
        var bytes = Encoding.UTF8.GetBytes(combined);
        var protectedData = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, protectedData);
    }

    private string ReadRaw()
    {
        if (!File.Exists(_filePath))
        {
            return string.Empty;
        }
        try
        {
            var bytes = File.ReadAllBytes(_filePath);
            var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(unprotected);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void DeleteAll()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}
