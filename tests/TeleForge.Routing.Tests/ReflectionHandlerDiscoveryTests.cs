using Microsoft.Extensions.Logging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Discovery;
using TeleForge.Routing.Handlers;
using TeleForge.Routing.Registration;

namespace TeleForge.Routing.Tests;

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
