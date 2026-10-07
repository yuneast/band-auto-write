using BandProgram.Dev;

namespace BandProgram.Tests;

public class DevConfigTests
{
    [Fact]
    public void Loads_job_params_from_json()
    {
        string file = Path.Combine(Path.GetTempPath(), $"dev-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, """
        {
          "posting":  { "type": 1, "betweenWorkSec": 60, "reserved": false, "paste": true,
                        "reserveHour": 9, "reserveMin": 30, "repeatCount": 0, "repeatBetweenSec": 3600 }
        }
        """);
        try
        {
            DevConfig config = DevConfig.Load(file);
            Assert.Equal(1, config.Posting.Type);
            Assert.Equal(60, config.Posting.BetweenWorkSec);
            Assert.True(config.Posting.Paste);
            Assert.Equal(9, config.Posting.ReserveHour);
            Assert.Equal(3600, config.Posting.RepeatBetweenSec);
            Assert.NotNull(config.Comment); // 빠진 항목은 기본값
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Example_file_in_repo_is_valid()
    {
        string example = Path.Combine(FindRepoRoot(), "devdata.example", "dev.json");
        DevConfig config = DevConfig.Load(example);
        Assert.NotNull(config.Posting);
        Assert.NotNull(config.Comment);
        Assert.NotNull(config.Chatting);
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "BandProgram.sln")))
        {
            dir = Path.GetDirectoryName(dir) ?? throw new InvalidOperationException("repo root not found");
        }
        return dir;
    }
}
