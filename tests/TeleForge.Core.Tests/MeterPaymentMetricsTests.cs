using TeleForge.Payments.Metrics;
using TeleForge.TestUtilities;

namespace TeleForge.Core.Tests;

public class MeterPaymentMetricsTests
{
    [Fact]
    public void PaymentFailed_UsesBoundedReasonLabels()
    {
        TelegramMetricsRuntime.Configure(false, false, false, false, true);
        using var capture = new MetricCaptureListener(MeterPaymentMetrics.MeterName);
        var sut = new MeterPaymentMetrics();

        for (var i = 0; i < 40; i++)
            sut.PaymentFailed("main", "shop", $"reason-{i}");

        var records = capture.ForInstrument("teleforge.telegram.payments.payment.outcome")
            .Where(r => r.Tags.TryGetValue("status", out var status) && status == "failed")
            .ToArray();

        Assert.NotEmpty(records);

        var distinctReasonLabels = records
            .Select(r => r.Tags.TryGetValue("reason", out var reason) ? reason : null)
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(distinctReasonLabels.Length <= 17, "reason label count must be capped (16 + other)");
        Assert.Contains("other", distinctReasonLabels);
    }

    [Fact]
    public void AntiFraudCheck_UsesBoundedCheckLabels()
    {
        TelegramMetricsRuntime.Configure(false, false, false, false, true);
        using var capture = new MetricCaptureListener(MeterPaymentMetrics.MeterName);
        var sut = new MeterPaymentMetrics();

        for (var i = 0; i < 40; i++)
            sut.AntiFraudCheck($"Check Name {i} / variant", i % 2 == 0);

        var records = capture.ForInstrument("teleforge.telegram.payments.antifraud.check").ToArray();

        Assert.NotEmpty(records);

        var distinctCheckLabels = records
            .Select(r => r.Tags.TryGetValue("check", out var check) ? check : null)
            .Where(check => !string.IsNullOrWhiteSpace(check))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(distinctCheckLabels.Length <= 17, "check label count must be capped (16 + other)");
        Assert.Contains("other", distinctCheckLabels);
    }
}
