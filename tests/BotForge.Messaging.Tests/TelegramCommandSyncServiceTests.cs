using BotForge.Core;
using BotForge.Messaging.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace BotForge.Messaging.Tests;

public class TelegramCommandSyncServiceTests
{
    [Fact]
    public async Task SynchronizeAsync_SetsCommands_ForNonEmptySet()
    {
        var clientProvider = Substitute.For<ITelegramBotClientProvider>();
        var client = Substitute.For<ITelegramBotClient>();
        clientProvider.GetClient("main").Returns(client);

        var sut = new TelegramCommandSyncService(clientProvider, NullLogger<TelegramCommandSyncService>.Instance);
        var commandSets = new[]
        {
            new TelegramCommandSet(
                new BotCommandScopeDefault(),
                "en",
                [new BotCommand { Command = "start", Description = "Start" }])
        };

        await sut.SynchronizeAsync("main", commandSets, CancellationToken.None);

        await client.Received(1).SendRequest(
            Arg.Is<SetMyCommandsRequest>(request =>
                request.Scope != null && request.Scope.GetType() == typeof(BotCommandScopeDefault)
                && request.LanguageCode == "en"
                && request.Commands.Single().Command == "start"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SynchronizeAsync_DeletesCommands_ForEmptySet()
    {
        var clientProvider = Substitute.For<ITelegramBotClientProvider>();
        var client = Substitute.For<ITelegramBotClient>();
        clientProvider.GetClient("admin").Returns(client);

        var sut = new TelegramCommandSyncService(clientProvider, NullLogger<TelegramCommandSyncService>.Instance);
        var commandSets = new[]
        {
            new TelegramCommandSet(
                new BotCommandScopeDefault(),
                null,
                [])
        };

        await sut.SynchronizeAsync("admin", commandSets, CancellationToken.None);

        await client.Received(1).SendRequest(
            Arg.Is<DeleteMyCommandsRequest>(request =>
                request.Scope != null && request.Scope.GetType() == typeof(BotCommandScopeDefault)
                && request.LanguageCode == null),
            Arg.Any<CancellationToken>());
    }
}
