using BotForge.Core;
using BotForge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace AdvancedBot.Middleware;

/// <summary>
/// Custom send middleware that logs every outgoing message.
/// Demonstrates how to create custom ISendMiddleware.
/// </summary>
public class AuditSendMiddleware(ILogger<AuditSendMiddleware> logger) : ISendMiddleware
{
    public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
    {
        logger.LogInformation(
            "Sending message to chat {ChatId} via bot {BotId} (attempt {Attempt})",
            context.Message.ChatId,
            context.Bot.Key,
            context.Attempt);

        var result = await next(context);

        if (result.Success)
            logger.LogInformation("Message sent successfully to chat {ChatId}", context.Message.ChatId);
        else
            logger.LogWarning("Message send failed to chat {ChatId}: {Error}", context.Message.ChatId, result.Error);

        return result;
    }
}
