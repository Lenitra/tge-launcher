using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("LauncherTests")]

public static class ErrorLogger
{
    internal static string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly object _fileLock = new();

    static ErrorLogger()
    {
        try { Directory.CreateDirectory(LogDir); } catch { /* best effort */ }
    }

    public static void LogException(Exception? ex, string context = "")
    {
        if (ex == null) return;
        try
        {
            WriteToFile(BuildFullText(ex, context));
        }
        catch (Exception loggerEx)
        {
            try { Console.WriteLine($"[ErrorLogger] Erreur interne: {loggerEx.Message}"); } catch { }
        }
    }

    public static void RegisterGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                LogException(ex, "UnhandledException");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogException(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        };

        System.Windows.Forms.Application.ThreadException += (s, e) =>
        {
            LogException(e.Exception, "WinForms ThreadException");
        };
    }

    internal static string BuildFullText(Exception ex, string context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        if (!string.IsNullOrWhiteSpace(context))
            sb.AppendLine($"Contexte: {context}");
        sb.AppendLine($"Machine: {Environment.MachineName}  User: {Environment.UserName}");
        sb.AppendLine($"OS: {Environment.OSVersion}  CLR: {Environment.Version}");
        sb.AppendLine();
        sb.AppendLine(ex.ToString());
        sb.AppendLine();
        return sb.ToString();
    }

    internal static void WriteToFile(string text)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            string file = Path.Combine(LogDir, $"errors_{DateTime.Now:yyyy-MM-dd}.log");
            lock (_fileLock)
            {
                File.AppendAllText(file, text);
            }
        }
        catch (Exception ex)
        {
            try { Console.WriteLine($"[ErrorLogger] Echec ecriture fichier: {ex.Message}"); } catch { }
        }
    }
}
