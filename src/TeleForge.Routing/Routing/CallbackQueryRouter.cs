using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Core.Enums;
using TeleForge.Routing.Handlers;
using TeleForge.Routing.Metrics;
using TeleForge.Routing.Registration;
using Telegram.Bot;

namespace TeleForge.Routing.Routing;

public class CallbackQueryRouter(
    IHandlerRegistry registry,
    ITelegramBotClientProvider clientProvider,
    CallbackAnswerStrategy answerStrategy,
    ILogger<CallbackQueryRouter> logger)
{
    public async Task RouteAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var callbackQuery = context.RawUpdate.CallbackQuery;
        if (callbackQuery?.Data is not { } callbackData)
            return;

        var callbackId = callbackQuery.Id;
        var client = clientProvider.GetClient(context.BotId);

        if (answerStrategy == CallbackAnswerStrategy.ProcessFirst)
        {
            await ProcessFirstAsync(context, client, callbackId, callbackData).ConfigureAwait(false);
        }
        else
        {
            await AnswerFirstAsync(context, client, callbackId, callbackData).ConfigureAwait(false);
        }
    }

    private async Task AnswerFirstAsync(TelegramUpdateContext context, ITelegramBotClient client,
        string callbackId, string callbackData)
    {
        var answerTask = AnswerSilentlyAsync(client, callbackId, context.CancellationToken);

        var registration = registry.FindCallback(callbackData);
        if (registration is null)
        {
            logger.LogInformation(
                "No handler registered for callback data '{CallbackData}' (id={CallbackId}, user={UserId})",
                callbackData, callbackId, context.UserId);
            await answerTask.ConfigureAwait(false);
            return;
        }

        var routeParams = ExtractRouteParameters(callbackData, registration.Pattern);
        var callbackContext = new CallbackContext(context, callbackData, callbackId, routeParams);
        var handler = (ICallbackQueryHandler)ActivatorUtilities.CreateInstance(
            context.RequestServices, registration.HandlerType);

        logger.LogInformation(
            "Routing callback '{CallbackData}' (pattern='{Pattern}') to handler {Handler} (id={CallbackId}, user={UserId})",
            callbackData, registration.Pattern, registration.HandlerType.Name, callbackId, context.UserId);

        var sw = Stopwatch.StartNew();
        var hadException = false;
        try
        {
            await answerTask.ConfigureAwait(false);
            var result = await handler.HandleAsync(callbackContext, context.CancellationToken).ConfigureAwait(false);
            if (!result.Success) context.Result = UpdateResult.Blocked(result.ErrorMessage ?? "Callback handler failed.");
        }
        catch
        {
            hadException = true;
            throw;
        }
        finally
        {
            sw.Stop();
            var pattern = registration.Pattern;
            var status = hadException ? "exception" : context.Result?.Success == false ? "blocked" : "success";
            RoutingMetrics.RecordHandlerCall(
                context.BotId, "callback_query", pattern, sw.Elapsed, status);
        }
    }

    private async Task ProcessFirstAsync(TelegramUpdateContext context, ITelegramBotClient client,
        string callbackId, string callbackData)
    {
        var registration = registry.FindCallback(callbackData);
        if (registration is null)
        {
            logger.LogInformation(
                "No handler registered for callback data '{CallbackData}' (id={CallbackId}, user={UserId})",
                callbackData, callbackId, context.UserId);
            return;
        }

        var routeParams = ExtractRouteParameters(callbackData, registration.Pattern);
        var callbackContext = new CallbackContext(context, callbackData, callbackId, routeParams);
        var handler = (ICallbackQueryHandler)ActivatorUtilities.CreateInstance(
            context.RequestServices, registration.HandlerType);

        logger.LogInformation(
            "Routing callback '{CallbackData}' (pattern='{Pattern}') to handler {Handler} (id={CallbackId}, user={UserId})",
            callbackData, registration.Pattern, registration.HandlerType.Name, callbackId, context.UserId);

        var sw = Stopwatch.StartNew();
        var hadException = false;
        try
        {
            var result = await handler.HandleAsync(callbackContext, context.CancellationToken).ConfigureAwait(false);
            if (!result.Success) context.Result = UpdateResult.Blocked(result.ErrorMessage ?? "Callback handler failed.");
        }
        catch
        {
            hadException = true;
            throw;
        }
        finally
        {
            await AnswerSilentlyAsync(client, callbackId, context.CancellationToken).ConfigureAwait(false);
            sw.Stop();
            var pattern = registration.Pattern;
            var status = hadException ? "exception" : context.Result?.Success == false ? "blocked" : "success";
            RoutingMetrics.RecordHandlerCall(
                context.BotId, "callback_query", pattern, sw.Elapsed, status);
        }
    }

    private static async Task AnswerSilentlyAsync(ITelegramBotClient client, string callbackId,
        CancellationToken ct)
    {
        try
        {
            await client.AnswerCallbackQuery(callbackId, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    private static Dictionary<string, string> ExtractRouteParameters(string data, string pattern)
    {
        var parameters = new Dictionary<string, string>();
        var dataParts = data.Split(':');
        var patternParts = pattern.Split(':');

        if (dataParts.Length != patternParts.Length)
            return parameters;

        for (var i = 0; i < patternParts.Length; i++)
        {
            var pp = patternParts[i];
            if (!pp.StartsWith('{') || !pp.EndsWith('}'))
                continue;

            var paramDef = pp[1..^1];
            var colonIndex = paramDef.IndexOf(':');
            var paramName = colonIndex > 0 ? paramDef[..colonIndex] : paramDef;

            parameters[paramName] = dataParts[i];
        }

        return parameters;
    }
}
