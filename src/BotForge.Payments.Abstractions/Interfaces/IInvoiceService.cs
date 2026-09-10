namespace BotForge.Payments.Abstractions;

/// <summary>
/// Service for creating invoice links and sending invoices via Telegram.
/// </summary>
public interface IInvoiceService
{
    Task<InvoiceResult> CreateLinkAsync(InvoiceDefinition invoice, string botId, CancellationToken ct = default);

    Task<InvoiceResult> SendInvoiceAsync(long chatId, InvoiceDefinition invoice, string botId,
        CancellationToken ct = default);
}
