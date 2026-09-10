using BotForge.Routing.Attributes;
using BotForge.Routing.Discovery;
using BotForge.Routing.Handlers;
using BotForge.Routing.Registration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BotForge.Routing.Tests;

public class ReflectionHandlerDiscoveryTests
{
    [Fact]
    public void DiscoverAndRegister_RegistersUsersSharedAndChatSharedHandlers()
    {
        var registry = new HandlerRegistry();
        var discovery = new ReflectionHandlerDiscovery(NullLogger<ReflectionHandlerDiscovery>.Instance);

        discovery.DiscoverAndRegister(registry, [typeof(ReflectionUsersSharedHandler).Assembly]);

        Assert.Equal(typeof(ReflectionUsersSharedHandler), registry.FindUsersShared()?.HandlerType);
        Assert.Equal(typeof(ReflectionChatSharedHandler), registry.FindChatShared()?.HandlerType);
    }

    [UsersSharedMessage]
    private sealed class ReflectionUsersSharedHandler : IUsersSharedHandler
    {
        public Task HandleAsync(UsersSharedContext context, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    [ChatSharedMessage]
    private sealed class ReflectionChatSharedHandler : IChatSharedHandler
    {
        public Task HandleAsync(ChatSharedContext context, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
