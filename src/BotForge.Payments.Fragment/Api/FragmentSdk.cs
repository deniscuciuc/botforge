using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

internal sealed class FragmentSdk : IDisposable
{
    private readonly bool _disposeClient;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public FragmentSdk(
        string[]? tokens = null,
        TimeSpan? timeout = null,
        HttpClient? httpClient = null,
        Uri? baseAddress = null)
    {
        Token = new Tokens();
        if (tokens != null)
            foreach (var t in tokens)
                Token.Add(t);

        if (httpClient is null)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = baseAddress ?? new Uri("https://api.fragment-api.com"),
                Timeout = timeout ?? TimeSpan.FromMinutes(3)
            };
            _disposeClient = true;
        }
        else
        {
            _httpClient = httpClient;
            if (httpClient.BaseAddress == null)
                httpClient.BaseAddress = baseAddress ?? new Uri("https://api.fragment-api.com");
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    public Tokens Token { get; }
    public Uri BaseAddress => _httpClient.BaseAddress!;

    public void Dispose()
    {
        if (_disposeClient) _httpClient.Dispose();
    }

    public Task<OrderResponse?> BuyStarsAsync(
        StarsOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<StarsOrderRequest, OrderResponse?>("/v1/order/stars/", request, true, cancellationToken);
    }

    public async Task<string> AuthenticateAsync(
        GenerateNewTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var auth = await PostAsync<GenerateNewTokenRequest, GenerateNewTokenResponse>("/v1/auth/authenticate/", request,
            false, cancellationToken).ConfigureAwait(false);
        Token.Add(auth.Token);
        return auth.Token;
    }

    public async Task<UserInfoResponse?> GetUserInfoAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required", nameof(username));
        var path = $"/v1/misc/user/{Uri.EscapeDataString(username)}/";
        return await GetAsync<UserInfoResponse>(path, cancellationToken, true).ConfigureAwait(false);
    }

    public Task<WalletInfoResponse?> GetWalletBalanceAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<WalletInfoResponse>("/v1/misc/wallet/", cancellationToken);
    }

    public Task<OrderResponse?> GetOrderAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Order id is required", nameof(id));
        return GetAsync<OrderResponse>($"/v1/order/{Uri.EscapeDataString(id)}/", cancellationToken);
    }

    public Task<OrderResponse?> GetOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetOrderAsync(id.ToString(), cancellationToken);
    }

    public Task<OrderResponse?> GiftPremiumAsync(
        PremiumOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<PremiumOrderRequest, OrderResponse?>("/v1/order/premium/", request, true, cancellationToken);
    }

    private async Task<TResponse?> GetAsync<TResponse>(
        string path,
        CancellationToken cancellationToken,
        bool treat404AsNull = false)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        AddAuthIfNeeded(req);
        using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (treat404AsNull && resp.StatusCode == HttpStatusCode.NotFound)
            return default;
        await EnsureSuccessOrThrow(resp, cancellationToken).ConfigureAwait(false);
        if (resp.Content.Headers.ContentLength == 0)
            return default;
        var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8,
            "application/json");
        if (requiresAuth) AddAuthIfNeeded(req, true);
        using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessOrThrow(resp, cancellationToken).ConfigureAwait(false);
        var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result == null) throw new FragmentApiException("Empty response body", resp.StatusCode);
        return result;
    }

    private void AddAuthIfNeeded(HttpRequestMessage req, bool throwIfMissing = false)
    {
        if (Token.Count > 0)
            req.Headers.Authorization = new AuthenticationHeaderValue("JWT", Token.Next());
        else if (throwIfMissing)
            throw new InvalidOperationException("Authentication token not set. Call AuthenticateAsync first.");
    }

    private async Task EnsureSuccessOrThrow(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        Error[]? errors = null;
        try
        {
            {
                var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using var streamDisposal = stream.ConfigureAwait(false);
                if (stream is { CanSeek: true, Length: 0 })
                {
                }
                else
                {
                    var wrapper = await JsonSerializer.DeserializeAsync<ErrorsWrapper>(stream, _jsonOptions, ct).ConfigureAwait(false);
                    errors = wrapper?.Errors?.ToArray();
                }
            }
        }
        catch
        {
            // Ignore any errors during error parsing, we'll throw a more generic exception below
        }

        throw new FragmentApiException(
            $"Fragment API returned status {(int)response.StatusCode} {response.ReasonPhrase}", response.StatusCode,
            errors);
    }
}
