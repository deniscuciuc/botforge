namespace BotForge.Routing.Routing;

internal static class CommandTextParser
{
    public static bool TryParse(string? text, out string command, out string[] arguments)
    {
        command = string.Empty;
        arguments = [];

        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/'))
            return false;

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        command = Normalize(parts[0]);
        if (string.IsNullOrWhiteSpace(command) || !command.StartsWith('/'))
            return false;

        arguments = parts.Length > 1 ? parts[1..] : [];
        return true;
    }

    public static string Normalize(string commandToken)
    {
        if (string.IsNullOrWhiteSpace(commandToken) || !commandToken.StartsWith('/'))
            return commandToken;

        var atIndex = commandToken.IndexOf('@');
        return atIndex > 1 ? commandToken[..atIndex] : commandToken;
    }
}
