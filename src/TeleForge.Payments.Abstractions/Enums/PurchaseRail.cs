namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Identifies which payment rail originated a settlement event.
/// </summary>
public enum PurchaseRail
{
    /// <summary>Telegram Stars invoice (existing flow).</summary>
    Stars,

    /// <summary>Bot sent a Telegram gift as purchase fulfillment.</summary>
    GiftOutbound,

    /// <summary>User sent a Telegram gift to the bot / business account as payment.</summary>
    GiftInbound,

    /// <summary>User connected a TON wallet via TON Connect and signed a transfer.</summary>
    TonConnect,

    /// <summary>User sent TON to a generated deposit address / comment reference.</summary>
    TonDirect,

    /// <summary>User paid via a custodial wallet provider (e.g. Telegram Wallet).</summary>
    CustodialWallet
}
