using TeleForge.Core;
using TeleForge.Routing.Abstractions;

namespace TeleForge.Routing.Authorization;

public class ChatTypeRequirement : IAuthorizationRequirement
{
    public required string[] AllowedChatTypes { get; init; }

    public Task<bool> IsSatisfiedAsync(TelegramUpdateContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue("ChatType", out var raw) && raw is string chatType)
            return Task.FromResult(AllowedChatTypes.Contains(chatType, StringComparer.OrdinalIgnoreCase));

        var resolved = context.RawUpdate.Message?.Chat.Type.ToString()
                       ?? context.RawUpdate.CallbackQuery?.Message?.Chat.Type.ToString();

        if (resolved is null)
            return Task.FromResult(false);

        chatType = resolved;

        return Task.FromResult(AllowedChatTypes.Contains(chatType, StringComparer.OrdinalIgnoreCase));
    }
}
