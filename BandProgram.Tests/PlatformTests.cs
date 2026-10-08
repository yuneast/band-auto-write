using System.Diagnostics;

namespace BandProgram.Tests;

[Collection("Serial")]
public class PlatformTests
{
    [Fact]
    public void Adb_executable_matches_os()
    {
        Assert.Equal(OperatingSystem.IsWindows() ? "adb.exe" : "adb", ADB.Executable);
    }

    [Fact]
    public void Util_clipboard_defaults_to_pbcopy_on_mac()
    {
        if (!OperatingSystem.IsMacOS()) return;
        Assert.IsType<PbcopyClipboard>(Util.Clipboard);
    }

    [Fact]
    public void Pbcopy_clipboard_keeps_korean_and_newlines()
    {
        if (!OperatingSystem.IsMacOS()) return;
        string previous = RunPbpaste();
        try
        {
            new PbcopyClipboard().SetText("한글 붙여넣기\n둘째 줄");
            Assert.Equal("한글 붙여넣기\n둘째 줄", RunPbpaste());
        }
        finally
        {
            new PbcopyClipboard().SetText(previous); // 사용자의 클립보드 복원
        }
    }

    private static string RunPbpaste()
    {
        var psi = new ProcessStartInfo("pbpaste") { RedirectStandardOutput = true };
        psi.Environment["LANG"] = "en_US.UTF-8";
        using Process p = Process.Start(psi)!;
        string text = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return text;
    }
}
