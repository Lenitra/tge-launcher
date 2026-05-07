using System.Text.Json;
using Xunit;

namespace LauncherTests;

public class CredentialsTests
{
    [Fact]
    public void Roundtrip_PreservesEmailAndPassword()
    {
        var creds = new Program.Credentials { Email = "user@example.com", Password = "p@ss w0rd" };
        string json = JsonSerializer.Serialize(creds);
        var parsed = JsonSerializer.Deserialize<Program.Credentials>(json);

        Assert.NotNull(parsed);
        Assert.Equal("user@example.com", parsed!.Email);
        Assert.Equal("p@ss w0rd", parsed.Password);
    }

    [Fact]
    public void EmptyJson_DeserializesWithEmptyFields()
    {
        var parsed = JsonSerializer.Deserialize<Program.Credentials>("{\"Email\":\"\",\"Password\":\"\"}");
        Assert.NotNull(parsed);
        Assert.Equal(string.Empty, parsed!.Email);
        Assert.Equal(string.Empty, parsed.Password);
    }
}
