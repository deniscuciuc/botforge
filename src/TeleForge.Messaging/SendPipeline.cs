using Microsoft.Extensions.DependencyInjection;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;

namespace TeleForge.Messaging;

public class SendPipeline : ISendPipeline
{
    private readonly TelegramSendDelegate _pipeline;

    public SendPipeline(IServiceProvider serviceProvider, TelegramMessagingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Build pipeline: middleware[0] → middleware[1] → ... → TelegramApiTransport
        var transport = serviceProvider.GetRequiredService<TelegramApiTransport>();
        TelegramSendDelegate pipeline = context => transport.InvokeAsync(context, _ =>
            Task.FromResult(SendResult.Failed("No transport configured")));

        // Wrap in reverse order so first registered middleware executes first
        for (var i = options.SendMiddleware.Count - 1; i >= 0; i--)
        {
            var middlewareType = options.SendMiddleware[i];
            var next = pipeline;

            pipeline = context =>
            {
                var middleware = (ISendMiddleware)serviceProvider.GetRequiredService(middlewareType);
                return middleware.InvokeAsync(context, next);
            };
        }

        _pipeline = pipeline;
    }

    public Task<SendResult> SendAsync(SendContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CancellationToken = ct;
        return _pipeline(context);
    }
}
