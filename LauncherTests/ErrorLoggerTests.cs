using Xunit;

namespace LauncherTests;

public class ErrorLoggerTests : IDisposable
{
    private readonly string _originalLogDir;
    private readonly string _tempDir;

    public ErrorLoggerTests()
    {
        _originalLogDir = ErrorLogger.LogDir;
        _tempDir = Path.Combine(Path.GetTempPath(), "tge-launcher-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        ErrorLogger.LogDir = _tempDir;
    }

    public void Dispose()
    {
        ErrorLogger.LogDir = _originalLogDir;
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void LogException_NullException_DoesNothing()
    {
        ErrorLogger.LogException(null, "context-null");
        Assert.Empty(Directory.GetFiles(_tempDir));
    }

    [Fact]
    public void LogException_WritesFileWithStackTrace()
    {
        Exception thrown;
        try { ThrowDeep(); return; } catch (Exception ex) { thrown = ex; }

        ErrorLogger.LogException(thrown, "unit-test-context");

        var files = Directory.GetFiles(_tempDir, "errors_*.log");
        Assert.Single(files);

        string content = File.ReadAllText(files[0]);
        Assert.Contains("unit-test-context", content);
        Assert.Contains("ThrowDeep", content);
        Assert.Contains("InvalidOperationException", content);
    }

    [Fact]
    public void BuildFullText_IncludesContextAndTimestamp()
    {
        var ex = new Exception("hello");
        string text = ErrorLogger.BuildFullText(ex, "my-ctx");
        Assert.Contains("Contexte: my-ctx", text);
        Assert.Contains("Machine:", text);
        Assert.Contains("OS:", text);
        Assert.Contains("hello", text);
    }

    [Fact]
    public void WriteToFile_AppendsToDailyFile()
    {
        ErrorLogger.WriteToFile("ligne-1\n");
        ErrorLogger.WriteToFile("ligne-2\n");

        var files = Directory.GetFiles(_tempDir, "errors_*.log");
        Assert.Single(files);
        string content = File.ReadAllText(files[0]);
        Assert.Contains("ligne-1", content);
        Assert.Contains("ligne-2", content);
    }

    private static void ThrowDeep()
    {
        throw new InvalidOperationException("deep-fault");
    }
}
