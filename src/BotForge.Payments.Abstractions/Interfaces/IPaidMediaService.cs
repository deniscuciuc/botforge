using Telegram.Bot.Types;

namespace BotForge.Payments.Abstractions;

/// <summary>
/// Service for sending paid media and handling paid media purchases.
/// </summary>
public interface IPaidMediaService
{
    Task<Message> SendPaidMediaAsync(PaidMediaDefinition definition, CancellationToken ct = default);
}

/// <summary>
/// Handles purchases of paid media by users.
/// </summary>
public interface IPaidMediaPurchaseHandler
{
    Task HandleAsync(PaidMediaPurchaseContext context, CancellationToken ct = default);
}
