using BotForge.Core;
using BotForge.Payments.Abstractions;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Payments;

namespace BotForge.Payments.Invoice;

public class InvoiceService(
    ITelegramBotClientProvider botProvider,
    IPaymentMetrics metrics,
    ILogger<InvoiceService> logger)
    : IInvoiceService
{
    public async Task<InvoiceResult> CreateLinkAsync(InvoiceDefinition invoice, string botId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        try
        {
            var client = botProvider.GetClient(botId);
            var prices = invoice.Prices.Select(p => new LabeledPrice { Label = p.Label, Amount = p.Amount }).ToArray();

            var link = await client.CreateInvoiceLink(
                invoice.Title,
                invoice.Description,
                invoice.Payload,
                CurrencyToString(invoice.Currency),
                prices,
                NullIfEmpty(invoice.ProviderToken),
                maxTipAmount: invoice.MaxTipAmount,
                suggestedTipAmounts: invoice.SuggestedTipAmounts,
                providerData: invoice.ProviderData,
                photoUrl: invoice.PhotoUrl,
                photoSize: null,
                photoWidth: invoice.PhotoWidth,
                photoHeight: invoice.PhotoHeight,
                needName: invoice.NeedName,
                needPhoneNumber: invoice.NeedPhoneNumber,
                needEmail: invoice.NeedEmail,
                needShippingAddress: invoice.NeedShippingAddress,
                sendPhoneNumberToProvider: invoice.SendPhoneNumberToProvider,
                sendEmailToProvider: invoice.SendEmailToProvider,
                isFlexible: invoice.IsFlexible,
                subscriptionPeriod: invoice.SubscriptionPeriod,
                businessConnectionId: invoice.BusinessConnectionId,
                cancellationToken: ct).ConfigureAwait(false);

            metrics.InvoiceCreated(botId, invoice.Type, invoice.Currency);
            logger.LogDebug("Invoice link created for payload {Payload}", invoice.Payload);

            return InvoiceResult.Link(link);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create invoice link for payload {Payload}", invoice.Payload);
            return InvoiceResult.Failed(ex.Message);
        }
    }

    public async Task<InvoiceResult> SendInvoiceAsync(long chatId, InvoiceDefinition invoice, string botId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        try
        {
            var client = botProvider.GetClient(botId);
            var prices = invoice.Prices.Select(p => new LabeledPrice { Label = p.Label, Amount = p.Amount }).ToArray();

            var message = await client.SendInvoice(
                chatId,
                invoice.Title,
                invoice.Description,
                invoice.Payload,
                CurrencyToString(invoice.Currency),
                prices,
                NullIfEmpty(invoice.ProviderToken),
                maxTipAmount: invoice.MaxTipAmount,
                suggestedTipAmounts: invoice.SuggestedTipAmounts,
                providerData: invoice.ProviderData,
                photoUrl: invoice.PhotoUrl,
                photoSize: null,
                photoWidth: invoice.PhotoWidth,
                photoHeight: invoice.PhotoHeight,
                needName: invoice.NeedName,
                needPhoneNumber: invoice.NeedPhoneNumber,
                needEmail: invoice.NeedEmail,
                needShippingAddress: invoice.NeedShippingAddress,
                sendPhoneNumberToProvider: invoice.SendPhoneNumberToProvider,
                sendEmailToProvider: invoice.SendEmailToProvider,
                isFlexible: invoice.IsFlexible,
                cancellationToken: ct).ConfigureAwait(false);

            metrics.InvoiceCreated(botId, invoice.Type, invoice.Currency);
            logger.LogDebug("Invoice sent to chat {ChatId} for payload {Payload}", chatId, invoice.Payload);

            return InvoiceResult.Sent(message.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invoice to chat {ChatId} for payload {Payload}", chatId,
                invoice.Payload);
            return InvoiceResult.Failed(ex.Message);
        }
    }

    private static string CurrencyToString(PaymentCurrency currency)
    {
        return currency switch
        {
            PaymentCurrency.Xtr => "XTR",
            _ => currency.ToString().ToUpperInvariant()
        };
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
