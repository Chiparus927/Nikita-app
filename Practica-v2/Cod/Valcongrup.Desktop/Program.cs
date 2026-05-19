using Valcongrup.Forms;

namespace Valcongrup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        StartupTrace("Main start");
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => LogCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) LogCrash(ex);
        };
        StartupTrace("Before FormLogin ctor");
        var login = new FormLogin();
        StartupTrace("After FormLogin ctor");
        Application.Run(login);
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Valcongrup");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "crash.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n{ex}\r\n\r\n");
            MessageBox.Show(
                "A apărut o eroare. Detaliile au fost salvate în crash.log.",
                "VALCONGRUP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            MessageBox.Show(ex.Message, "VALCONGRUP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal static void StartupTrace(string message)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "startup.log"),
                $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

}
