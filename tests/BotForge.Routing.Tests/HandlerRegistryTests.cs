using BotForge.Routing.Attributes;
using BotForge.Routing.Registration;

namespace BotForge.Routing.Tests;

public class HandlerRegistryTests
{
    [Fact]
    public void RegisterCommand_And_FindCommand_Works()
    {
        var registry = new HandlerRegistry();
        var registration = new CommandRegistration
        {
            Command = "/start",
            Description = "Start bot",
            HandlerType = typeof(FakeCommandHandler),
            CommandAttribute = new TelegramCommandAttribute("/start", "Start bot")
        };

        registry.RegisterCommand(registration);
        var found = registry.FindCommand("/start");

        Assert.NotNull(found);
        Assert.Equal("/start", found.Command);
        Assert.Equal(typeof(FakeCommandHandler), found.HandlerType);
    }

    [Fact]
    public void FindCommand_WhenNotRegistered_ReturnsNull()
    {
        var registry = new HandlerRegistry();
        Assert.Null(registry.FindCommand("/nonexistent"));
    }

    [Fact]
    public void RegisterCommand_WithDifferentBotKeys_AllowsSameCommand()
    {
        var registry = new HandlerRegistry();
        registry.RegisterCommand(new CommandRegistration
        {
            Command = "/help",
            BotId = "main",
            Description = "Main help",
            HandlerType = typeof(FakeCommandHandler),
            CommandAttribute = new TelegramCommandAttribute("/help", "Main help") { BotId = "main" }
        });
        registry.RegisterCommand(new CommandRegistration
        {
            Command = "/help",
            BotId = "admin",
            Description = "Admin help",
            HandlerType = typeof(FakeCommandHandler),
            CommandAttribute = new TelegramCommandAttribute("/help", "Admin help") { BotId = "admin" }
        });

        var main = registry.FindCommand("/help", "main");
        var admin = registry.FindCommand("/help", "admin");

        Assert.NotNull(main);
        Assert.NotNull(admin);
        Assert.Equal("main", main.BotId);
        Assert.Equal("admin", admin.BotId);
    }

    [Fact]
    public void FindCommand_FallsBackToGlobal_WhenBotScopedMissing()
    {
        var registry = new HandlerRegistry();
        registry.RegisterCommand(new CommandRegistration
        {
            Command = "/start",
            Description = "Global start",
            HandlerType = typeof(FakeCommandHandler),
            CommandAttribute = new TelegramCommandAttribute("/start", "Global start")
        });

        var found = registry.FindCommand("/start", "admin");

        Assert.NotNull(found);
        Assert.Null(found.BotId);
    }

    [Fact]
    public void RegisterCallback_And_FindCallback_Works()
    {
        var registry = new HandlerRegistry();
        var registration = new CallbackRegistration
        {
            Pattern = "menu:{section}",
            HandlerType = typeof(FakeCallbackHandler),
            CallbackAttribute = new CallbackQueryAttribute("menu:{section}", "Menu navigation")
        };

        registry.RegisterCallback(registration);
        var found = registry.FindCallback("menu:settings");

        Assert.NotNull(found);
        Assert.Equal(typeof(FakeCallbackHandler), found.HandlerType);
    }

    [Fact]
    public void GetAllCommands_ReturnsAll()
    {
        var registry = new HandlerRegistry();
        registry.RegisterCommand(new CommandRegistration
        {
            Command = "/start",
            Description = "Start",
            HandlerType = typeof(FakeCommandHandler),
            CommandAttribute = new TelegramCommandAttribute("/start", "Start")
        });
        registry.RegisterCommand(new CommandRegistration
        {
            Command = "/help",
            Description = "Help",
            HandlerType = typeof(FakeCommandHandler),
            CommandAttribute = new TelegramCommandAttribute("/help", "Help")
        });

        var all = registry.GetAllCommands();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void RegisterUsersShared_And_FindUsersShared_Works()
    {
        var registry = new HandlerRegistry();
        registry.RegisterUsersShared(new UsersSharedRegistration { HandlerType = typeof(FakeUsersSharedHandler) });

        var found = registry.FindUsersShared();

        Assert.NotNull(found);
        Assert.Equal(typeof(FakeUsersSharedHandler), found.HandlerType);
    }

    [Fact]
    public void RegisterChatShared_And_FindChatShared_Works()
    {
        var registry = new HandlerRegistry();
        registry.RegisterChatShared(new ChatSharedRegistration { HandlerType = typeof(FakeChatSharedHandler) });

        var found = registry.FindChatShared();

        Assert.NotNull(found);
        Assert.Equal(typeof(FakeChatSharedHandler), found.HandlerType);
    }

    private sealed class FakeCommandHandler : Handlers.ICommandHandler
    {
        public Task<Handlers.CommandResult> HandleAsync(Handlers.CommandContext context, CancellationToken ct)
        {
            return Task.FromResult(Handlers.CommandResult.Ok());
        }
    }

    private sealed class FakeCallbackHandler : Handlers.ICallbackQueryHandler
    {
        public Task<Handlers.CallbackResult> HandleAsync(Handlers.CallbackContext context, CancellationToken ct)
        {
            return Task.FromResult(Handlers.CallbackResult.Ok());
        }
    }

    private sealed class FakeUsersSharedHandler : Handlers.IUsersSharedHandler
    {
        public Task HandleAsync(Handlers.UsersSharedContext context, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeChatSharedHandler : Handlers.IChatSharedHandler
    {
        public Task HandleAsync(Handlers.ChatSharedContext context, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
