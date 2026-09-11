using System.Globalization;
using TeleForge.Payments.Abstractions;

namespace CryptoShop.Handlers;

/// <summary>
/// Typed payload for crypto-shop purchases.
/// Serializes as "crypto-shop:{productId}:{orderId}".
/// </summary>
public sealed record CryptoShopPayload(int ProductId, string OrderId) : TypedPayload
{
    public override string Prefix => "crypto-shop";

    public override string[] SerializeFields()
    {
        return [ProductId.ToString(CultureInfo.InvariantCulture), OrderId];
    }

    public static CryptoShopPayload Deserialize(string[] fields)
    {
        return new CryptoShopPayload(int.Parse(fields[0], CultureInfo.InvariantCulture), fields[1]);
    }
}
