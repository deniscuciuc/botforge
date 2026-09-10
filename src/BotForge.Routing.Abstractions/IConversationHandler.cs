using BotForge.Core;

namespace BotForge.Routing.Abstractions;

/// <summary>
/// Handler for a conversation step. Receives user input within an active conversation flow.
/// </summary>
public interface IConversationHandler
{
    Task<ConversationResult> HandleStepAsync(ConversationStepContext context, CancellationToken ct);
}

/// <summary>
/// Context for a conversation step handler.
/// </summary>
public class ConversationStepContext(
    TelegramUpdateContext updateContext,
    ConversationStep step,
    string input)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public ConversationStep Step { get; } = step;
    public string Input { get; } = input;

    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;

    public string? GetStepData(string key)
    {
        return Step.Data.TryGetValue(key, out var value) ? value : null;
    }
}

/// <summary>
/// Result of a conversation step.
/// </summary>
public class ConversationResult
{
    public bool Completed { get; init; }
    public ConversationStep? NextStep { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>Conversation completed successfully.</summary>
    public static ConversationResult Complete()
    {
        return new ConversationResult { Completed = true };
    }

    /// <summary>Move to the next step.</summary>
    public static ConversationResult GoTo(ConversationStep nextStep)
    {
        return new ConversationResult { Completed = false, NextStep = nextStep };
    }

    /// <summary>Validation failed, stay on current step.</summary>
    public static ConversationResult Invalid(string errorMessage)
    {
        return new ConversationResult { Completed = false, ErrorMessage = errorMessage };
    }
}
