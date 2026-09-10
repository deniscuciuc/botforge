namespace BotForge.Routing.Handlers;

public class CommandResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static CommandResult Ok()
    {
        return new CommandResult { Success = true };
    }

    public static CommandResult Fail(string error)
    {
        return new CommandResult { Success = false, ErrorMessage = error };
    }
}

public class CallbackResult
{
    public bool Success { get; init; }
    public string? AlertText { get; init; }
    public bool ShowAlert { get; init; }
    public string? ErrorMessage { get; init; }

    public static CallbackResult Ok()
    {
        return new CallbackResult { Success = true };
    }

    public static CallbackResult Alert(string text, bool showAlert = true)
    {
        return new CallbackResult { Success = true, AlertText = text, ShowAlert = showAlert };
    }

    public static CallbackResult Fail(string error)
    {
        return new CallbackResult { Success = false, ErrorMessage = error };
    }
}
