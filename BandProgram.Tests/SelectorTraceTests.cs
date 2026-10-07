using System.Runtime.CompilerServices;

namespace BandProgram.Tests;

[Collection("Serial")]
public class SelectorTraceTests : IDisposable
{
    private readonly List<string> lines = new();
    private DateTime now = new DateTime(2026, 10, 7, 15, 30, 12);

    public SelectorTraceTests()
    {
        SelectorTrace.ResetForTests();
        SelectorTrace.Sink = line => lines.Add(line);
        SelectorTrace.SnapshotEnabled = false;
        SelectorTrace.Now = () => now;
    }

    public void Dispose()
    {
        SelectorTrace.ResetForTests();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LookUpMissingElement()
    {
        Util.getInstance().findElement("[class='result _bandPageCount']"); // 드라이버 없음 → 실패
    }

    [Fact]
    public void Miss_through_Util_reports_selector_and_first_caller_outside_Util()
    {
        LookUpMissingElement();

        string line = Assert.Single(lines);
        Assert.Contains("[SELECTOR MISS] \"[class='result _bandPageCount']\"", line);
        Assert.Contains("← SelectorTraceTests.LookUpMissingElement (SelectorTraceTests.cs:", line);
        Assert.DoesNotContain("Util.", line);
    }

    [Fact]
    public void Miss_without_driver_still_logs_and_never_throws()
    {
        SelectorTrace.SnapshotEnabled = true; // 드라이버가 없으니 스냅샷은 건너뛴다
        SelectorTrace.Miss(".x", null);

        string line = Assert.Single(lines);
        Assert.Contains("url: (no driver)", line);
        Assert.DoesNotContain("snapshot:", line);
    }

    [Fact]
    public void Same_location_within_one_second_is_logged_once()
    {
        LookUpMissingElement();
        now = now.AddMilliseconds(500);
        LookUpMissingElement();
        Assert.Single(lines);

        now = now.AddMilliseconds(600);
        LookUpMissingElement();
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void No_sink_means_no_work()
    {
        SelectorTrace.Sink = null;
        LookUpMissingElement(); // 예외 없이 끝나야 한다
        Assert.Empty(lines);
    }

    [Fact]
    public void Throwing_sink_does_not_escape()
    {
        SelectorTrace.Sink = _ => throw new InvalidOperationException("boom");
        LookUpMissingElement();
    }
}
