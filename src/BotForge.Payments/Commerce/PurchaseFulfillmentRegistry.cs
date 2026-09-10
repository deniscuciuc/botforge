using BotForge.Payments.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BotForge.Payments.Commerce;

/// <summary>
/// Registry of <see cref="IPurchaseFulfillmentHandler"/> types keyed by application payload prefix.
/// Works alongside the existing <see cref="Router.PaymentHandlerRegistry"/> (which covers Telegram invoice
/// pre-checkout / success events) to handle settlement notifications from gift and wallet rails.
/// </summary>
public class PurchaseFulfillmentRegistry
{
    private readonly Dictionary<string, List<Type>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string payloadPrefix, Type handlerType)
    {
        GetOrCreate(payloadPrefix).Add(handlerType);
    }

    public IReadOnlyList<IPurchaseFulfillmentHandler> GetHandlers(string payloadPrefix, IServiceProvider services)
    {
        if (!_handlers.TryGetValue(payloadPrefix, out var types))
            return [];

        return types.Select(t => (IPurchaseFulfillmentHandler)services.GetRequiredService(t)).ToList();
    }

    public bool HasHandlers(string payloadPrefix)
    {
        return _handlers.ContainsKey(payloadPrefix);
    }

    private List<Type> GetOrCreate(string prefix)
    {
        if (!_handlers.TryGetValue(prefix, out var list))
        {
            list = [];
            _handlers[prefix] = list;
        }

        return list;
    }
}
