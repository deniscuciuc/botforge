# Payments

## Overview

The payments system provides a complete Telegram Stars payment flow:

1. **Invoice creation** — build and send invoices to users
2. **Pre-checkout validation** — validate orders before payment
3. **Payment processing** — fulfill orders after successful payment
4. **Refunds** — automatic and manual refund handling
5. **Payouts** — withdraw Stars via Fragment or manual review
6. **Anti-fraud** — velocity checks and amount limits

## Registration

```csharp
builder.Services.AddTelegramPayments(
    options =>
    {
        options.AutoRefundOnFailure = true;
        options.PayoutBatchSize = 10;
    },
    handlers =>
    {
        handlers.AddValidator<MyValidator>("shop");
        handlers.AddProcessor<MyProcessor>("shop");
        handlers.AddRefundProcessor<MyRefundProcessor>("shop");
    });
```

### PaymentOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AutoRefundOnFailure` | `bool` | `true` | Auto-refund if processing fails |
| `PayoutWorkerInterval` | `TimeSpan` | `30s` | Background payout poll interval |
| `PayoutBatchSize` | `int` | `10` | Payouts per batch |
| `PayoutMaxRetries` | `int` | `3` | Max payout retry attempts |
| `RevenueSyncEnabled` | `bool` | `false` | Enable transaction sync |
| `RevenueSyncInterval` | `TimeSpan` | `5 min` | Sync poll interval |

## Creating Invoices

### Building an Invoice

```csharp
var invoice = InvoiceBuilder.Stars()
    .WithTitle("Premium Access")
    .WithDescription("Unlock all features for 30 days")
    .WithPayload(new PremiumPayload { UserId = userId })
    .AddPrice("Premium Plan", 100)
    .WithPhoto("https://example.com/premium.jpg", 320, 320)
    .Build();
```

### InvoiceBuilder Methods

| Method | Description |
|--------|-------------|
| `Stars()` | Static factory — creates builder for Telegram Stars |
| `WithTitle(string)` | Invoice title (required) |
| `WithDescription(string)` | Invoice description (required) |
| `WithPayload(TypedPayload)` | Typed payload (serialized automatically) |
| `WithRawPayload(string)` | Raw payload string |
| `AddPrice(string label, int amount)` | Add price line (at least one required) |
| `WithPhoto(url, width?, height?)` | Invoice thumbnail |
| `WithMaxTipAmount(int)` | Maximum allowed tip |
| `WithSuggestedTips(params int[])` | Suggested tip amounts |
| `WithProviderToken(string)` | Payment provider token (empty = Stars) |
| `WithProviderData(string)` | Provider-specific JSON data |
| `WithSubscriptionPeriod(int seconds)` | For subscription invoices |
| `WithBusinessConnection(string)` | Business connection ID |
| `AsType(InvoiceType)` | Override invoice type |
| `RequireName()` | Request buyer's name |
| `RequirePhoneNumber()` | Request buyer's phone |
| `RequireEmail()` | Request buyer's email |
| `RequireShippingAddress()` | Request shipping address |
| `Build()` | Validate and build `InvoiceDefinition` |

### Sending Invoices

```csharp
// Send directly to a chat
InvoiceResult result = await invoiceService.SendInvoiceAsync(chatId, invoice, "main", ct);
if (result.Success)
    Console.WriteLine($"Invoice sent: message {result.MessageId}");

// Or create a shareable link
InvoiceResult link = await invoiceService.CreateLinkAsync(invoice, "main", ct);
if (link.Success)
    Console.WriteLine($"Pay at: {link.InvoiceLink}");
```

## Typed Payloads

Define structured payloads that serialize to a colon-separated format:

```csharp
public record ShopPayload : TypedPayload
{
    public override string Prefix => "shop";
    public required string OrderId { get; init; }
    public override string[] SerializeFields() => [OrderId];
}
```

Serialized as `"shop:abc123"`. The prefix is used to route to the correct validator/processor.

### Deserializing Payloads

```csharp
// In a validator or processor:
var payload = context.GetPayload(fields => new ShopPayload
{
    OrderId = fields[0]
});
```

## Pre-Checkout Validation

Validates orders before Telegram processes the payment:

```csharp
public class ShopValidator : IPreCheckoutValidator
{
    public async Task<CheckoutValidationResult> ValidateAsync(
        PreCheckoutContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(f => new ShopPayload { OrderId = f[0] });

        // Check inventory, validate amounts, etc.
        if (!await IsOrderValid(payload.OrderId))
            return CheckoutValidationResult.Reject("order_invalid", "Order no longer available");

        return CheckoutValidationResult.Approve();
    }
}
```

### PreCheckoutContext Properties

| Property | Type | Description |
|----------|------|-------------|
| `Query` | `PreCheckoutQuery` | Raw Telegram query |
| `BotId` | `string` | Bot key |
| `PayloadPrefix` | `string` | Extracted prefix |
| `RawPayload` | `string` | Full payload string |
| `UserId` | `long` | Buyer's user ID |
| `TotalAmount` | `long` | Total in smallest units |
| `Currency` | `string` | Currency code |
| `PreCheckoutQueryId` | `string` | Query ID |
| `Items` | `IDictionary<string, object>` | Data bag between validators |

### Global Validators

Run for ALL payment prefixes:

```csharp
builder.Services.AddGlobalPreCheckoutValidator<FraudCheckValidator>();
```

## Payment Processing

Called after payment succeeds:

```csharp
public class ShopProcessor : IPaymentProcessor
{
    public async Task ProcessAsync(SuccessfulPaymentContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(f => new ShopPayload { OrderId = f[0] });
        
        await FulfillOrder(payload.OrderId, context.ChargeId);

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId)
            .WithText($"Order {payload.OrderId} confirmed!")
            .SendAsync(ct);
    }
}
```

### SuccessfulPaymentContext Properties

| Property | Type | Description |
|----------|------|-------------|
| `Payment` | `SuccessfulPayment` | Raw Telegram payment |
| `BotId` | `string` | Bot key |
| `PayloadPrefix` | `string` | Extracted prefix |
| `RawPayload` | `string` | Full payload string |
| `ChatId` | `long` | Buyer's chat ID |
| `UserId` | `long` | Buyer's user ID |
| `MessageId` | `int` | Payment message ID |
| `ChargeId` | `string` | Telegram charge ID |
| `TotalAmount` | `long` | Total amount |
| `Currency` | `string` | Currency code |
| `ProviderPaymentChargeId` | `string?` | Provider's charge ID |
| `Items` | `IDictionary<string, object>` | Data bag |

> **Note**: If processing throws and `AutoRefundOnFailure` is enabled, the framework automatically issues a refund.

## Refunds

### Automatic Refunds

Enabled by default via `AutoRefundOnFailure = true`. If `IPaymentProcessor.ProcessAsync` throws, the framework calls `RefundStarPayment` automatically.

### Manual Refunds

```csharp
bool success = await refundService.RefundStarPaymentAsync(
    botId: "main",
    userId: 123456,
    telegramPaymentChargeId: chargeId,
    ct);
```

### Refund Processors

Handle business logic when a refund is received:

```csharp
public class ShopRefundProcessor : IRefundProcessor
{
    public async Task<RefundDecision> ProcessAsync(RefundContext context, CancellationToken ct)
    {
        // Reverse inventory, credits, etc.
        return RefundDecision.Reverse(context.TotalAmount, RefundReason.CustomerRequest);
    }
}
```

The refund pipeline includes idempotency checks via `IPaymentStore.IsRefundRecordedAsync`.

## Payouts

Withdraw bot Stars to a destination:

```csharp
var result = await payoutPipeline.RequestPayoutAsync(new PayoutRequest
{
    UserId = userId,
    Amount = 500,
    Currency = PaymentCurrency.Xtr,
    Destination = "TON_WALLET_ADDRESS",
    Provider = PayoutProvider.Fragment
}, ct);
```

### Payout Providers

| Provider | Description |
|----------|-------------|
| `ManualPayoutProvider` | Always returns `NeedsReview` — for manual approval |
| `FragmentPayoutProvider` | Executes via Fragment API |

### Fragment Setup

```csharp
builder.Services.AddFragmentPayoutProvider(options =>
{
    options.ApiKey = "FRAGMENT_API_KEY";
    // Additional Fragment configuration...
});

// Optional health check
builder.Services.AddFragmentHealthCheck();
```

### Background Workers

```csharp
builder.Services.AddPayoutWorker();          // Processes pending payouts
builder.Services.AddTransactionSyncWorker(); // Syncs Star transactions
```

## Anti-Fraud

Anti-fraud checks run before any payout:

```csharp
builder.Services.AddAntiFraudCheck<AmountLimitCheck>();
builder.Services.AddAntiFraudCheck<VelocityCheck>();
```

### Built-in Checks

| Check | Options | Description |
|-------|---------|-------------|
| `AmountLimitCheck` | `MinAmount`, `MaxAmount`, `MaxDailyTotal` | Validates amount bounds |
| `VelocityCheck` | `MaxPerHour`, `MaxPerDay` | Rate-limits payout requests |

### Custom Checks

```csharp
public class MyFraudCheck : IAntiFraudCheck
{
    public string Name => "my-check";

    public async Task<AntiFraudResult> EvaluateAsync(AntiFraudContext context, CancellationToken ct)
    {
        if (context.Amount > 10000)
            return AntiFraudResult.Fail("Amount exceeds limit");
        return AntiFraudResult.Pass();
    }
}
```

## Payment Store

Implement `IPaymentStore` to persist payment records:

```csharp
public interface IPaymentStore
{
    Task RecordPaymentAsync(PaymentRecord record, CancellationToken ct);
    Task<PaymentRecord?> GetByChargeIdAsync(string chargeId, CancellationToken ct);
    Task UpdatePaymentStatusAsync(string chargeId, PaymentStatus status, CancellationToken ct);
    Task RecordRefundAsync(RefundRecord record, CancellationToken ct);
    Task<bool> IsRefundRecordedAsync(string chargeId, CancellationToken ct);
    Task<string> SavePayoutAsync(PayoutRecord record, CancellationToken ct);
    Task<IReadOnlyList<PayoutRecord>> GetPendingPayoutsAsync(int limit, CancellationToken ct);
    Task UpdatePayoutStatusAsync(string id, PayoutStatus status, string? txId, string? error, CancellationToken ct);
    Task SaveStarTransactionsAsync(IEnumerable<StarTransactionRecord> records, CancellationToken ct);
    Task<DateTimeOffset?> GetLastTransactionSyncDateAsync(string botId, CancellationToken ct);
}
```

## Additional Services

### Star Transactions

```csharp
StarBalance balance = await starTransactionService.GetBalanceAsync("main", ct);
var transactions = await starTransactionService.GetTransactionsAsync("main", offset: 0, limit: 100, ct);
```

### Gifts

```csharp
var gifts = await giftService.GetAvailableGiftsAsync("main", ct);
await giftService.SendGiftAsync(new SendGiftRequest { ... }, ct);
await giftService.GiftPremiumAsync("main", userId, months: 3, ct);
```

### Subscriptions

```csharp
var link = await subscriptionService.CreateSubscriptionLinkAsync(plan, ct);
await subscriptionService.EditUserSubscriptionAsync("main", userId, chargeId, isCanceled: true, ct);
```

### Paid Media

```csharp
await paidMediaService.SendPaidMediaAsync(new PaidMediaDefinition { ... }, ct);
```

## Payment Flow Diagram

```
User clicks "Pay" → Telegram sends PreCheckoutQuery
    ↓
PreCheckoutPipeline:
    1. GlobalPreCheckoutValidators (all prefixes)
    2. Prefix-specific IPreCheckoutValidator chain
    3. AnswerPreCheckoutQuery(approved/rejected)
    ↓
User confirms payment → Telegram sends SuccessfulPayment
    ↓
PaymentProcessingPipeline:
    1. Prefix-specific IPaymentProcessor chain
    2. On success → metrics
    3. On failure + AutoRefundOnFailure → auto-refund → metrics
    ↓
Refund received → RefundPipeline:
    1. Idempotency check
    2. Prefix-specific IRefundProcessor chain
    3. Record refund → metrics
```

See [Metrics and Observability](metrics.md) for the complete payment metrics catalog and dashboards.

---

## Gift-Based Commerce (Inbound Gifts as Payment)

Telegram Gifts can be used as a payment rail: users send a Telegram Gift to the bot, which is mapped to a `PurchaseOrder` and triggers the same fulfillment pipeline as Star invoices.

### Architecture

```
User sends Gift → Message service update (Message.Gift / Message.UniqueGift)
    ↓
IGiftMessageHandler / IUniqueGiftMessageHandler (routing handler)
    ↓
IGiftIntakeService.ProcessReceivedGiftAsync()
    ↓
  Idempotency check (IGiftIntakeStore)
  Match to pending PurchaseOrder (ICommerceStore)
    ↓
PurchaseFulfillmentPipeline.FulfillAsync()
    ↓
IPurchaseFulfillmentHandler chain (per-prefix)
```

### Registration

```csharp
// 1. Register base payments + cross-rail commerce
builder.Services.AddTelegramPayments(...);

builder.Services.AddPurchaseFulfillment(handlers =>
{
    handlers.AddFulfillmentHandler<MyGiftFulfillmentHandler>("shop");
});

// 2. Enable inbound gift support
builder.Services.AddGiftInboundSupport();

// 3. Register the two store implementations (your application code)
builder.Services.AddSingleton<ICommerceStore, MyCommerceStore>();
builder.Services.AddSingleton<IGiftIntakeStore, MyGiftIntakeStore>();
```

### Creating a Gift Payment Order

Before the user sends a gift, create a `PurchaseOrder` with `Rail = GiftInbound` so the intake service can match it:

```csharp
var order = new PurchaseOrder
{
    UserId = userId,
    BotId = "main",
    Payload = "shop:item42",
    Amount = 50,           // convert-star count required
    Currency = PaymentCurrency.Xtr,
    Rail = PurchaseRail.GiftInbound,
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
};

await commerceStore.SaveOrderAsync(order, ct);
// Send the deposit address or gift catalog to the user
```

### Handling Gift Messages in the Router

```csharp
[GiftMessage]
public class GiftPaymentHandler(IGiftIntakeService giftIntake) : IGiftMessageHandler
{
    public async Task HandleAsync(GiftMessageContext context, CancellationToken ct)
    {
        await giftIntake.ProcessReceivedGiftAsync(new ReceivedGiftEvent
        {
            BotId = context.BotId,
            OwnedGiftId = context.OwnedGiftId ?? string.Empty,
            GiftId = context.GiftInfo.Gift.Id,
            GiftType = GiftType.Regular,
            SenderUserId = context.UserId,
            ConvertStarCount = context.ConvertStarCount,
            CanBeUpgraded = context.CanBeUpgraded,
            GiftedAt = DateTimeOffset.UtcNow,
        }, ct);
    }
}
```

### Fulfillment Handler

```csharp
public class MyGiftFulfillmentHandler : IPurchaseFulfillmentHandler
{
    public async Task FulfillAsync(PurchaseOrder order, SettlementRecord settlement, CancellationToken ct)
    {
        // Grant access, deliver item, etc.
        var orderId = order.Payload.Split(':')[1];
        await myOrderService.CompleteOrderAsync(orderId, ct);
    }
}
```

### Business Account Reconciliation

For bots connected to a Business account, use `IGiftService.GetBusinessAccountGiftsAsync` and `ReconcileBusinessGiftsAsync` to process gifts that arrived without a service message:

```csharp
await giftIntakeService.ReconcileBusinessGiftsAsync(
    botId: "main",
    businessConnectionId: "biz-abc123",
    ct);
```

---

## TON / Crypto Wallet Payments

The `TeleForge.Payments.Crypto` package adds three wallet payment rails:

| Rail | Description |
|------|-------------|
| `TonConnect` | Mini App opens Tonkeeper/TON Space wallet via deep-link |
| `TonDirect` | User sends TON/USDT to the bot's deposit address with a memo comment |
| `CustodialWallet` | Custodial provider webhook (implement custom `ITonRpcClient`) |

### Registration

```csharp
using TeleForge.Payments.Crypto.Extensions;

// 1. Base payments + wallet support
builder.Services.AddTelegramPayments(...);
builder.Services.AddPurchaseFulfillment(h => h.AddFulfillmentHandler<MyFulfillmentHandler>("shop"));
builder.Services.AddWalletPaymentSupport();

// 2. Register stores
builder.Services.AddSingleton<ICommerceStore, MyCommerceStore>();
builder.Services.AddSingleton<IWalletPaymentStore, MyWalletPaymentStore>();

// 3. TON services + blockchain observer
builder.Services.AddTonWalletPayments(options =>
{
    options.DepositAddress = "EQD...your_ton_deposit_address";
    options.ApiKey = "toncenter_api_key";          // optional but recommended
    options.PollingInterval = TimeSpan.FromSeconds(15);
    options.UsdtJettonWalletAddress = "EQD...usdt_jetton_wallet"; // optional
});
```

### TON Connect Flow (Mini App)

```csharp
// In a command or callback handler
var info = await tonConnectService.CreateSessionAsync(new WalletPaymentRequest
{
    PurchaseOrderId = orderId,
    UserId = userId,
    Rail = PurchaseRail.TonConnect,
    Amount = 1_000_000_000L,   // 1 TON in nanotons
    Currency = PaymentCurrency.Ton,
}, ct);

// Send the payment URL to the user via a WebApp button
// info.PaymentUrl contains the Tonkeeper deep-link
```

### Direct Address Flow

```csharp
var info = await tonDirectService.CreatePaymentAsync(new WalletPaymentRequest
{
    PurchaseOrderId = orderId,
    UserId = userId,
    Rail = PurchaseRail.TonDirect,
    Amount = 1_000_000_000L,
    Currency = PaymentCurrency.Ton,
}, ct);

// Instruct the user:
// Send {amount} TON to {info.DepositAddress}
// Comment: {info.RequiredMemo}
```

The `TonBlockchainObserverWorker` runs in the background, polls the deposit address, matches incoming transfers by memo comment, and calls `WalletPaymentService.RecordTransferAsync` automatically.

### Custom RPC Backend

Replace the default Toncenter client with your own implementation:

```csharp
builder.Services.AddTonWalletPayments(options => { ... });
builder.Services.UseTonRpcClient<MyTonApiClient>(); // replaces ToncenterRpcClient
```

### Store Interfaces to Implement

| Interface | Purpose |
|-----------|---------|
| `ICommerceStore` | `PurchaseOrder` and `SettlementRecord` persistence |
| `IWalletPaymentStore` | `WalletPaymentSession` persistence |
| `IGiftIntakeStore` | `GiftIntakeRecord` persistence (gift rail only) |

---

## Cross-Rail PurchaseOrder Model

All non-invoice rails share the same `PurchaseOrder` structure:

```csharp
public class PurchaseOrder
{
    public string Id { get; init; }              // Framework-generated GUID
    public long UserId { get; init; }            // Buyer
    public long? ChatId { get; init; }           // Optional chat context
    public string BotId { get; init; }          // Bot key
    public string Payload { get; init; }         // "prefix:data" — used to route fulfillment handlers
    public long Amount { get; init; }            // Amount in smallest unit
    public PaymentCurrency Currency { get; init; }
    public PurchaseRail Rail { get; init; }      // Stars | GiftInbound | TonConnect | TonDirect | ...
    public SettlementStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public Dictionary<string, string> Metadata { get; init; } // Optional constraints (e.g. "gift_id")
}
```

### PurchaseRail Enum

| Value | Description |
|-------|-------------|
| `Stars` | Telegram Stars invoice |
| `GiftOutbound` | Gift sent by the bot as fulfillment |
| `GiftInbound` | User sends a gift to the bot |
| `TonConnect` | TON Connect wallet (Mini App) |
| `TonDirect` | Direct TON/USDT transfer to deposit address |
| `CustodialWallet` | Custodial wallet provider webhook |

---

## Examples

The `examples/` folder contains two fully working bots that demonstrate every payment rail described in this document.

---

### GiftCommerceBot — Stars + Gift-as-Payment

**Location**: `examples/GiftCommerceBot/`

This bot sells digital products and accepts payment via either **Telegram Stars** (classic invoice flow) or an **inbound Telegram Gift** (gift-as-payment). Both rails are processed by a single `IPurchaseFulfillmentHandler`.

#### DI Wiring (`Program.cs`)

```csharp
// Persistence
builder.Services.AddDbContext<GiftShopDbContext>(o =>
    o.UseSqlite("Data Source=giftshop.db"));

// Telegram
builder.Services.AddTelegramMessaging(...)
    .AddBot("main", ...);
builder.Services.AddTelegramRouting()
    .AddHandlersFromAssembly(Assembly.GetExecutingAssembly());

// Payments — register validator and processor under the same prefix
builder.Services.AddTelegramPayments(payments =>
{
    payments.AddCheckoutValidator<GiftShopCheckoutValidator>("gift-shop");
    payments.AddPaymentProcessor<GiftShopStarsProcessor>("gift-shop");
});

// Fulfillment pipeline — one handler covers both rails
builder.Services.AddPurchaseFulfillment(fulfillment =>
    fulfillment.AddFulfillmentHandler<GiftShopFulfillmentHandler>("gift-shop"));

// Gift inbound support — registers GiftIntakeService and reconciliation worker
builder.Services.AddGiftInboundSupport();

// Stores (EF Core implementations live in Stores/GiftStores.cs)
builder.Services.AddScoped<ICommerceStore, EfCommerceStore>();
builder.Services.AddScoped<IGiftIntakeStore, EfGiftIntakeStore>();

// Register fulfillment handler for DI resolution by the pipeline
builder.Services.AddScoped<GiftShopFulfillmentHandler>();
```

> `AddGiftInboundSupport()` registers `IGiftIntakeService` and the background reconciliation worker.  
> The fulfillment handler must be registered **both** with the pipeline registry (via `AddFulfillmentHandler`) **and** as a DI service.

#### Stars Payment Flow

```
User: /start → browses catalog → taps "⭐ Pay with Stars"
  → BuyWithStarsHandler
      Creates PurchaseOrder(Rail=Stars, Payload="gift-shop:{productId}:{orderId}")
      Calls IInvoiceService.SendInvoiceAsync(InvoiceBuilder.Stars(...).WithPayload(payload))
  → Telegram sends pre-checkout update
  → GiftShopCheckoutValidator.ValidateAsync
      Loads order from ICommerceStore, checks status == Pending
  → Telegram sends successful_payment update
  → GiftShopStarsProcessor.ProcessAsync
      Creates SettlementRecord, calls PurchaseFulfillmentPipeline.FulfillAsync(order, settlement)
  → GiftShopFulfillmentHandler.FulfillAsync
      Sends "✅ Purchase Confirmed! (paid with ⭐ N Stars)"
```

#### Gift Payment Flow

```
User: taps "🎁 Pay with a Gift"
  → BuyWithGiftHandler
      Creates PurchaseOrder(Rail=GiftInbound, Payload="gift-shop:{productId}:{orderId}")
      Sends "Send a gift worth ⭐N to this chat within 24h"
  [User sends a Telegram Gift in the chat]
  → GiftReceivedHandler (implements IGiftMessageHandler)
      Calls IGiftIntakeService.ProcessReceivedGiftAsync(userId, gift)
      Service finds matching pending order, matches by userId + star value
      Calls PurchaseFulfillmentPipeline.FulfillAsync(order, settlement)
  → GiftShopFulfillmentHandler.FulfillAsync
      Sends "✅ Purchase Confirmed! (paid with 🎁 Gift)"
```

#### Reconciliation

The `/reconcile` command triggers `ReconcileBusinessGiftsAsync`, which fetches all gifts received by the Business account from the Telegram API and processes any that were missed (e.g. if the bot was offline). This is useful for Business connection bots where incoming gifts arrive as Business messages.

```csharp
[TelegramCommand("/reconcile")]
public class ReconcileGiftsCommand(IGiftIntakeService giftIntakeService, IOptions<...> config)
    : ICommandHandler
{
    public async Task HandleAsync(CommandContext context, CancellationToken ct)
    {
        var count = await giftIntakeService.ReconcileBusinessGiftsAsync(
            businessConnectionId, ct);
        await context.SendTextMessageAsync($"Reconciled {count} gifts.");
    }
}
```

---

### CryptoShop — Stars + TON Connect + TON Direct

**Location**: `examples/CryptoShop/`

This bot demonstrates **three payment rails** through a single fulfillment handler:

| Rail | Button | Description |
|------|--------|-------------|
| Stars | ⭐ Pay with Stars | Telegram invoice — works in any chat |
| TonConnect | 🔗 TON Connect | Tonkeeper / TON Space Mini App deep-link |
| TonDirect | 📥 TON Direct Address | Send TON/USDT to bot's deposit address with a memo comment |

#### DI Wiring (`Program.cs`)

```csharp
// Telegram + routing
builder.Services.AddTelegramMessaging(...).AddBot("main", ...);
builder.Services.AddTelegramRouting()
    .AddHandlersFromAssembly(Assembly.GetExecutingAssembly());

// Stars payment validator/processor under "crypto-shop" prefix
builder.Services.AddTelegramPayments(payments =>
{
    payments.AddCheckoutValidator<CryptoShopCheckoutValidator>("crypto-shop");
    payments.AddPaymentProcessor<CryptoShopStarsProcessor>("crypto-shop");
});

// Fulfillment pipeline — one handler for all three rails
builder.Services.AddPurchaseFulfillment(fulfillment =>
    fulfillment.AddFulfillmentHandler<CryptoShopFulfillmentHandler>("crypto-shop"));

// Wallet payment service — manages WalletPaymentSession lifecycle
builder.Services.AddWalletPaymentSupport();

// TON services + TonBlockchainObserverWorker background worker
builder.Services.AddTonWalletPayments(options =>
{
    options.DepositAddress        = configuration["TonPayments:DepositAddress"]!;
    options.ApiKey                = configuration["TonPayments:ApiKey"];
    options.PollingInterval       = TimeSpan.FromSeconds(15);
    // options.UsdtJettonWalletAddress = "...";  // enable USDT support
});

// Stores
builder.Services.AddScoped<ICommerceStore, EfCommerceStore>();
builder.Services.AddScoped<IWalletPaymentStore, EfWalletPaymentStore>();
builder.Services.AddScoped<CryptoShopFulfillmentHandler>();
```

> `AddWalletPaymentSupport()` registers `IWalletPaymentService`.  
> `AddTonWalletPayments()` registers `TonConnectSessionService`, `TonDirectAddressService`, and starts `TonBlockchainObserverWorker`.  
> `IWalletPaymentStore` **must** be registered or the observer worker will throw on startup.

#### TON Connect Flow

```
User: taps "🔗 TON Connect"
  → TonConnectCheckoutHandler
      Creates PurchaseOrder(Rail=TonConnect, Currency=Ton, Amount=<nanotons>)
      Calls TonConnectSessionService.CreateSessionAsync(WalletPaymentRequest)
        → Returns TonConnectPaymentInfo { Session, PaymentUrl }
          PaymentUrl is a Tonkeeper deep-link: tonkeeper://transfer?address=...&amount=...&text={memo}
      Sends message with [Open Wallet] InlineKeyboardButton(url: PaymentUrl)
                           + [Check Status] callback "shop:status:{sessionId}"

  [User opens Tonkeeper, approves transaction]

  → TonBlockchainObserverWorker (background, polls every 15 s)
      Fetches transactions on the deposit address from Toncenter API
      Matches transaction comment (memo) to WalletPaymentSession
      Calls IWalletPaymentService.RecordTransferAsync(sessionId, txHash, senderAddress)
        → Updates session status to Confirmed
        → Creates SettlementRecord, calls PurchaseFulfillmentPipeline.FulfillAsync
  → CryptoShopFulfillmentHandler.FulfillAsync
      Sends "✅ Order confirmed! Paid with 🔗 TON Connect — 1.5 TON"
```

#### TON Direct Address Flow

```
User: taps "📥 TON Direct"
  → TonDirectCheckoutHandler
      Creates PurchaseOrder(Rail=TonDirect, Currency=Ton)
      Calls TonDirectAddressService.CreatePaymentAsync(WalletPaymentRequest)
        → Returns DirectAddressPaymentInfo { Session, DepositAddress, RequiredMemo, UsdtJettonAddress }
      Sends message:
        "Send exactly 1.5 TON to:
         EQD...your_deposit_address
         Comment (required): ABC123XYZ
         ⚠️ The comment is mandatory — transfers without it cannot be matched."
      Includes [Check Status] callback

  [User opens any TON wallet, completes transfer with memo]

  → TonBlockchainObserverWorker matches by RequiredMemo comment
  → Same fulfillment path as TON Connect above
```

#### Status Polling

Users can check payment status at any time without waiting for background detection:

```csharp
// Inline button callback: "shop:status:{sessionId}"
[CallbackQuery("shop:status:{sessionId}")]
public class PaymentStatusHandler(IWalletPaymentService walletService)
    : ICallbackQueryHandler
{
    public async Task HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var sessionId = context.GetRouteParam<string>("sessionId");
        var session   = await walletService.GetSessionAsync(sessionId, ct);

        var statusText = session?.Status switch
        {
            SettlementStatus.Confirmed => "✅ Payment confirmed!",
            SettlementStatus.Pending   => "⏳ Awaiting payment...",
            SettlementStatus.Expired   => "❌ Payment expired.",
            _                          => "❓ Unknown status."
        };
        // Edit the message to show current status + re-check button
    }
}
```

The `/status {sessionId}` command provides the same status via a text command, useful for deep-links.

#### Fulfillment Handler (all three rails)

A single handler covers every rail; use `order.Rail` to customize the confirmation message:

```csharp
public class CryptoShopFulfillmentHandler(ITelegramMessageService messenger)
    : IPurchaseFulfillmentHandler
{
    public async Task FulfillAsync(PurchaseOrder order, SettlementRecord settlement, CancellationToken ct)
    {
        var railLabel = order.Rail switch
        {
            PurchaseRail.Stars      => $"⭐ {order.Amount} Stars",
            PurchaseRail.TonConnect => $"🔗 TON Connect — {order.Amount / 1e9:F2} TON",
            PurchaseRail.TonDirect  => $"📥 TON Direct — {order.Amount / 1e9:F2} TON",
            _                       => order.Rail.ToString()
        };
        await messenger.SendTextMessageAsync(order.ChatId!.Value,
            $"✅ Order confirmed!\nPaid with {railLabel}");
    }
}
```

---

### Common Patterns Across Examples

#### TypedPayload Routing

Both examples use a `TypedPayload` subclass to embed purchase context into the Telegram payload string:

```csharp
sealed record GiftShopPayload(int ProductId, string OrderId) : TypedPayload
{
    public override string Prefix => "gift-shop";
    public override string Serialize() => $"{Prefix}:{ProductId}:{OrderId}";
    public static GiftShopPayload? TryParse(string raw) { ... }
}
```

The prefix (`"gift-shop"`, `"crypto-shop"`) must match the string passed to `AddFulfillmentHandler<T>("prefix")`, `AddCheckoutValidator<T>("prefix")`, and `AddPaymentProcessor<T>("prefix")`.

#### Order Lifecycle

```
Created (Pending)
    ↓ user pays
Processing  ← pipeline.FulfillAsync called (saves settlement)
    ↓ fulfillment handler runs
Confirmed   ← pipeline marks order Confirmed after handler returns
```

For wallet rails (TonConnect/TonDirect) the observer worker drives this transition automatically. For Stars, the `IPaymentProcessor` implementation triggers it. The same `PurchaseFulfillmentPipeline` handles both paths.

#### Store Implementations

Both examples provide EF Core + SQLite implementations you can adapt to your own database:

| Store | Class | Used By |
|-------|-------|---------|
| `ICommerceStore` | `EfCommerceStore` | Stars + all wallet rails |
| `IGiftIntakeStore` | `EfGiftIntakeStore` | GiftCommerceBot gift rail |
| `IWalletPaymentStore` | `EfWalletPaymentStore` | CryptoShop TON rails |

