using System.Text.Json;

namespace BandProgram.Dev;

internal sealed class JobParams
{
    public int Type { get; set; }
    public int BetweenWorkSec { get; set; } = 60;
    public bool Reserved { get; set; }
    public bool Paste { get; set; } = true;
    public int ReserveHour { get; set; }
    public int ReserveMin { get; set; }
    public int RepeatCount { get; set; } = 1;
    public int RepeatBetweenSec { get; set; } = 60;
}

internal sealed class DevConfig
{
    public JobParams Posting { get; set; } = new();
    public JobParams Comment { get; set; } = new();
    public JobParams Chatting { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static DevConfig Load(string path)
    {
        DevConfig config = JsonSerializer.Deserialize<DevConfig>(File.ReadAllText(path), Options) ?? new DevConfig();
        config.Posting ??= new JobParams();
        config.Comment ??= new JobParams();
        config.Chatting ??= new JobParams();
        return config;
    }
}
