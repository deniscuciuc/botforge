using BotForge.Core;
using Telegram.Bot.Types;

namespace BotForge.Routing.Handlers;

public interface IContactHandler
{
    Task HandleAsync(ContactContext context, CancellationToken ct);
}

public class ContactContext(TelegramUpdateContext updateContext, Contact contact)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public Contact Contact { get; } = contact;
    public string PhoneNumber => Contact.PhoneNumber;
    public string? FirstName => Contact.FirstName;
    public long? ContactUserId => Contact.UserId;
    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
