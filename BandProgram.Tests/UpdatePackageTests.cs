using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace BandProgram.Tests;

public class UpdatePackageTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), $"band-pkg-{Guid.NewGuid():N}");

    public UpdatePackageTests() => Directory.CreateDirectory(dir);

    public void Dispose() => Directory.Delete(dir, true);

    private string MakeZip(params (string name, string content)[] entries)
    {
        string path = Path.Combine(dir, $"{Guid.NewGuid():N}.zip");
        using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using Stream s = zip.CreateEntry(name).Open();
            s.Write(Encoding.UTF8.GetBytes(content));
        }
        return path;
    }

    [Fact]
    public void Sha256Hex_is_lowercase_hex_of_file()
    {
        string file = Path.Combine(dir, "a.bin");
        File.WriteAllBytes(file, new byte[] { 1, 2, 3 });
        Assert.Equal(Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 3 })).ToLowerInvariant(), UpdatePackage.Sha256Hex(file));
    }

    [Fact]
    public void TryExtract_accepts_exactly_the_two_files()
    {
        string zip = MakeZip(("BandProgram.exe", "exe"), ("selenium-manager.exe", "sm"));
        string target = Path.Combine(dir, "out");
        Directory.CreateDirectory(target);

        Assert.True(UpdatePackage.TryExtract(zip, target, out string error), error);
        Assert.Equal("exe", File.ReadAllText(Path.Combine(target, "BandProgram.exe")));
        Assert.Equal("sm", File.ReadAllText(Path.Combine(target, "selenium-manager.exe")));
    }

    [Fact]
    public void TryExtract_ignores_name_case_but_writes_canonical_names()
    {
        string zip = MakeZip(("bandprogram.EXE", "exe"), ("Selenium-Manager.exe", "sm"));
        string target = Path.Combine(dir, "out");
        Directory.CreateDirectory(target);

        Assert.True(UpdatePackage.TryExtract(zip, target, out _));
        Assert.Equal(new[] { "BandProgram.exe", "selenium-manager.exe" }, Directory.GetFiles(target).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("BandProgram.exe")]                                   // selenium-manager 없음
    [InlineData("BandProgram.exe|selenium-manager.exe|extra.dll")]    // 추가 파일
    [InlineData("sub/BandProgram.exe|selenium-manager.exe")]          // 폴더
    [InlineData("..\\BandProgram.exe|selenium-manager.exe")]          // 상위 경로
    [InlineData("BandProgram.exe|BandProgram.exe")]                   // 중복
    public void TryExtract_rejects_unexpected_entries(string names)
    {
        string zip = MakeZip(names.Split('|').Select(n => (n, "x")).ToArray());
        string target = Path.Combine(dir, "out");
        Directory.CreateDirectory(target);

        Assert.False(UpdatePackage.TryExtract(zip, target, out string error));
        Assert.False(string.IsNullOrEmpty(error));
        Assert.Empty(Directory.GetFiles(target));
    }

    [Fact]
    public void TryExtract_rejects_corrupt_zip()
    {
        string bad = Path.Combine(dir, "bad.zip");
        File.WriteAllText(bad, "not a zip");
        Assert.False(UpdatePackage.TryExtract(bad, dir, out _));
    }
}
