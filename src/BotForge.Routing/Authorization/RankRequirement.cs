using BotForge.Core;
using BotForge.Routing.Abstractions;

namespace BotForge.Routing.Authorization;

public class RankRequirement : IAuthorizationRequirement
{
    public required string[] AllowedRanks { get; init; }

    public Task<bool> IsSatisfiedAsync(TelegramUpdateContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var userRank = context.UserRank;
        return Task.FromResult(!string.IsNullOrEmpty(userRank) &&
                               AllowedRanks.Contains(userRank, StringComparer.OrdinalIgnoreCase));
    }
}
