using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using BotForge.Core;
using BotForge.Payments.Abstractions;

namespace BotForge.Payments.Metrics;

public sealed class MeterPaymentMetrics : IPaymentMetrics
{
    private const int MaxReasonLabels = 16;
    private const int MaxCheckLabels = 16;

    public const string MeterName = "BotForge.Payments";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> InvoiceCreatedCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.invoice.created", "{invoice}",
            "Created invoices.");

    private static readonly Counter<long> CheckoutCompletedCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.checkout.completed", "{checkout}",
            "Pre-checkout approvals/rejections.");

    private static readonly Counter<long> PaymentOutcomeCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.payment.outcome", "{payment}",
            "Payment success/failure outcomes.");

    private static readonly Histogram<long> PaymentAmountHistogram =
        Meter.CreateHistogram<long>("botforge.telegram.payments.payment.amount", "star",
            "Payment amount in stars.");

    private static readonly Counter<long> RefundProcessedCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.refund.processed", "{refund}",
            "Processed refunds.");

    private static readonly Histogram<long> RefundAmountHistogram =
        Meter.CreateHistogram<long>("botforge.telegram.payments.refund.amount", "star",
            "Refund amount in stars.");

    private static readonly Counter<long> PayoutRequestedCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.payout.requested", "{payout}",
            "Requested payouts.");

    private static readonly Counter<long> PayoutCompletedCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.payout.completed", "{payout}",
            "Completed payouts by status.");

    private static readonly Histogram<double> PayoutDurationMsHistogram =
        Meter.CreateHistogram<double>("botforge.telegram.payments.payout.duration", "ms",
            "Payout duration in milliseconds.");

    private static readonly Counter<long> GiftSentCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.gift.sent", "{gift}",
            "Sent gifts.");

    private static readonly Counter<long> PaidMediaSoldCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.paid_media.sold", "{sale}",
            "Paid media sales.");

    private static readonly Histogram<long> PaidMediaStarsHistogram =
        Meter.CreateHistogram<long>("botforge.telegram.payments.paid_media.stars", "star",
            "Paid media stars sold per transaction.");

    private static readonly Counter<long> AntiFraudCheckCounter =
        Meter.CreateCounter<long>("botforge.telegram.payments.antifraud.check", "{check}",
            "Anti-fraud checks and outcomes.");

    private static readonly Histogram<double> PaymentProcessingDurationMs =
        Meter.CreateHistogram<double>("botforge.telegram.payments.processing.duration", "ms",
            "Payment processing duration in milliseconds.");

    private readonly object _reasonLabelsLock = new();
    private readonly HashSet<string> _reasonLabelsSeen = new(StringComparer.Ordinal);

    private readonly object _checkLabelsLock = new();
    private readonly HashSet<string> _checkLabelsSeen = new(StringComparer.Ordinal);

    public void InvoiceCreated(string botId, InvoiceType type, PaymentCurrency currency)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        InvoiceCreatedCounter.Add(1,
            new TagList
            {
                { "bot_key", botId },
                { "invoice_type", type.ToString() },
                { "currency", currency.ToString() }
            });
    }

    public void CheckoutCompleted(string botId, string payloadPrefix, bool approved)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        CheckoutCompletedCounter.Add(1,
            new TagList
            {
                { "bot_key", botId },
                { "payload_prefix", payloadPrefix },
                { "approved", approved ? "true" : "false" }
            });
    }

    public void PaymentSucceeded(string botId, string payloadPrefix, int amount, PaymentCurrency currency)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        var tags = new TagList
        {
            { "bot_key", botId },
            { "payload_prefix", payloadPrefix },
            { "status", "success" },
            { "currency", currency.ToString() }
        };

        PaymentOutcomeCounter.Add(1, tags);
        PaymentAmountHistogram.Record(amount, tags);
    }

    public void PaymentFailed(string botId, string payloadPrefix, string reason)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        var reasonLabel = SanitizeReasonLabel(reason);

        PaymentOutcomeCounter.Add(1,
            new TagList
            {
                { "bot_key", botId },
                { "payload_prefix", payloadPrefix },
                { "status", "failed" },
                { "reason", reasonLabel }
            });
    }

    public void RefundProcessed(string botId, RefundReason reason, int amount)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        var tags = new TagList
        {
            { "bot_key", botId },
            { "reason", reason.ToString() }
        };

        RefundProcessedCounter.Add(1, tags);
        RefundAmountHistogram.Record(amount, tags);
    }

    public void PayoutRequested(PayoutProvider provider, PaymentCurrency currency)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        PayoutRequestedCounter.Add(1,
            new TagList
            {
                { "provider", provider.ToString() },
                { "currency", currency.ToString() }
            });
    }

    public void PayoutCompleted(PayoutProvider provider, PaymentCurrency currency, PayoutStatus status)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        PayoutCompletedCounter.Add(1,
            new TagList
            {
                { "provider", provider.ToString() },
                { "currency", currency.ToString() },
                { "status", status.ToString() }
            });
    }

    public void PayoutDuration(PayoutProvider provider, double milliseconds)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        PayoutDurationMsHistogram.Record(milliseconds,
            new TagList
            {
                { "provider", provider.ToString() }
            });
    }

    public void GiftSent(string botId, GiftType giftType)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        GiftSentCounter.Add(1,
            new TagList
            {
                { "bot_key", botId },
                { "gift_type", giftType.ToString() }
            });
    }

    public void PaidMediaSold(string botId, int starCount)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        var tags = new TagList
        {
            { "bot_key", botId }
        };

        PaidMediaSoldCounter.Add(1, tags);
        PaidMediaStarsHistogram.Record(starCount, tags);
    }

    public void AntiFraudCheck(string checkName, bool passed)
    {
        if (!TelegramMetricsRuntime.PaymentsEnabled)
            return;

        var checkLabel = SanitizeCheckLabel(checkName);

        AntiFraudCheckCounter.Add(1,
            new TagList
            {
                { "check", checkLabel },
                { "passed", passed ? "true" : "false" }
            });
    }

    public IDisposable MeasurePaymentProcessing(string botId, string payloadPrefix)
    {
        return new DurationScope(botId, payloadPrefix);
    }

    private sealed class DurationScope(string botId, string payloadPrefix) : IDisposable
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public void Dispose()
        {
            if (!TelegramMetricsRuntime.PaymentsEnabled)
                return;

            _sw.Stop();
            PaymentProcessingDurationMs.Record(_sw.Elapsed.TotalMilliseconds,
                new TagList
                {
                    { "bot_key", botId },
                    { "payload_prefix", payloadPrefix }
                });
        }
    }

    private string SanitizeReasonLabel(string? reason)
    {
        return SanitizeBoundedLabel(reason, _reasonLabelsSeen, _reasonLabelsLock, MaxReasonLabels);
    }

    private string SanitizeCheckLabel(string? checkName)
    {
        return SanitizeBoundedLabel(checkName, _checkLabelsSeen, _checkLabelsLock, MaxCheckLabels);
    }

    private static string SanitizeBoundedLabel(
        string? raw,
        HashSet<string> observedLabels,
        object sync,
        int maxDistinct)
    {
        var normalized = NormalizeLabel(raw);

        lock (sync)
        {
            if (observedLabels.Contains(normalized))
                return normalized;

            if (observedLabels.Count >= maxDistinct)
                return "other";

            observedLabels.Add(normalized);
            return normalized;
        }
    }

    private static string NormalizeLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        var input = value.Trim().ToLowerInvariant();
        var sb = new StringBuilder(input.Length);
        var prevUnderscore = false;

        foreach (var ch in input)
        {
            var isAsciiAlphaNum = ch is >= 'a' and <= 'z' or >= '0' and <= '9';
            if (isAsciiAlphaNum)
            {
                sb.Append(ch);
                prevUnderscore = false;
                continue;
            }

            if (!prevUnderscore)
            {
                sb.Append('_');
                prevUnderscore = true;
            }
        }

        var normalized = sb.ToString().Trim('_');
        if (string.IsNullOrEmpty(normalized))
            normalized = "unknown";

        const int maxLen = 32;
        if (normalized.Length > maxLen)
            normalized = normalized[..maxLen];

        return normalized;
    }
}
