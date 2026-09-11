namespace TeleForge.Core;

public static class TelegramMetricsRuntime
{
    private static readonly Lock Sync = new();

    public static bool MessagingEnabled { get; private set; }
    public static bool RoutingEnabled { get; private set; }
    public static bool ConsumerEnabled { get; private set; }
    public static bool TemplatesEnabled { get; private set; }
    public static bool PaymentsEnabled { get; private set; }

    public static void Configure(
        bool messagingEnabled,
        bool routingEnabled,
        bool consumerEnabled,
        bool templatesEnabled,
        bool paymentsEnabled)
    {
        lock (Sync)
        {
            MessagingEnabled = messagingEnabled;
            RoutingEnabled = routingEnabled;
            ConsumerEnabled = consumerEnabled;
            TemplatesEnabled = templatesEnabled;
            PaymentsEnabled = paymentsEnabled;
        }
    }
}
