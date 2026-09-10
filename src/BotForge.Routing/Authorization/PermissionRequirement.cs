using BotForge.Core;
using BotForge.Routing.Abstractions;

namespace BotForge.Routing.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public required string[] Permissions { get; init; }

    public Task<bool> IsSatisfiedAsync(TelegramUpdateContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Items.TryGetValue("Permissions", out var raw) || raw is not IEnumerable<string> userPermissions)
            return Task.FromResult(false);

        var permSet = userPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(Permissions.All(p => permSet.Contains(p)));
    }
}
