using BotForge.Core;
using BotForge.Routing.Abstractions;

namespace BotForge.Routing.Authorization;

public class PolicyBuilder
{
    private readonly List<IAuthorizationRequirement> _requirements = [];

    public PolicyBuilder AddRequirement(IAuthorizationRequirement requirement)
    {
        _requirements.Add(requirement);
        return this;
    }

    public PolicyBuilder RequirePermission(params string[] permissions)
    {
        _requirements.Add(new PermissionRequirement { Permissions = permissions });
        return this;
    }

    public PolicyBuilder RequireRank(params string[] ranks)
    {
        _requirements.Add(new RankRequirement { AllowedRanks = ranks });
        return this;
    }

    public PolicyBuilder RequireChatType(params string[] chatTypes)
    {
        _requirements.Add(new ChatTypeRequirement { AllowedChatTypes = chatTypes });
        return this;
    }

    public IAuthorizationPolicy Build(string name)
    {
        return new CompositeAuthorizationPolicy(name, _requirements.ToArray());
    }
}

internal sealed class CompositeAuthorizationPolicy(string name, IAuthorizationRequirement[] requirements)
    : IAuthorizationPolicy
{
    public string Name { get; } = name;

    public async Task<AuthorizationResult> EvaluateAsync(TelegramUpdateContext context, CancellationToken ct = default)
    {
        foreach (var requirement in requirements)
            if (!await requirement.IsSatisfiedAsync(context, ct).ConfigureAwait(false))
                return AuthorizationResult.Fail($"Requirement {requirement.GetType().Name} not satisfied.");

        return AuthorizationResult.Success();
    }
}
