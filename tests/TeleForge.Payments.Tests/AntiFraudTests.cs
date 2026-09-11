using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using TeleForge.Payments.Abstractions;
using TeleForge.Payments.AntiFraud;

namespace TeleForge.Payments.Tests;

/// <summary>
/// Anti-fraud is the last gate before a payout leaves. A check that silently passes, or a
/// pipeline that keeps going after a failure, means money moves that should not have.
/// </summary>
public sealed class AntiFraudTests
{
    private static AntiFraudContext Context(int amount, long userId = 42) => new()
    {
        UserId = userId,
        Amount = amount,
        Currency = PaymentCurrency.Ton,
        Provider = PayoutProvider.Fragment,
        Destination = "EQ_destination",
    };

    private static AmountLimitCheck Limits(int min = 1, int max = int.MaxValue) =>
        new(Options.Create(new AmountLimitCheckOptions { MinAmount = min, MaxAmount = max }));

    [Fact]
    public async Task AmountLimit_Passes_WithinRange()
    {
        var result = await Limits(min: 10, max: 100).EvaluateAsync(Context(50));

        Assert.True(result.Passed);
    }

    [Theory]
    [InlineData(10, 100, 10)]
    [InlineData(10, 100, 100)]
    public async Task AmountLimit_TreatsBothBoundsAsInclusive(int min, int max, int amount)
    {
        var result = await Limits(min, max).EvaluateAsync(Context(amount));

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task AmountLimit_Fails_BelowMinimum()
    {
        var result = await Limits(min: 10).EvaluateAsync(Context(9));

        Assert.False(result.Passed);
        Assert.Equal("antifraud_amount_too_low", result.TemplateKey);
    }

    [Fact]
    public async Task AmountLimit_Fails_AboveMaximum()
    {
        var result = await Limits(max: 100).EvaluateAsync(Context(101));

        Assert.False(result.Passed);
        Assert.Equal("antifraud_amount_too_high", result.TemplateKey);
    }

    [Fact]
    public async Task AmountLimit_Fails_OnZeroAndNegativeAmounts_UnderTheDefaultMinimum()
    {
        Assert.False((await Limits().EvaluateAsync(Context(0))).Passed);
        Assert.False((await Limits().EvaluateAsync(Context(-1))).Passed);
    }

    [Fact]
    public async Task AmountLimit_RejectsANullContext()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Limits().EvaluateAsync(null!));
    }

    [Fact]
    public async Task Pipeline_Passes_WhenEveryCheckPasses()
    {
        var metrics = Substitute.For<IPaymentMetrics>();
        var pipeline = new AntiFraudPipeline(
            [Check("one", pass: true), Check("two", pass: true)],
            metrics,
            NullLogger<AntiFraudPipeline>.Instance);

        var result = await pipeline.EvaluateAsync(Context(50));

        Assert.True(result.Passed);
        metrics.Received(1).AntiFraudCheck("one", true);
        metrics.Received(1).AntiFraudCheck("two", true);
    }

    [Fact]
    public async Task Pipeline_ShortCircuits_OnTheFirstFailure()
    {
        var metrics = Substitute.For<IPaymentMetrics>();
        var second = Check("second", pass: true);

        var result = await new AntiFraudPipeline(
            [Check("first", pass: false, reason: "nope"), second],
            metrics,
            NullLogger<AntiFraudPipeline>.Instance).EvaluateAsync(Context(50));

        Assert.False(result.Passed);
        Assert.Equal("nope", result.FailureReason);

        // The second check must never run — a later check cannot rescue a rejected payout.
        await second.DidNotReceive().EvaluateAsync(Arg.Any<AntiFraudContext>(), Arg.Any<CancellationToken>());
        metrics.DidNotReceive().AntiFraudCheck("second", Arg.Any<bool>());
    }

    [Fact]
    public async Task Pipeline_RecordsTheFailingCheckInMetrics()
    {
        var metrics = Substitute.For<IPaymentMetrics>();

        await new AntiFraudPipeline(
            [Check("velocity", pass: false, reason: "too fast")],
            metrics,
            NullLogger<AntiFraudPipeline>.Instance).EvaluateAsync(Context(50));

        metrics.Received(1).AntiFraudCheck("velocity", false);
    }

    [Fact]
    public async Task Pipeline_PassesWithNoChecksRegistered()
    {
        // An empty pipeline is a deployment mistake, not a rejection — but it must be a
        // deliberate, visible one, so it passes rather than throwing.
        var result = await new AntiFraudPipeline(
            [], Substitute.For<IPaymentMetrics>(), NullLogger<AntiFraudPipeline>.Instance)
            .EvaluateAsync(Context(50));

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Pipeline_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var check = Substitute.For<IAntiFraudCheck>();
        check.Name.Returns("cancels");
        check.EvaluateAsync(Arg.Any<AntiFraudContext>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromCanceled<AntiFraudResult>(cts.Token));

        var pipeline = new AntiFraudPipeline(
            [check], Substitute.For<IPaymentMetrics>(), NullLogger<AntiFraudPipeline>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pipeline.EvaluateAsync(Context(50), cts.Token));
    }

    [Fact]
    public async Task Pipeline_RejectsANullContext()
    {
        var pipeline = new AntiFraudPipeline(
            [], Substitute.For<IPaymentMetrics>(), NullLogger<AntiFraudPipeline>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => pipeline.EvaluateAsync(null!));
    }

    [Fact]
    public async Task Velocity_FailsClosed_WhenNoStoreIsRegistered()
    {
        // Registering a velocity check states an intent to limit payouts. Passing everything
        // because the store is missing would be the worst possible failure for a control
        // that exists to stop money leaving.
        var check = new VelocityCheck(Options.Create(new VelocityCheckOptions()));

        var result = await check.EvaluateAsync(Context(100));

        Assert.False(result.Passed);
        Assert.Equal("antifraud_velocity_unavailable", result.TemplateKey);
    }

    [Fact]
    public async Task Velocity_Passes_WhenUnderBothLimits()
    {
        var check = new VelocityCheck(
            Options.Create(new VelocityCheckOptions { MaxPerHour = 5, MaxPerDay = 20 }),
            StoreReturning(hourly: 2, daily: 7));

        Assert.True((await check.EvaluateAsync(Context(100))).Passed);
    }

    [Fact]
    public async Task Velocity_Fails_OnTheHourlyLimit()
    {
        var check = new VelocityCheck(
            Options.Create(new VelocityCheckOptions { MaxPerHour = 5, MaxPerDay = 20 }),
            StoreReturning(hourly: 5, daily: 5));

        var result = await check.EvaluateAsync(Context(100));

        Assert.False(result.Passed);
        Assert.Equal("antifraud_velocity_hourly", result.TemplateKey);
    }

    [Fact]
    public async Task Velocity_Fails_OnTheDailyLimit()
    {
        var check = new VelocityCheck(
            Options.Create(new VelocityCheckOptions { MaxPerHour = 5, MaxPerDay = 20 }),
            StoreReturning(hourly: 1, daily: 20));

        var result = await check.EvaluateAsync(Context(100));

        Assert.False(result.Passed);
        Assert.Equal("antifraud_velocity_daily", result.TemplateKey);
    }

    [Fact]
    public async Task Velocity_TreatsTheLimitAsExclusive()
    {
        // At exactly one below the limit the payout still goes through; the limit is the
        // first rejected value, not the last accepted one.
        var check = new VelocityCheck(
            Options.Create(new VelocityCheckOptions { MaxPerHour = 5, MaxPerDay = 20 }),
            StoreReturning(hourly: 4, daily: 19));

        Assert.True((await check.EvaluateAsync(Context(100))).Passed);
    }

    [Fact]
    public async Task Velocity_ChecksTheHourWindowBeforeTheDayWindow()
    {
        // Cheaper, more specific window first: a user over the hourly limit should not cost
        // a second store round-trip.
        var store = StoreReturning(hourly: 99, daily: 0);

        await new VelocityCheck(
            Options.Create(new VelocityCheckOptions { MaxPerHour = 5, MaxPerDay = 20 }), store)
            .EvaluateAsync(Context(100));

        await store.Received(1).CountPayoutsSinceAsync(
            Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Velocity_RejectsANullContext()
    {
        var check = new VelocityCheck(Options.Create(new VelocityCheckOptions()));

        await Assert.ThrowsAsync<ArgumentNullException>(() => check.EvaluateAsync(null!));
    }

    /// <summary>
    /// A store whose payout count depends on how far back the window reaches: the hourly
    /// query asks for one hour, the daily query for a day.
    /// </summary>
    private static IPaymentStore StoreReturning(int hourly, int daily)
    {
        var store = Substitute.For<IPaymentStore>();
        store.CountPayoutsSinceAsync(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var since = call.ArgAt<DateTimeOffset>(1);
                return DateTimeOffset.UtcNow - since > TimeSpan.FromHours(2) ? daily : hourly;
            });
        return store;
    }

    private static IAntiFraudCheck Check(string name, bool pass, string reason = "failed")
    {
        var check = Substitute.For<IAntiFraudCheck>();
        check.Name.Returns(name);
        check.EvaluateAsync(Arg.Any<AntiFraudContext>(), Arg.Any<CancellationToken>())
            .Returns(pass ? AntiFraudResult.Pass() : AntiFraudResult.Fail(reason, name.ToLowerInvariant()));
        return check;
    }
}
