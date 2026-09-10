using System.Diagnostics;
using BotForge.Core;
using BotForge.Routing.Handlers;
using BotForge.Routing.Metrics;
using BotForge.Routing.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotForge.Routing.Routing;

/// <summary>
/// Routes plain text messages to handlers registered with [TextMessage].
/// </summary>
public class TextMessageRouter(IHandlerRegistry registry, ILogger<TextMessageRouter> logger)
{
    public async Task RouteAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var text = context.RawUpdate.Message?.Text;
        if (string.IsNullOrEmpty(text))
            return;

        var registration = registry.FindTextHandler(text);
        if (registration == null)
        {
            logger.LogDebug("No text handler found for message: {Text}", text[..Math.Min(text.Length, 50)]);
            return;
        }

        logger.LogDebug("Routing text message to {Handler}", registration.HandlerType.Name);

        var handler = (ITextMessageHandler)ActivatorUtilities.CreateInstance(
            context.RequestServices, registration.HandlerType);

        var textContext = new TextMessageContext(context, text);

        var sw = Stopwatch.StartNew();
        var hadException = false;
        try
        {
            await handler.HandleAsync(textContext, context.CancellationToken).ConfigureAwait(false);
        }
        catch
        {
            hadException = true;
            throw;
        }
        finally
        {
            sw.Stop();
            var status = hadException ? "exception" : context.Result?.Success == false ? "blocked" : "success";
            RoutingMetrics.RecordHandlerCall(
                context.BotId, "text_message", registration.HandlerType.Name, sw.Elapsed, status);
        }
    }
}
