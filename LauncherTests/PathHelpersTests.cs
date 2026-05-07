using Xunit;

namespace LauncherTests;

public class PathHelpersTests
{
    [Fact]
    public void GetAllDrives_ContainsCDrive()
    {
        var drives = Program.GetAllDrives();
        Assert.NotEmpty(drives);
        Assert.Contains(drives, d => d.StartsWith("C:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetCredentials_NoFile_ReturnsNull()
    {
        var prevDir = Environment.CurrentDirectory;
        var temp = Path.Combine(Path.GetTempPath(), "tge-launcher-creds-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Environment.CurrentDirectory = temp;
            var creds = Program.GetCredentials();
            Assert.Null(creds);
        }
        finally
        {
            Environment.CurrentDirectory = prevDir;
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    [Fact]
    public void GetCredentials_WithFile_ReturnsParsed()
    {
        var prevDir = Environment.CurrentDirectory;
        var temp = Path.Combine(Path.GetTempPath(), "tge-launcher-creds-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Environment.CurrentDirectory = temp;
            File.WriteAllText("credentials.json", "{\"Email\":\"a@b.c\",\"Password\":\"secret\"}");
            var creds = Program.GetCredentials();
            Assert.NotNull(creds);
            Assert.Equal("a@b.c", creds!.Email);
            Assert.Equal("secret", creds.Password);
        }
        finally
        {
            Environment.CurrentDirectory = prevDir;
            try { Directory.Delete(temp, true); } catch { }
        }
    }
}
