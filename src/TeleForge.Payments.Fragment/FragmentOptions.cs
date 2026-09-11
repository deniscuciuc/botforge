namespace TeleForge.Payments.Fragment;

/// <summary>
/// Configuration for the Fragment payout provider.
/// </summary>
public class FragmentOptions
{
    /// <summary>JWT tokens for Fragment API authentication (supports round-robin rotation).</summary>
    public List<string> Tokens { get; set; } = [];

    /// <summary>API key for generating new tokens.</summary>
    public Guid? ApiKey { get; set; }

    /// <summary>Phone number associated with the Fragment account.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Mnemonic words for authentication.</summary>
    public List<string> Mnemonics { get; set; } = [];

    /// <summary>Fragment API base URL override (default: https://api.fragment-api.com).</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>HTTP request timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Whether to automatically re-authenticate when tokens expire.</summary>
    public bool AutoReAuth { get; set; } = true;

    /// <summary>Token expiry buffer — trigger re-auth this long before actual expiry.</summary>
    public TimeSpan TokenExpiryBuffer { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Optional webhook URL for async order completion notifications.</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>Minimum wallet balance (in TON) before payouts are paused.</summary>
    public decimal MinWalletBalance { get; set; } = 0;
}
