using BotForge.Core;
using Telegram.Bot.Types;

namespace BotForge.Routing.Handlers;

public interface IUsersSharedHandler
{
    Task HandleAsync(UsersSharedContext context, CancellationToken ct);
}

public class UsersSharedContext(TelegramUpdateContext updateContext, UsersShared usersShared)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public UsersShared UsersShared { get; } = usersShared;
    public int RequestId => UsersShared.RequestId;
    public IReadOnlyList<SharedUser> Users => UsersShared.Users;
    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
