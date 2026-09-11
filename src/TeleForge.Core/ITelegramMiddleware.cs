namespace TeleForge.Core;

public delegate Task TelegramUpdateDelegate(TelegramUpdateContext context);

public interface ITelegramMiddleware
{
    Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next);
}
