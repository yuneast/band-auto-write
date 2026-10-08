using System.Text;

namespace BandProgram.Tests;

public class UpdateManifestTests
{
    private const string Sha = "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec";

    private static bool Parse(string json, out UpdateManifest m, out string error)
        => UpdateManifest.TryParse(Encoding.UTF8.GetBytes(json), out m, out error);

    [Fact]
    public void Parses_valid_manifest()
    {
        Assert.True(Parse($$"""{ "version": "2026.10.8.1015", "url": "http://newsoft.kr/download/BandProgram-2026.10.8.1015.zip", "sha256": "{{Sha}}" }""", out var m, out var error), error);
        Assert.Equal(new Version(2026, 10, 8, 1015), m.Version);
        Assert.Equal("http://newsoft.kr/download/BandProgram-2026.10.8.1015.zip", m.Url.ToString());
        Assert.Equal(Sha, m.Sha256);
    }

    [Fact]
    public void Sha256_is_normalized_to_lowercase()
    {
        Assert.True(Parse($$"""{ "version": "1.2", "url": "https://x/a.zip", "sha256": "{{Sha.ToUpperInvariant()}}" }""", out var m, out _));
        Assert.Equal(Sha, m.Sha256);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{ "url": "http://x/a.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "abc", "url": "http://x/a.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "url": "file:///c:/evil.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "url": "relative/a.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "url": "http://x/a.zip", "sha256": "1234" }""")]
    [InlineData("""{ "version": "1.0", "url": "http://x/a.zip", "sha256": "zz62ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    public void Rejects_invalid_manifest(string json)
    {
        Assert.False(Parse(json, out var m, out var error));
        Assert.Null(m);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
