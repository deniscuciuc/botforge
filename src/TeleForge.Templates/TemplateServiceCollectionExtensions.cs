using Microsoft.Extensions.DependencyInjection;
using TeleForge.Messaging.Abstractions;

namespace TeleForge.Templates;

public static class TemplateServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramTemplates(
        this IServiceCollection services,
        Action<TelegramTemplateOptions>? configure = null)
    {
        var options = new TelegramTemplateOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IEmojiRegistry, EmojiRegistry>();
        services.AddSingleton<IMessageTemplateStore, InMemoryMessageTemplateStore>();
        services.AddSingleton<ITemplateRenderer, TemplateRenderer>();
        services.AddSingleton<IKeyboardBuilder, KeyboardBuilder>();
        services.AddSingleton<IMessageRenderer, TemplateMessageRenderer>();
        services.AddSingleton<ConditionalEvaluator>();
        services.AddSingleton<LoopProcessor>();
        services.AddSingleton<PartialResolver>();
        services.AddSingleton<FormatterPipeline>();
        services.AddSingleton<YamlTemplateLoader>();

        foreach (var formatterRegistration in options.Formatters)
            services.AddSingleton(typeof(ITemplateFormatter), formatterRegistration);

        return services;
    }
}
