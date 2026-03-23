using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace tododeploy;

public sealed class AppLogger
{
    public AppLogger()
    {
        var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TodoDeploy");
        Directory.CreateDirectory(basePath);
        LogFilePath = Path.Combine(basePath, "app.log");
    }

    public string LogFilePath { get; }

    public async Task LogAsync(string action, string detail)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {action}: {detail}";
        Debug.WriteLine(line);
        await File.AppendAllTextAsync(LogFilePath, line + Environment.NewLine);
    }
}
