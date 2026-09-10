using Microsoft.Extensions.DependencyInjection;

namespace BotForge.Streaming;

/// <summary>
///     Extension methods for registering Telegram streaming services with the DI container.
/// </summary>
public static class TelegramStreamingExtensions
{
    /// <summary>
    ///     Registers <see cref="ITelegramDraftService" />, <see cref="StreamingSessionGuard" />,
    ///     and <see cref="StreamingResponseSender" /> as DI services.
    /// </summary>

    public static IServiceCollection AddTelegramStreaming(
        this IServiceCollection services,
        Action<TelegramStreamingOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<TelegramStreamingOptions>();
        if (configure is not null)
            optionsBuilder.PostConfigure(configure);

        services.AddSingleton<ITelegramDraftService, TelegramDraftService>();
        services.AddSingleton<StreamingSessionGuard>();
        services.AddScoped<StreamingResponseSender>();

        return services;
    }
}
