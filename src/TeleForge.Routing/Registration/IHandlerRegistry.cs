using TeleForge.Routing.Handlers;

namespace TeleForge.Routing.Registration;

public interface IHandlerRegistry
{
    void RegisterCommand(CommandRegistration registration);
    void RegisterCallback(CallbackRegistration registration);
    void RegisterTextHandler(TextMessageRegistration registration);
    void RegisterInlineQuery(InlineQueryRegistration registration);
    void RegisterChosenInlineResult(ChosenInlineResultRegistration registration);
    void RegisterMedia(MediaRegistration registration);
    void RegisterLocation(LocationRegistration registration);
    void RegisterContact(ContactRegistration registration);
    void RegisterUsersShared(UsersSharedRegistration registration);
    void RegisterChatShared(ChatSharedRegistration registration);
    void RegisterPollAnswer(PollAnswerRegistration registration);
    void RegisterChatMember(ChatMemberRegistration registration);
    void RegisterDice(DiceRegistration registration);
    void RegisterGiftMessage(GiftMessageRegistration registration);
    void RegisterUniqueGiftMessage(UniqueGiftMessageRegistration registration);

    CommandRegistration? FindCommand(string command, string? botId = null);
    CallbackRegistration? FindCallback(string callbackData);
    TextMessageRegistration? FindTextHandler(string text);
    InlineQueryRegistration? FindInlineQuery(string query);
    ChosenInlineResultRegistration? FindChosenInlineResult();
    MediaRegistration? FindMedia(MediaType mediaType);
    LocationRegistration? FindLocation();
    ContactRegistration? FindContact();
    UsersSharedRegistration? FindUsersShared();
    ChatSharedRegistration? FindChatShared();
    PollAnswerRegistration? FindPollAnswer();
    ChatMemberRegistration? FindChatMember(bool isMyChatMember);
    DiceRegistration? FindDice(string emoji);
    GiftMessageRegistration? FindGiftMessage();
    UniqueGiftMessageRegistration? FindUniqueGiftMessage();

    IReadOnlyList<CommandRegistration> GetAllCommands(string? botId = null);
    IReadOnlyList<CallbackRegistration> GetAllCallbacks();
    IReadOnlyList<TextMessageRegistration> GetAllTextHandlers();
}
