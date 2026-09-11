using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeleForge.Consumer.Extensions;
using TeleForge.Messaging;
using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Extensions;
using TeleForge.Routing.Handlers;

namespace TeleForge.Docs.Snippets;

/// <summary>README — the quick-start host.</summary>
internal static class QuickStart
{
    internal static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTelegramMessaging(messaging =>
            messaging.AddBot("main", bot => bot.Token = builder.Configuration["Telegram:BotToken"]!));

        builder.Services.AddTelegramRouting(routing =>
            routing.AddHandlersFromAssembly(typeof(QuickStart).Assembly));

        builder.Services.AddTelegramConsumer(consumer => consumer.ConcurrencyLimit = 10);

        await builder.Build().RunAsync();
    }
}

/// <summary>README — a handler is a class with an attribute.</summary>
[TelegramCommand("/start")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("Hi! Send me anything and I'll echo it back.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
