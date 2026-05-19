using System.IO;

namespace Valcongrup.Forms;

internal static class CrashLogger
{
    public static void Log(Exception ex, string context)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Valcongrup");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "crash.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{context}]\r\n{ex}\r\n\r\n");
        }
        catch
        {
            // Logging should never crash the application.
        }
    }
}
