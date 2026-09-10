using BotForge.Core;

namespace BotForge.Routing.Abstractions;

public interface IAuthorizationPolicy
{
    string Name { get; }
    Task<AuthorizationResult> EvaluateAsync(TelegramUpdateContext context, CancellationToken ct = default);
}

public interface IAuthorizationRequirement
{
    Task<bool> IsSatisfiedAsync(TelegramUpdateContext context, CancellationToken ct = default);
}

public class AuthorizationResult
{
    public bool Succeeded { get; init; }
    public string? FailureReason { get; init; }

    public static AuthorizationResult Success()
    {
        return new AuthorizationResult { Succeeded = true };
    }

    public static AuthorizationResult Fail(string reason)
    {
        return new AuthorizationResult { Succeeded = false, FailureReason = reason };
    }
}
