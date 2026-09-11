namespace TeleForge.Core;

public interface ITelegramUpdatePipeline
{
    Task ProcessAsync(TelegramUpdateContext context, CancellationToken ct = default);
}
