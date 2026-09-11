using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TeleForge.Core;
using TeleForge.Payments.Abstractions;
using TeleForge.Payments.Checkout;
using TeleForge.Payments.Router;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;

namespace TeleForge.Payments.Tests;

/// <summary>
/// Pre-checkout is the one moment Telegram lets a bot refuse a payment. Answering with
/// approval when a validator rejected — or failing to answer at all — takes money for
/// something that will not be delivered.
/// </summary>
public sealed class PreCheckoutPipelineTests
{
    private static PreCheckoutContext Context(string prefix = "SUB") => new()
    {
        Query = new PreCheckoutQuery
        {
            Id = "pcq-1",
            From = new User { Id = 42, FirstName = "Test" },
            Currency = "XTR",
            TotalAmount = 500,
            InvoicePayload = $"{prefix}:premium:30",
        },
        BotId = "main",
        RawPayload = $"{prefix}:premium:30",
        PayloadPrefix = prefix,
    };

    private sealed class Harness
    {
        public ITelegramBotClient Client { get; } = Substitute.For<ITelegramBotClient>();
        public IPaymentMetrics Metrics { get; } = Substitute.For<IPaymentMetrics>();
        public PaymentHandlerRegistry Registry { get; } = new();
        public ServiceCollection Services { get; } = new();

        public PreCheckoutPipeline Build()
        {
            var provider = Substitute.For<ITelegramBotClientProvider>();
            provider.GetClient(Arg.Any<string>()).Returns(Client);

            Services.AddSingleton(Registry);
            var sp = Services.BuildServiceProvider();

            return new PreCheckoutPipeline(provider, sp, Metrics, NullLogger<PreCheckoutPipeline>.Instance);
        }

        public IEnumerable<AnswerPreCheckoutQueryRequest> Answers =>
            Client.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == nameof(ITelegramBotClient.SendRequest))
                .SelectMany(c => c.GetArguments())
                .OfType<AnswerPreCheckoutQueryRequest>();
    }

    [Fact]
    public async Task Approves_WhenNoValidatorsAreRegistered()
    {
        var harness = new Harness();

        await harness.Build().HandleAsync(Context());

        var answer = Assert.Single(harness.Answers);
        Assert.Equal("pcq-1", answer.PreCheckoutQueryId);
        Assert.Null(answer.ErrorMessage);
        harness.Metrics.Received(1).CheckoutCompleted("main", "SUB", true);
    }

    [Fact]
    public async Task Rejects_WhenAGlobalValidatorRejects()
    {
        var harness = new Harness();
        harness.Services.AddSingleton<GlobalPreCheckoutValidator>(
            new StubGlobalValidator(CheckoutValidationResult.Reject("checkout_blocked", "blocked user")));

        await harness.Build().HandleAsync(Context());

        var answer = Assert.Single(harness.Answers);
        Assert.Equal("blocked user", answer.ErrorMessage);
        harness.Metrics.Received(1).CheckoutCompleted("main", "SUB", false);
    }

    [Fact]
    public async Task AnswersOnlyOnce_WhenAGlobalValidatorRejects()
    {
        var harness = new Harness();
        harness.Services.AddSingleton<GlobalPreCheckoutValidator>(
            new StubGlobalValidator(CheckoutValidationResult.Reject("checkout_blocked", "blocked")));

        await harness.Build().HandleAsync(Context());

        // Telegram treats a second answer to the same query as an error; approving after
        // rejecting would take the payment anyway.
        Assert.Single(harness.Answers);
    }

    [Fact]
    public async Task Approves_WhenEveryGlobalValidatorApproves()
    {
        var harness = new Harness();
        harness.Services.AddSingleton<GlobalPreCheckoutValidator>(
            new StubGlobalValidator(CheckoutValidationResult.Approve()));
        harness.Services.AddSingleton<GlobalPreCheckoutValidator>(
            new StubGlobalValidator(CheckoutValidationResult.Approve()));

        await harness.Build().HandleAsync(Context());

        Assert.Null(Assert.Single(harness.Answers).ErrorMessage);
        harness.Metrics.Received(1).CheckoutCompleted("main", "SUB", true);
    }

    [Fact]
    public async Task StillAnswers_WhenAValidatorThrows()
    {
        var harness = new Harness();
        harness.Services.AddSingleton<GlobalPreCheckoutValidator>(new ThrowingGlobalValidator());

        await harness.Build().HandleAsync(Context());

        // An unanswered pre-checkout query leaves the user's payment hanging until it times
        // out, so a crash inside a validator must still produce an answer.
        Assert.Equal("Internal error", Assert.Single(harness.Answers).ErrorMessage);
        harness.Metrics.Received(1).CheckoutCompleted("main", "SUB", false);
    }

    [Fact]
    public async Task RejectsANullContext()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new Harness().Build().HandleAsync(null!));
    }

    private sealed class StubGlobalValidator(CheckoutValidationResult result) : GlobalPreCheckoutValidator
    {
        public override Task<CheckoutValidationResult> ValidateAsync(
            PreCheckoutContext context, CancellationToken ct = default)
            => Task.FromResult(result);
    }

    private sealed class ThrowingGlobalValidator : GlobalPreCheckoutValidator
    {
        public override Task<CheckoutValidationResult> ValidateAsync(
            PreCheckoutContext context, CancellationToken ct = default)
            => throw new InvalidOperationException("validator exploded");
    }
}
