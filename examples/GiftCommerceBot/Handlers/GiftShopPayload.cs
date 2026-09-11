using System.Globalization;
using TeleForge.Payments.Abstractions;

namespace GiftCommerceBot.Handlers;

/// <summary>
/// Typed payload for gift-shop purchases. Serializes as "gift-shop:{productId}:{orderId}".
/// </summary>
public sealed record GiftShopPayload(int ProductId, string OrderId) : TypedPayload
{
    public override string Prefix => "gift-shop";

    public override string[] SerializeFields()
    {
        return [ProductId.ToString(CultureInfo.InvariantCulture), OrderId];
    }

    public static GiftShopPayload Deserialize(string[] fields)
    {
        return new GiftShopPayload(int.Parse(fields[0], CultureInfo.InvariantCulture), fields[1]);
    }
}
