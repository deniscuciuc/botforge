using Telegram.Bot.Types.Enums;

namespace TeleForge.Core;

public class BotConfiguration
{
    public string Key { get; set; } = null!;
    public string Token { get; set; } = null!;
    public UpdateType[] AllowedUpdates { get; set; } = [];
    public bool DedicatedTransport { get; set; }
    public int ConcurrencyLimit { get; set; } = 50;
    public UpdateTransport Transport { get; set; } = UpdateTransport.LongPolling;
    public BotTransportOptions TransportOptions { get; set; } = new();
    public BotRateLimitOptions RateLimit { get; set; } = new();
}

public class BotTransportOptions
{
    public int MaxConnections { get; set; } = 8;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(5);
}

public class BotRateLimitOptions
{
    public int GlobalPerSecond { get; set; } = 30;
    public int PerChatPerSecond { get; set; } = 1;
    public int GroupPerMinute { get; set; } = 20;
}
