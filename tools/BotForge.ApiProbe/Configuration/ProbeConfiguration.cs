namespace BotForge.ApiProbe.Configuration;

public sealed class ProbeConfiguration
{
    public List<BotEntry> Bots { get; set; } = [];
    public long PrivateChatId { get; set; }
    public long GroupChatId { get; set; }
    public long SupergroupChatId { get; set; }
    public long ChannelChatId { get; set; }
    public List<long> FanoutChatIds { get; set; } = [];
    public string? TestPhotoUrl { get; set; }
    public ProbeDefaults Defaults { get; set; } = new();
    public string OutputDirectory { get; set; } = "./reports";
}

public sealed class BotEntry
{
    public string Name { get; set; } = "";
    public string Token { get; set; } = "";
}

public sealed class ProbeDefaults
{
    public int MessageCount { get; set; } = 50;
    public int RampStartRps { get; set; } = 1;
    public int RampStepRps { get; set; } = 2;
    public int RampStepDurationSeconds { get; set; } = 5;
    public int CooldownSeconds { get; set; } = 60;
    public bool CleanupAfterRun { get; set; }
}
