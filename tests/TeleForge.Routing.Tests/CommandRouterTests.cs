using Microsoft.Extensions.Logging.Abstractions;
using TeleForge.Core;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;
using TeleForge.Routing.Registration;
using TeleForge.Routing.Routing;
using TeleForge.TestUtilities;

namespace TeleForge.Routing.Tests;

public class CommandRouterTests
{
    private static readonly IServiceProvider EmptyServices = new EmptyServiceProvider();

    [Fact]
    public async Task RouteAsync_PrefersLongestTemplateMatch_ForCommandRootVariants()
    {
        TenantRootHandler.Reset();
        TenantBySlugHandler.Reset();

        var registry = new HandlerRegistry();
        RegisterCommand<TenantRootHandler>(registry, "/tenant");
        RegisterCommand<TenantBySlugHandler>(registry, "/tenant {slug}");

        var context = CreateContext("/tenant acme");
        var router = new CommandRouter(registry, NullLogger<CommandRouter>.Instance);

        await router.RouteAsync(context);

        Assert.Equal(0, TenantRootHandler.Invocations);
        Assert.Equal(1, TenantBySlugHandler.Invocations);
        Assert.Equal(["acme"], TenantBySlugHandler.LastArguments);
        Assert.Equal("/tenant {slug}", TenantBySlugHandler.LastCommand);
    }

    [Fact]
    public async Task RouteAsync_KeepsParameterOrdering_WhenTemplateContainsEmbeddedSegments()
    {
        EmbeddedTemplateHandler.Reset();

        var registry = new HandlerRegistry();
        RegisterCommand<EmbeddedTemplateHandler>(registry, "/credits add {slug} {amount}");

        var context = CreateContext("/credits add store-1 50 urgent");
        var router = new CommandRouter(registry, NullLogger<CommandRouter>.Instance);

        await router.RouteAsync(context);

        Assert.Equal(1, EmbeddedTemplateHandler.Invocations);
        Assert.Equal(["store-1", "50", "urgent"], EmbeddedTemplateHandler.LastArguments);
    }

    [Fact]
    public async Task RouteAsync_BlocksWhenRequiredArgumentsNotSatisfied_AfterTemplateCapture()
    {
        RequiredArgsHandler.Reset();

        var registry = new HandlerRegistry();
        RegisterCommand<RequiredArgsHandler>(registry, "/tenant {slug}", requiredArguments: 2);

        var context = CreateContext("/tenant acme");
        var router = new CommandRouter(registry, NullLogger<CommandRouter>.Instance);

        await router.RouteAsync(context);

        Assert.Equal(0, RequiredArgsHandler.Invocations);
        Assert.False(context.Result!.Success);
        Assert.Contains("requires at least 2 argument(s)", context.Result.Reason);
    }

    [Fact]
    public async Task RouteAsync_NormalizesBotMention_ForMultiTokenAndSingleTokenCommands()
    {
        MultiTokenMentionHandler.Reset();
        SingleTokenMentionHandler.Reset();

        var registry = new HandlerRegistry();
        RegisterCommand<MultiTokenMentionHandler>(registry, "/prompts reload");
        RegisterCommand<SingleTokenMentionHandler>(registry, "/start");

        var router = new CommandRouter(registry, NullLogger<CommandRouter>.Instance);

        var multiContext = CreateContext("/prompts@SampleBot reload");
        await router.RouteAsync(multiContext);

        var singleContext = CreateContext("/start@SampleBot deep-link");
        await router.RouteAsync(singleContext);

        Assert.Equal(1, MultiTokenMentionHandler.Invocations);
        Assert.Equal([], MultiTokenMentionHandler.LastArguments);
        Assert.Equal(1, SingleTokenMentionHandler.Invocations);
        Assert.Equal(["deep-link"], SingleTokenMentionHandler.LastArguments);
        Assert.Equal("/start", SingleTokenMentionHandler.LastCommand);
    }

    [Fact]
    public async Task RouteAsync_PrefersBotScopedHandler_WhenPatternIsSameAsGlobal()
    {
        GlobalHelpHandler.Reset();
        BotScopedHelpHandler.Reset();

        var registry = new HandlerRegistry();
        RegisterCommand<GlobalHelpHandler>(registry, "/help");
        RegisterCommand<BotScopedHelpHandler>(registry, "/help", botId: "admin-bot");

        var context = CreateContext("/help", "admin-bot");
        var router = new CommandRouter(registry, NullLogger<CommandRouter>.Instance);

        await router.RouteAsync(context);

        Assert.Equal(0, GlobalHelpHandler.Invocations);
        Assert.Equal(1, BotScopedHelpHandler.Invocations);
    }

    private static TelegramUpdateContext CreateContext(string commandText, string botId = "test-bot")
    {
        var update = UpdateFactory.CreateMessageUpdate(10, 20, commandText);
        return new TelegramUpdateContext(update, botId)
        {
            RequestServices = EmptyServices
        };
    }

    private static void RegisterCommand<THandler>(
        HandlerRegistry registry,
        string command,
        int requiredArguments = 0,
        string? botId = null)
        where THandler : class, ICommandHandler
    {
        registry.RegisterCommand(new CommandRegistration
        {
            Command = command,
            BotId = botId,
            Description = command,
            HandlerType = typeof(THandler),
            CommandAttribute = new TelegramCommandAttribute(command) { RequiredArguments = requiredArguments }
        });
    }

    private sealed class TenantRootHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }

        public static void Reset()
        {
            Invocations = 0;
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class TenantBySlugHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }
        public static string[] LastArguments { get; private set; } = [];
        public static string LastCommand { get; private set; } = string.Empty;

        public static void Reset()
        {
            Invocations = 0;
            LastArguments = [];
            LastCommand = string.Empty;
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            LastArguments = context.Arguments;
            LastCommand = context.Command;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class EmbeddedTemplateHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }
        public static string[] LastArguments { get; private set; } = [];

        public static void Reset()
        {
            Invocations = 0;
            LastArguments = [];
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            LastArguments = context.Arguments;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class RequiredArgsHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }

        public static void Reset()
        {
            Invocations = 0;
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class MultiTokenMentionHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }
        public static string[] LastArguments { get; private set; } = [];

        public static void Reset()
        {
            Invocations = 0;
            LastArguments = [];
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            LastArguments = context.Arguments;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class SingleTokenMentionHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }
        public static string[] LastArguments { get; private set; } = [];
        public static string LastCommand { get; private set; } = string.Empty;

        public static void Reset()
        {
            Invocations = 0;
            LastArguments = [];
            LastCommand = string.Empty;
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            LastArguments = context.Arguments;
            LastCommand = context.Command;
            return Task.FromResult(CommandResult.Ok());
        }

    }

    private sealed class GlobalHelpHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }

        public static void Reset()
        {
            Invocations = 0;
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class BotScopedHelpHandler : ICommandHandler
    {
        public static int Invocations { get; private set; }

        public static void Reset()
        {
            Invocations = 0;
        }

        public Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
        {
            Invocations++;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
