using System.Globalization;
using TeleForge.Payments.Abstractions;

namespace TelegramShop.Handlers;

/// <summary>
/// Typed payload for shop purchases. Serializes as "shop:orderId".
/// </summary>
public sealed record ShopPayload(int OrderId) : TypedPayload
{
    public override string Prefix => "shop";

    public override string[] SerializeFields()
    {
        return [OrderId.ToString(CultureInfo.InvariantCulture)];
    }

    public static ShopPayload Deserialize(string[] fields)
    {
        return new ShopPayload(int.Parse(fields[0], CultureInfo.InvariantCulture));
    }
}
