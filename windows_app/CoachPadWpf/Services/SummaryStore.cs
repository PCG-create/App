using System.Security.Cryptography;
using System.Text;

namespace CoachPadWpf;

public sealed class SummaryStore
{
    private readonly string _filePath;

    public SummaryStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoachPadWpf");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "summary.dat");
    }

    public void Save(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var protectedData = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, protectedData);
    }

    public string? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }
        var bytes = File.ReadAllBytes(_filePath);
        var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(unprotected);
    }

    public void DeleteAll()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}
