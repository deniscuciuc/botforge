using Microsoft.Extensions.DependencyInjection;
using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.Router;

/// <summary>
/// Registry for payment handler types keyed by payload prefix.
/// Used by the checkout and refund pipelines to resolve handlers at runtime.
/// </summary>
public class PaymentHandlerRegistry
{
    private readonly Dictionary<string, List<Type>> _validators = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Type>> _processors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Type>> _refundProcessors = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterValidator(string prefix, Type validatorType)
    {
        if (!_validators.TryGetValue(prefix, out var list))
        {
            list = [];
            _validators[prefix] = list;
        }

        list.Add(validatorType);
    }

    public void RegisterProcessor(string prefix, Type processorType)
    {
        if (!_processors.TryGetValue(prefix, out var list))
        {
            list = [];
            _processors[prefix] = list;
        }

        list.Add(processorType);
    }

    public void RegisterRefundProcessor(string prefix, Type processorType)
    {
        if (!_refundProcessors.TryGetValue(prefix, out var list))
        {
            list = [];
            _refundProcessors[prefix] = list;
        }

        list.Add(processorType);
    }

    public IReadOnlyList<IPreCheckoutValidator> GetValidators(string prefix, IServiceProvider services)
    {
        return !_validators.TryGetValue(prefix, out var types)
            ? []
            : types.Select(t => (IPreCheckoutValidator)services.GetRequiredService(t)).ToList();
    }

    public IReadOnlyList<IPaymentProcessor> GetProcessors(string prefix, IServiceProvider services)
    {
        return !_processors.TryGetValue(prefix, out var types)
            ? []
            : types.Select(t => (IPaymentProcessor)services.GetRequiredService(t)).ToList();
    }

    public IReadOnlyList<IRefundProcessor> GetRefundProcessors(string prefix, IServiceProvider services)
    {
        return !_refundProcessors.TryGetValue(prefix, out var types)
            ? []
            : types.Select(t => (IRefundProcessor)services.GetRequiredService(t)).ToList();
    }

    public bool HasHandlers(string prefix)
    {
        return _validators.ContainsKey(prefix) || _processors.ContainsKey(prefix) ||
               _refundProcessors.ContainsKey(prefix);
    }
}
