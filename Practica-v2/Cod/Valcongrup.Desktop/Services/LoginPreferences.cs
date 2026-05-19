namespace Valcongrup.Desktop.Services;

internal static class LoginPreferences
{
    private static string DirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Valcongrup");

    private static string EmailFilePath => Path.Combine(DirectoryPath, "remembered-email.txt");

    public static string? LoadRememberedEmail()
    {
        try
        {
            if (!File.Exists(EmailFilePath))
                return null;
            return File.ReadAllText(EmailFilePath).Trim();
        }
        catch
        {
            return null;
        }
    }

    public static void SaveRememberedEmail(string email)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(EmailFilePath, email.Trim());
        }
        catch
        {
            /* ignore */
        }
    }

    public static void ClearRememberedEmail()
    {
        try
        {
            if (File.Exists(EmailFilePath))
                File.Delete(EmailFilePath);
        }
        catch
        {
            /* ignore */
        }
    }
}
