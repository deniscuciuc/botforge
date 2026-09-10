using BotForge.Payments.Fragment.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotForge.Payments.Fragment;

/// <summary>
/// Manages Fragment API JWT token lifecycle: rotation, pre-seeding, and auto-re-auth.
/// </summary>
internal sealed class FragmentAuthManager(
    FragmentSdk sdk,
    IOptions<FragmentOptions> options,
    ILogger<FragmentAuthManager> logger)
{
    private readonly FragmentOptions _options = options.Value;
    private DateTime _lastAuthTime = DateTime.MinValue;

    /// <summary>
    /// Ensures at least one valid token is available. Seeds initial tokens from options
    /// and re-authenticates if needed.
    /// </summary>
    public async Task EnsureAuthenticatedAsync(CancellationToken ct = default)
    {
        if (sdk.Token.Count > 0 && !IsTokenExpiringSoon())
            return;

        if (!CanAuthenticate())
        {
            if (sdk.Token.Count > 0)
                return; // Rely on existing tokens

            throw new InvalidOperationException(
                "No Fragment tokens available and auto-auth credentials not configured. " +
                "Provide Tokens in FragmentOptions or configure ApiKey/PhoneNumber/Mnemonics.");
        }

        await AuthenticateAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a fresh authentication against the Fragment API.
    /// </summary>
    public async Task<string> AuthenticateAsync(CancellationToken ct = default)
    {
        if (!CanAuthenticate())
            throw new InvalidOperationException("Auto-auth credentials not configured in FragmentOptions.");

        var request = new GenerateNewTokenRequest(
            _options.ApiKey,
            _options.PhoneNumber!,
            _options.Mnemonics);

        var token = await sdk.AuthenticateAsync(request, ct).ConfigureAwait(false);
        _lastAuthTime = DateTime.UtcNow;
        logger.LogInformation("Fragment API re-authenticated, token pool size: {TokenCount}", sdk.Token.Count);
        return token;
    }

    /// <summary>
    /// Seeds the SDK's token pool from configuration.
    /// </summary>
    public void SeedTokens()
    {
        foreach (var token in _options.Tokens) sdk.Token.Add(token);

        if (sdk.Token.Count > 0)
        {
            _lastAuthTime = DateTime.UtcNow;
            logger.LogInformation("Seeded {Count} Fragment API token(s) from configuration", _options.Tokens.Count);
        }
    }

    private bool CanAuthenticate()
    {
        return _options.AutoReAuth &&
               !string.IsNullOrEmpty(_options.PhoneNumber) &&
               _options.Mnemonics.Count > 0;
    }

    private bool IsTokenExpiringSoon()
    {
        if (_lastAuthTime == DateTime.MinValue)
            return true;

        // Fragment tokens typically expire after ~24h; re-auth well before
        var elapsed = DateTime.UtcNow - _lastAuthTime;
        return elapsed > TimeSpan.FromHours(23) - _options.TokenExpiryBuffer;
    }
}
