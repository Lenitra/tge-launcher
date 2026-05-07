using System.Text.RegularExpressions;
using Xunit;

namespace LauncherTests;

public class VersionParsingTests
{
    private static readonly Regex ModFileRegex = new(@"tge_mod_([0-9]+)\.zip", RegexOptions.Compiled);

    [Theory]
    [InlineData("tge_mod_20251030120015.zip", 20251030120015L)]
    [InlineData("tge_mod_1.zip", 1L)]
    public void ModFileName_ParsesNumericVersion(string fileName, long expected)
    {
        var match = ModFileRegex.Match(fileName);
        Assert.True(match.Success);
        Assert.True(long.TryParse(match.Groups[1].Value, out var v));
        Assert.Equal(expected, v);
    }

    [Theory]
    [InlineData("LauncherTGE_20251030120015.zip")]
    [InlineData("tge_mod_.zip")]
    [InlineData("tge_mod_abc.zip")]
    [InlineData("readme.txt")]
    public void ModFileName_RejectsNonMatching(string fileName)
    {
        var match = ModFileRegex.Match(fileName);
        Assert.False(match.Success && long.TryParse(match.Groups[1].Value, out _));
    }

    [Fact]
    public void HighestVersion_IsKept()
    {
        var files = new[]
        {
            "tge_mod_100.zip",
            "tge_mod_5.zip",
            "tge_mod_999.zip",
            "tge_mod_50.zip",
        };
        long max = 0;
        foreach (var f in files)
        {
            var m = ModFileRegex.Match(f);
            if (m.Success && long.TryParse(m.Groups[1].Value, out var v) && v > max) max = v;
        }
        Assert.Equal(999L, max);
    }
}
