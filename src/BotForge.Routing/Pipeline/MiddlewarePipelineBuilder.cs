using BotForge.Core;

namespace BotForge.Routing.Pipeline;

public class MiddlewarePipelineBuilder
{
    private readonly List<Func<TelegramUpdateDelegate, TelegramUpdateDelegate>> _components = [];

    public MiddlewarePipelineBuilder Use(Func<TelegramUpdateDelegate, TelegramUpdateDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    public MiddlewarePipelineBuilder UseMiddleware(ITelegramMiddleware middleware)
    {
        _components.Add(next => context => middleware.InvokeAsync(context, next));
        return this;
    }

    public TelegramUpdateDelegate Build(TelegramUpdateDelegate terminalHandler)
    {
        var pipeline = terminalHandler;

        // Build in reverse order so first-added middleware runs first
        for (var i = _components.Count - 1; i >= 0; i--) pipeline = _components[i](pipeline);

        return pipeline;
    }
}
