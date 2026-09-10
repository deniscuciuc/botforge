using Telegram.Bot.Types;

namespace BotForge.Core;

public class TelegramUpdateContext(Update rawUpdate, string botId)
{
    private readonly Dictionary<Type, object> _features = new();

    public Update RawUpdate { get; } = rawUpdate;
    public string BotId { get; } = botId;
    public string UpdateType { get; } = ResolveUpdateType(rawUpdate);
    public long? ChatId { get; } = ResolveChatId(rawUpdate);
    public int? ThreadId { get; } = ResolveThreadId(rawUpdate);
    public long? UserId { get; } = ResolveUserId(rawUpdate);

    public string? Language { get; set; }
    public string? UserRank { get; set; }
    public UpdateResult? Result { get; set; }

    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
    public CancellationToken CancellationToken { get; set; }
    public IServiceProvider RequestServices { get; set; } = null!;

    public T GetFeature<T>() where T : class
    {
        return _features.TryGetValue(typeof(T), out var feature)
            ? (T)feature
            : throw new InvalidOperationException($"Feature {typeof(T).Name} not found in context.");
    }

    public T? GetFeatureOrDefault<T>() where T : class
    {
        return _features.TryGetValue(typeof(T), out var feature) ? (T)feature : null;
    }

    public void SetFeature<T>(T feature) where T : class
    {
        _features[typeof(T)] = feature;
    }

    private static string ResolveUpdateType(Update update)
    {
        if (update.Message != null) return "Message";
        if (update.CallbackQuery != null) return "CallbackQuery";
        if (update.InlineQuery != null) return "InlineQuery";
        if (update.ChosenInlineResult != null) return "ChosenInlineResult";
        if (update.EditedMessage != null) return "EditedMessage";
        if (update.ChannelPost != null) return "ChannelPost";
        if (update.EditedChannelPost != null) return "EditedChannelPost";
        if (update.PreCheckoutQuery != null) return "PreCheckoutQuery";
        if (update.ShippingQuery != null) return "ShippingQuery";
        if (update.Poll != null) return "Poll";
        if (update.PollAnswer != null) return "PollAnswer";
        if (update.MyChatMember != null) return "MyChatMember";
        if (update.ChatMember != null) return "ChatMember";
        return update.ChatJoinRequest != null
            ? "ChatJoinRequest"
            : "Unknown";
    }

    private static long? ResolveChatId(Update update)
    {
        if (update.Message != null) return update.Message.Chat.Id;
        if (update.CallbackQuery?.Message != null) return update.CallbackQuery.Message.Chat.Id;
        if (update.EditedMessage != null) return update.EditedMessage.Chat.Id;
        if (update.ChannelPost != null) return update.ChannelPost.Chat.Id;
        if (update.EditedChannelPost != null) return update.EditedChannelPost.Chat.Id;
        if (update.MyChatMember != null) return update.MyChatMember.Chat.Id;
        return update.ChatMember != null
            ? update.ChatMember.Chat.Id
            : update.ChatJoinRequest?.Chat.Id;
    }

    private static long? ResolveUserId(Update update)
    {
        if (update.Message != null) return update.Message.From?.Id;
        if (update.CallbackQuery != null) return update.CallbackQuery.From.Id;
        if (update.InlineQuery != null) return update.InlineQuery.From.Id;
        if (update.ChosenInlineResult != null) return update.ChosenInlineResult.From.Id;
        if (update.EditedMessage != null) return update.EditedMessage.From?.Id;
        if (update.PreCheckoutQuery != null) return update.PreCheckoutQuery.From.Id;
        if (update.ShippingQuery != null) return update.ShippingQuery.From.Id;
        if (update.PollAnswer != null) return update.PollAnswer.User?.Id;
        if (update.MyChatMember != null) return update.MyChatMember.From.Id;
        return update.ChatMember != null
            ? update.ChatMember.From.Id
            : update.ChatJoinRequest?.From.Id;
    }

    private static int? ResolveThreadId(Update update)
    {
        return update.Message?.MessageThreadId
               ?? update.CallbackQuery?.Message?.MessageThreadId
               ?? update.EditedMessage?.MessageThreadId;
    }
}

public class UpdateResult
{
    public bool Success { get; init; }
    public string? Reason { get; init; }
    public TimeSpan? RetryAfter { get; init; }

    public static UpdateResult Ok()
    {
        return new UpdateResult { Success = true };
    }

    public static UpdateResult Blocked(string reason)
    {
        return new UpdateResult { Success = false, Reason = reason };
    }

    public static UpdateResult RetryLater(TimeSpan retryAfter)
    {
        return new UpdateResult { Success = false, Reason = "Rate limited", RetryAfter = retryAfter };
    }
}
