using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace LauncherTests;

[Trait("Category", "Integration")]
public class FlaUIFlowTests
{
    [Fact]
    public void Launcher_NoEts2Launch_ReachesHomeAndExits()
    {
        string? buildDir = FindLatestBuildDir();
        Assert.True(buildDir != null,
            "[SETUP REQUIRED] Aucun dossier 'build/v*' trouvé. Compile d'abord via LauncherCompile.bat.");

        string exe = Path.Combine(buildDir!, "LauncherSrc.exe");
        Assert.True(File.Exists(exe), $"[SETUP REQUIRED] Exécutable introuvable: {exe}");

        string credsPath = Path.Combine(buildDir!, "credentials.json");
        Assert.True(HasUsableCredentials(credsPath),
            "[SETUP REQUIRED] credentials.json absent ou vide dans " + credsPath +
            ". Renseigne tes identifiants TrucksBook avant de lancer ce test.");

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = "--no-ets2-launch",
            WorkingDirectory = buildDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            CreateNoWindow = true,
        };

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        bool exited = proc.WaitForExit((int)TimeSpan.FromMinutes(3).TotalMilliseconds);
        if (!exited)
        {
            try { proc.Kill(true); } catch { }
            throw new Xunit.Sdk.XunitException(
                "Le launcher ne s'est pas terminé en 3 minutes en mode --no-ets2-launch.\n" +
                "STDOUT:\n" + stdout + "\nSTDERR:\n" + stderr);
        }

        // Ré-attendre la flush des streams après exit
        proc.WaitForExit();

        string output = stdout.ToString();
        // Marqueurs ASCII-safe pour eviter les pieges d'encodage console.
        Assert.True(
            output.Contains("Page 'home'") || output.Contains("ETS2 a"),
            "Le launcher n'a jamais atteint la page 'home'.\nSTDOUT:\n" + output + "\nSTDERR:\n" + stderr);

        Assert.Equal(0, proc.ExitCode);
    }

    private static string? FindLatestBuildDir()
    {
        string? root = FindRepoRoot();
        if (root == null) return null;

        foreach (var name in new[] { "build", "Build" })
        {
            var dir = Path.Combine(root, name);
            if (!Directory.Exists(dir)) continue;
            var versions = Directory.GetDirectories(dir, "v*")
                .OrderByDescending(d => Directory.GetLastWriteTimeUtc(d))
                .ToList();
            if (versions.Count > 0) return versions[0];
        }
        return null;
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static bool HasUsableCredentials(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            string? email = root.TryGetProperty("Email", out var e) ? e.GetString() : null;
            string? pwd = root.TryGetProperty("Password", out var p) ? p.GetString() : null;
            return !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(pwd);
        }
        catch { return false; }
    }
}
