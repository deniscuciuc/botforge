using BotForge.Messaging.Abstractions;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Crypto.DirectAddress;
using BotForge.Payments.Crypto.TonConnect;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using CryptoShop.Data;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoShop.Handlers;

/// <summary>
/// Initiates a TON Connect payment session and sends the user a deep-link they can open
/// in any TON wallet app (Tonkeeper, TON Space, etc.).
///
/// Flow:
///   1. User taps "TON Connect"
///   2. This handler creates a PurchaseOrder + WalletPaymentSession
///   3. Returns a deep-link button the user taps to open their wallet
///   4. The TonBlockchainObserverWorker polls for the transfer and calls
///      WalletPaymentService.RecordTransferAsync → triggers PurchaseFulfillmentPipeline
///
/// Route: shop:checkout:{productId}:ton-connect
/// </summary>
[CallbackQuery("shop:checkout:{productId}:ton-connect")]
public class TonConnectCheckoutHandler(
    ICommerceStore commerceStore,
    TonConnectSessionService tonConnect,
    ITelegramMessageService messaging,
    CryptoShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("productId");
        var product = await db.Products.FindAsync([productId], ct);

        if (product is null)
            return CallbackResult.Alert("Product not found.");

        var orderId = Guid.NewGuid().ToString("N");
        var payload = new CryptoShopPayload(productId, orderId);

        // Create and persist the PurchaseOrder (TON-priced)
        var order = new PurchaseOrder
        {
            Id = orderId,
            UserId = context.UserId!.Value,
            ChatId = context.ChatId!.Value,
            BotId = context.BotId,
            Payload = payload.Serialize(),
            Amount = product.PriceTon,
            Currency = PaymentCurrency.Ton,
            Rail = PurchaseRail.TonConnect,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await commerceStore.SaveOrderAsync(order, ct);

        // Create the WalletPaymentSession — this builds the deep-link URL
        var request = new WalletPaymentRequest
        {
            PurchaseOrderId = orderId,
            UserId = context.UserId!.Value,
            Rail = PurchaseRail.TonConnect,
            Amount = product.PriceTon,
            Currency = PaymentCurrency.Ton,
            Expiry = TimeSpan.FromMinutes(30)
        };

        var info = await tonConnect.CreateSessionAsync(request, ct);

        var tonDisplay = $"{product.PriceTon / 1_000_000_000.0:0.###} TON";

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                $"🔗 <b>TON Connect Payment</b>\n\n" +
                $"Product: <b>{product.Name}</b>\n" +
                $"Amount: <b>{tonDisplay}</b>\n\n" +
                $"Tap the button below to open your TON wallet and approve the transfer.\n\n" +
                $"⏳ This session expires in 30 minutes.\n" +
                $"Session ID: <code>{info.Session.SessionId[..8]}</code>")
            .WithInlineKeyboard(new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("💎 Open Wallet", info.PaymentUrl) },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "🔄 Check Payment Status",
                        $"shop:status:{info.Session.SessionId}")
                }
            }))
            .SendAsync(ct);

        return CallbackResult.Ok();
    }
}

/// <summary>
/// Initiates a TON Direct-Address payment session.
/// The user must manually send TON with the required memo comment to the deposit address.
///
/// Flow:
///   1. User taps "TON Direct"
///   2. This handler creates a PurchaseOrder + WalletPaymentSession with a unique memo
///   3. Tells the user the address + exact amount + required memo
///   4. The TonBlockchainObserverWorker detects the transfer via Toncenter API and settles
///
/// Route: shop:checkout:{productId}:ton-direct
/// </summary>
[CallbackQuery("shop:checkout:{productId}:ton-direct")]
public class TonDirectCheckoutHandler(
    ICommerceStore commerceStore,
    TonDirectAddressService tonDirect,
    ITelegramMessageService messaging,
    CryptoShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("productId");
        var product = await db.Products.FindAsync([productId], ct);

        if (product is null)
            return CallbackResult.Alert("Product not found.");

        var orderId = Guid.NewGuid().ToString("N");
        var payload = new CryptoShopPayload(productId, orderId);

        var order = new PurchaseOrder
        {
            Id = orderId,
            UserId = context.UserId!.Value,
            ChatId = context.ChatId!.Value,
            BotId = context.BotId,
            Payload = payload.Serialize(),
            Amount = product.PriceTon,
            Currency = PaymentCurrency.Ton,
            Rail = PurchaseRail.TonDirect,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        await commerceStore.SaveOrderAsync(order, ct);

        var request = new WalletPaymentRequest
        {
            PurchaseOrderId = orderId,
            UserId = context.UserId!.Value,
            Rail = PurchaseRail.TonDirect,
            Amount = product.PriceTon,
            Currency = PaymentCurrency.Ton,
            Expiry = TimeSpan.FromHours(1)
        };

        var info = await tonDirect.CreatePaymentAsync(request, ct);

        var tonDisplay = $"{product.PriceTon / 1_000_000_000.0:0.###} TON";

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                $"📥 <b>TON Direct Payment</b>\n\n" +
                $"Product: <b>{product.Name}</b>\n" +
                $"Amount: <b>{tonDisplay}</b>\n\n" +
                $"Send <b>exactly {tonDisplay}</b> to the address below.\n" +
                $"⚠️ You MUST include the memo comment, or your payment cannot be matched.\n\n" +
                $"📬 <b>Deposit address:</b>\n" +
                $"<code>{info.DepositAddress}</code>\n\n" +
                $"💬 <b>Required memo comment:</b>\n" +
                $"<code>{info.RequiredMemo}</code>\n\n" +
                $"⏳ This session expires in 1 hour.\n" +
                $"Session ID: <code>{info.Session.SessionId[..8]}</code>")
            .WithInlineKeyboard(new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "🔄 Check Payment Status",
                            $"shop:status:{info.Session.SessionId}")
                    }
                }))
            .SendAsync(ct);

        return CallbackResult.Ok();
    }
}

/// <summary>
/// Callback: check the status of a pending TON wallet payment session.
/// Route: shop:status:{sessionId}
/// </summary>
[CallbackQuery("shop:status:{sessionId}")]
public class PaymentStatusHandler(
    IWalletPaymentService walletPaymentService,
    ITelegramMessageService messaging) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var sessionId = context.GetRouteParam("sessionId");
        var session = await walletPaymentService.GetSessionAsync(sessionId, ct);

        if (session is null)
            return CallbackResult.Alert("Session not found.");

        var expiresIn = session.ExpiresAt - DateTimeOffset.UtcNow;
        var expired = expiresIn < TimeSpan.Zero;

        var statusEmoji = session.Status switch
        {
            SettlementStatus.Pending => expired ? "⌛ Expired" : "⏳ Pending",
            SettlementStatus.Confirmed => "✅ Confirmed",
            SettlementStatus.Failed => "❌ Failed",
            _ => session.Status.ToString()
        };

        var railDisplay = session.Rail switch
        {
            PurchaseRail.TonConnect => "TON Connect",
            PurchaseRail.TonDirect => "TON Direct",
            _ => session.Rail.ToString()
        };

        var text = $"💳 <b>Payment Status</b>\n\n" +
                   $"Rail: {railDisplay}\n" +
                   $"Status: {statusEmoji}\n";

        if (session.Status == SettlementStatus.Pending && !expired)
            text += $"Expires in: {expiresIn:hh\\:mm\\:ss}\n";
        else if (session.Status == SettlementStatus.Confirmed && session.TransactionHash is not null)
            text += $"Transaction: <code>{session.TransactionHash[..20]}…</code>\n";

        // Buttons: show "Re-check" if still pending
        InlineKeyboardMarkup? keyboard = null;
        if (session.Status == SettlementStatus.Pending && !expired)
            keyboard = new InlineKeyboardMarkup(
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Re-check", $"shop:status:{sessionId}") });

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .WithInlineKeyboard(keyboard ?? new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton[]>()))
            .SendAsync(ct);

        return CallbackResult.Ok();
    }
}

/// <summary>
/// /status — command version of the payment status check.
/// Usage: /status {sessionId}
/// </summary>
[TelegramCommand("/status", "Check a pending payment status")]
public class StatusCommandHandler(
    IWalletPaymentService walletPaymentService,
    ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        if (context.Arguments.Length == 0)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml("Usage: /status <code>{sessionId}</code>")
                .SendAsync(ct);
            return CommandResult.Ok();
        }

        var sessionId = context.Arguments[0];
        var session = await walletPaymentService.GetSessionAsync(sessionId, ct);

        if (session is null)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml($"❓ No session found for ID <code>{sessionId[..Math.Min(8, sessionId.Length)]}</code>.")
                .SendAsync(ct);
            return CommandResult.Ok();
        }

        var statusText = session.Status switch
        {
            SettlementStatus.Pending => "⏳ Pending",
            SettlementStatus.Confirmed => "✅ Confirmed",
            SettlementStatus.Failed => "❌ Failed",
            _ => session.Status.ToString()
        };

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                $"💳 <b>Session Status</b>\n\n" +
                $"Status: {statusText}\n" +
                $"Rail: {session.Rail}\n" +
                $"Amount: {session.ExpectedAmount / 1_000_000_000.0:0.###} TON\n" +
                (session.TransactionHash is not null
                    ? $"TX Hash: <code>{session.TransactionHash[..20]}…</code>"
                    : $"Expires: {session.ExpiresAt:yyyy-MM-dd HH:mm} UTC"))
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
