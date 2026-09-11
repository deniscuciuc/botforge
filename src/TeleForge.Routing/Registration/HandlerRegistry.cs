using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TeleForge.Routing.Handlers;

namespace TeleForge.Routing.Registration;

public class HandlerRegistry : IHandlerRegistry
{
    private readonly ConcurrentDictionary<string, CommandRegistration>
        _commands = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<CallbackRegistration> _callbacks = [];
    private readonly List<TextMessageRegistration> _textHandlers = [];
    private readonly List<InlineQueryRegistration> _inlineQueries = [];
    private readonly List<ChosenInlineResultRegistration> _chosenInlineResults = [];
    private readonly List<MediaRegistration> _mediaHandlers = [];
    private readonly List<LocationRegistration> _locationHandlers = [];
    private readonly List<ContactRegistration> _contactHandlers = [];
    private readonly List<UsersSharedRegistration> _usersSharedHandlers = [];
    private readonly List<ChatSharedRegistration> _chatSharedHandlers = [];
    private readonly List<PollAnswerRegistration> _pollAnswerHandlers = [];
    private readonly List<ChatMemberRegistration> _chatMemberHandlers = [];
    private readonly List<DiceRegistration> _diceHandlers = [];
    private readonly object _callbackLock = new();
    private readonly object _textLock = new();
    private readonly object _miscLock = new();

    public void RegisterCommand(CommandRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var normalizedCommand = NormalizeCommand(registration.Command);
        var normalizedBotKey = NormalizeBotKey(registration.BotId);
        var commandKey = BuildCommandKey(normalizedCommand, normalizedBotKey);
        var normalizedRegistration = new CommandRegistration
        {
            Command = normalizedCommand,
            BotId = normalizedBotKey,
            Description = registration.Description,
            HandlerType = registration.HandlerType,
            CommandAttribute = registration.CommandAttribute,
            CommandScopeAttributes = registration.CommandScopeAttributes,
            AuthorizeAttributes = registration.AuthorizeAttributes,
            RateLimitAttribute = registration.RateLimitAttribute,
            ChatTypeAttribute = registration.ChatTypeAttribute
        };

        if (!_commands.TryAdd(commandKey, normalizedRegistration))
        {
            var scope = normalizedBotKey ?? "global";
            throw new InvalidOperationException(
                $"Command '{normalizedCommand}' is already registered for bot scope '{scope}'.");
        }
    }

    public void RegisterCallback(CallbackRegistration registration)
    {
        lock (_callbackLock)
        {
            _callbacks.Add(registration);
        }
    }

    public CommandRegistration? FindCommand(string command, string? botId = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = NormalizeCommand(command);
        var normalizedBotKey = NormalizeBotKey(botId);

        if (normalizedBotKey != null)
        {
            var scopedKey = BuildCommandKey(normalized, normalizedBotKey);
            if (_commands.TryGetValue(scopedKey, out var scopedRegistration))
                return scopedRegistration;
        }

        var globalKey = BuildCommandKey(normalized, null);
        return _commands.GetValueOrDefault(globalKey);
    }

    public CallbackRegistration? FindCallback(string callbackData)
    {
        ArgumentNullException.ThrowIfNull(callbackData);

        lock (_callbackLock)
        {
            foreach (var reg in _callbacks)
                if (reg.IsRegex)
                {
                    if (Regex.IsMatch(callbackData, reg.Pattern))
                        return reg;
                }
                else
                {
                    if (MatchPattern(callbackData, reg.Pattern))
                        return reg;
                }
        }

        return null;
    }

    public IReadOnlyList<CommandRegistration> GetAllCommands(string? botId = null)
    {
        var normalizedBotKey = NormalizeBotKey(botId);
        if (normalizedBotKey == null)
            return _commands.Values.ToList();

        return _commands.Values
            .Where(x => x.BotId == normalizedBotKey || x.BotId == null)
            .ToList();
    }

    public IReadOnlyList<CallbackRegistration> GetAllCallbacks()
    {
        lock (_callbackLock)
        {
            return _callbacks.ToList();
        }
    }

    public void RegisterTextHandler(TextMessageRegistration registration)
    {
        lock (_textLock)
        {
            _textHandlers.Add(registration);
            _textHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }

    public TextMessageRegistration? FindTextHandler(string text)
    {
        lock (_textLock)
        {
            foreach (var reg in _textHandlers)
            {
                if (reg.Pattern == null)
                    return reg; // Catch-all handler

                if (Regex.IsMatch(text, reg.Pattern))
                    return reg;
            }
        }

        return null;
    }

    public IReadOnlyList<TextMessageRegistration> GetAllTextHandlers()
    {
        lock (_textLock)
        {
            return _textHandlers.ToList();
        }
    }

    public void RegisterInlineQuery(InlineQueryRegistration registration)
    {
        lock (_miscLock)
        {
            _inlineQueries.Add(registration);
        }
    }

    public InlineQueryRegistration? FindInlineQuery(string query)
    {
        lock (_miscLock)
        {
            foreach (var reg in _inlineQueries)
                if (reg.Pattern == null || Regex.IsMatch(query, reg.Pattern))
                    return reg;
        }

        return null;
    }

    public void RegisterChosenInlineResult(ChosenInlineResultRegistration registration)
    {
        lock (_miscLock)
        {
            _chosenInlineResults.Add(registration);
        }
    }

    public ChosenInlineResultRegistration? FindChosenInlineResult()
    {
        lock (_miscLock)
        {
            return _chosenInlineResults.FirstOrDefault();
        }
    }

    public void RegisterMedia(MediaRegistration registration)
    {
        lock (_miscLock)
        {
            _mediaHandlers.Add(registration);
        }
    }

    public MediaRegistration? FindMedia(MediaType mediaType)
    {
        lock (_miscLock)
        {
            return _mediaHandlers.FirstOrDefault(r => r.MediaType == mediaType);
        }
    }

    public void RegisterLocation(LocationRegistration registration)
    {
        lock (_miscLock)
        {
            _locationHandlers.Add(registration);
        }
    }

    public LocationRegistration? FindLocation()
    {
        lock (_miscLock)
        {
            return _locationHandlers.FirstOrDefault();
        }
    }

    public void RegisterContact(ContactRegistration registration)
    {
        lock (_miscLock)
        {
            _contactHandlers.Add(registration);
        }
    }

    public ContactRegistration? FindContact()
    {
        lock (_miscLock)
        {
            return _contactHandlers.FirstOrDefault();
        }
    }

    public void RegisterUsersShared(UsersSharedRegistration registration)
    {
        lock (_miscLock)
        {
            _usersSharedHandlers.Add(registration);
        }
    }

    public UsersSharedRegistration? FindUsersShared()
    {
        lock (_miscLock)
        {
            return _usersSharedHandlers.FirstOrDefault();
        }
    }

    public void RegisterChatShared(ChatSharedRegistration registration)
    {
        lock (_miscLock)
        {
            _chatSharedHandlers.Add(registration);
        }
    }

    public ChatSharedRegistration? FindChatShared()
    {
        lock (_miscLock)
        {
            return _chatSharedHandlers.FirstOrDefault();
        }
    }

    public void RegisterPollAnswer(PollAnswerRegistration registration)
    {
        lock (_miscLock)
        {
            _pollAnswerHandlers.Add(registration);
        }
    }

    public PollAnswerRegistration? FindPollAnswer()
    {
        lock (_miscLock)
        {
            return _pollAnswerHandlers.FirstOrDefault();
        }
    }

    public void RegisterChatMember(ChatMemberRegistration registration)
    {
        lock (_miscLock)
        {
            _chatMemberHandlers.Add(registration);
        }
    }

    public ChatMemberRegistration? FindChatMember(bool isMyChatMember)
    {
        lock (_miscLock)
        {
            return _chatMemberHandlers.FirstOrDefault(r => r.MyChatMemberOnly == isMyChatMember)
                   ?? _chatMemberHandlers.FirstOrDefault(r => !r.MyChatMemberOnly);
        }
    }

    public void RegisterDice(DiceRegistration registration)
    {
        lock (_miscLock)
        {
            _diceHandlers.Add(registration);
        }
    }

    public DiceRegistration? FindDice(string emoji)
    {
        lock (_miscLock)
        {
            return _diceHandlers.FirstOrDefault(r => r.Emoji == emoji)
                   ?? _diceHandlers.FirstOrDefault(r => r.Emoji == null);
        }
    }

    private readonly List<GiftMessageRegistration> _giftHandlers = [];
    private readonly List<UniqueGiftMessageRegistration> _uniqueGiftHandlers = [];

    public void RegisterGiftMessage(GiftMessageRegistration registration)
    {
        lock (_miscLock)
        {
            _giftHandlers.Add(registration);
        }
    }

    public void RegisterUniqueGiftMessage(UniqueGiftMessageRegistration registration)
    {
        lock (_miscLock)
        {
            _uniqueGiftHandlers.Add(registration);
        }
    }

    public GiftMessageRegistration? FindGiftMessage()
    {
        lock (_miscLock)
        {
            return _giftHandlers.FirstOrDefault();
        }
    }

    public UniqueGiftMessageRegistration? FindUniqueGiftMessage()
    {
        lock (_miscLock)
        {
            return _uniqueGiftHandlers.FirstOrDefault();
        }
    }

    private static string NormalizeCommand(string command)
    {
        // Strip bot mention suffix: "/start@MyBot" → "/start"
        var atIndex = command.IndexOf('@');
        return atIndex > 0 ? command[..atIndex] : command;
    }

    private static string? NormalizeBotKey(string? botId)
    {
        if (string.IsNullOrWhiteSpace(botId))
            return null;

        return botId.Trim();
    }

    private static string BuildCommandKey(string command, string? botId)
    {
        return botId == null ? $"{command}||*" : $"{command}||{botId}";
    }

    /// <summary>
    /// Matches callback data against a route pattern with typed parameters.
    /// Pattern: "shop:pack:{packId:int}" matches "shop:pack:42".
    /// </summary>
    private static bool MatchPattern(string data, string pattern)
    {
        var dataParts = data.Split(':');
        var patternParts = pattern.Split(':');

        if (dataParts.Length != patternParts.Length)
            return false;

        for (var i = 0; i < patternParts.Length; i++)
        {
            var pp = patternParts[i];
            if (pp.StartsWith('{') && pp.EndsWith('}'))
                continue; // Parameter placeholder — any value matches

            if (!string.Equals(dataParts[i], pp, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
