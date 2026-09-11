using Microsoft.Extensions.Logging;
using TeleForge.Payments.Fragment.Api;

namespace TeleForge.Payments.Fragment;

/// <summary>
/// High-level Fragment API client that wraps <see cref="FragmentSdk"/>
/// with auto-auth and structured result types.
/// </summary>
internal sealed class FragmentClient(
    FragmentSdk sdk,
    FragmentAuthManager authManager,
    FragmentMetrics metrics,
    ILogger<FragmentClient> logger)
{
    /// <summary>Buy Telegram Stars for a user.</summary>
    public async Task<OrderResponse?> SendStarsAsync(
        string username, int quantity, bool showSender = false,
        string? webhookUrl = null, CancellationToken ct = default)
    {
        await authManager.EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await sdk.BuyStarsAsync(
                new StarsOrderRequest(username, quantity, showSender, webhookUrl), ct).ConfigureAwait(false);
            sw.Stop();
            metrics.RecordRequest("SendStars", "ok", sw.Elapsed.TotalMilliseconds);
            metrics.RecordStarsAmount(quantity);
            logger.LogInformation("Stars sent: {Quantity} to @{Username}, order {OrderId}",
                quantity, username, result?.Id);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            metrics.RecordRequest("SendStars", "error", sw.Elapsed.TotalMilliseconds);
            logger.LogError(ex, "Failed to send {Quantity} stars to @{Username}", quantity, username);
            throw;
        }
    }

    /// <summary>Gift a Premium subscription to a user.</summary>
    public async Task<OrderResponse?> GiftPremiumAsync(
        string username, int months, bool showSender = false,
        string? webhookUrl = null, CancellationToken ct = default)
    {
        await authManager.EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await sdk.GiftPremiumAsync(
                new PremiumOrderRequest(username, months, showSender, webhookUrl), ct).ConfigureAwait(false);
            sw.Stop();
            metrics.RecordRequest("GiftPremium", "ok", sw.Elapsed.TotalMilliseconds);
            logger.LogInformation("Premium gifted: {Months}m to @{Username}, order {OrderId}",
                months, username, result?.Id);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            metrics.RecordRequest("GiftPremium", "error", sw.Elapsed.TotalMilliseconds);
            logger.LogError(ex, "Failed to gift {Months}m premium to @{Username}", months, username);
            throw;
        }
    }

    /// <summary>Get order status by ID.</summary>
    public async Task<OrderResponse?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        await authManager.EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
        return await sdk.GetOrderAsync(orderId, ct).ConfigureAwait(false);
    }

    /// <summary>Look up a Fragment user.</summary>
    public async Task<UserInfoResponse?> GetUserInfoAsync(string username, CancellationToken ct = default)
    {
        await authManager.EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
        return await sdk.GetUserInfoAsync(username, ct).ConfigureAwait(false);
    }

    /// <summary>Get wallet balance.</summary>
    public async Task<WalletInfoResponse?> GetWalletBalanceAsync(CancellationToken ct = default)
    {
        await authManager.EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
        return await sdk.GetWalletBalanceAsync(ct).ConfigureAwait(false);
    }
}
