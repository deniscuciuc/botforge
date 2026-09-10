namespace BotForge.Payments.Fragment.Api;

internal sealed class Tokens
{
    private readonly Lock _lock = new();
    private readonly List<string> _tokens = [];
    private int _currentIndex = -1;

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _tokens.Count;
            }
        }
    }

    public string Next()
    {
        lock (_lock)
        {
            if (_tokens.Count == 0)
                throw new InvalidOperationException($"No tokens available in {nameof(Tokens)}.");

            _currentIndex = (_currentIndex + 1) % _tokens.Count;
            return _tokens[_currentIndex];
        }
    }

    public void Add(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));

        lock (_lock)
        {
            _tokens.Add(token);
        }
    }
}
