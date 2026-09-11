using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Routing.Handlers;
using TeleForge.Routing.Metrics;
using TeleForge.Routing.Registration;

namespace TeleForge.Routing.Routing;

public class CommandRouter(IHandlerRegistry registry, ILogger<CommandRouter> logger)
{
    public async Task RouteAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var text = context.RawUpdate.Message?.Text;
        if (!TryTokenize(text, out var tokens))
            return;

        var registration = ResolveCommandRegistration(tokens, context.BotId, out var routedCommand, out var routedArguments);
        if (registration is null)
        {
            logger.LogDebug("No handler registered for command '{Command}' in bot scope '{BotId}'", tokens[0],
                context.BotId);
            return;
        }

        // Validate required arguments
        if (routedArguments.Length < registration.CommandAttribute.RequiredArguments)
        {
            context.Result = UpdateResult.Blocked(
                $"Command {routedCommand} requires at least {registration.CommandAttribute.RequiredArguments} argument(s).");
            return;
        }

        // Validate chat type
        if (registration.ChatTypeAttribute is { ChatTypes.Length: > 0 } chatTypeAttr)
        {
            var chatType = context.RawUpdate.Message?.Chat.Type.ToString();
            if (chatType != null && !chatTypeAttr.ChatTypes.Contains(chatType, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = UpdateResult.Blocked($"Command {routedCommand} is not available in {chatType} chats.");
                return;
            }
        }

        var commandContext = new CommandContext(context, routedCommand, routedArguments, text!);

        var handler = (ICommandHandler)ActivatorUtilities.CreateInstance(
            context.RequestServices, registration.HandlerType);

        logger.LogDebug("Routing command '{Command}' to handler {Handler}", routedCommand, registration.HandlerType.Name);

        var sw = Stopwatch.StartNew();
        var hadException = false;
        try
        {
            var result = await handler.HandleAsync(commandContext, context.CancellationToken).ConfigureAwait(false);
            if (!result.Success) context.Result = UpdateResult.Blocked(result.ErrorMessage ?? "Command handler failed.");
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
            context.BotId, "command", routedCommand, sw.Elapsed, status);
        }
    }

    private CommandRegistration? ResolveCommandRegistration(
        string[] tokens,
        string? botId,
        out string routedCommand,
        out string[] routedArguments)
    {
        var candidates = registry.GetAllCommands(botId)
            .OrderByDescending(x => x.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            .ThenByDescending(x => string.Equals(x.BotId, botId, StringComparison.Ordinal))
            .ThenBy(x => x.Command.Count(c => c == '{'))
            .ToList();

        foreach (var candidate in candidates)
        {
            if (!TryMatch(candidate.Command, tokens, out var captured, out var remaining))
                continue;

            routedCommand = candidate.Command;
            routedArguments = captured.Concat(remaining).ToArray();
            return candidate;
        }

        routedCommand = tokens[0];
        routedArguments = tokens.Length > 1 ? tokens[1..] : [];
        return null;
    }

    private static bool TryTokenize(string? text, out string[] tokens)
    {
        tokens = [];
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/'))
            return false;

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        parts[0] = CommandTextParser.Normalize(parts[0]);
        if (string.IsNullOrWhiteSpace(parts[0]) || !parts[0].StartsWith('/'))
            return false;

        tokens = parts;
        return true;
    }

    private static bool TryMatch(
        string commandPattern,
        string[] tokens,
        out List<string> capturedSegments,
        out string[] remainingTokens)
    {
        capturedSegments = [];
        remainingTokens = [];

        var patternTokens = commandPattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < patternTokens.Length)
            return false;

        for (var i = 0; i < patternTokens.Length; i++)
        {
            var patternToken = patternTokens[i];
            var actualToken = i == 0 ? CommandTextParser.Normalize(tokens[i]) : tokens[i];

            if (IsPlaceholder(patternToken))
            {
                capturedSegments.Add(actualToken);
                continue;
            }

            if (!string.Equals(patternToken, actualToken, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        remainingTokens = tokens.Skip(patternTokens.Length).ToArray();
        return true;
    }

    private static bool IsPlaceholder(string token)
    {
        return token.Length >= 3 && token[0] == '{' && token[^1] == '}';
    }
}
