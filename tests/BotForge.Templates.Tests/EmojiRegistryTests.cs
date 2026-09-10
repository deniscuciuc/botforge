namespace BotForge.Templates.Tests;

public class EmojiRegistryTests
{
    [Fact]
    public void Register_ThenGet_ReturnsEmoji()
    {
        var registry = new EmojiRegistry();
        registry.Register("rocket", "🚀");

        Assert.Equal("🚀", registry.Get("rocket"));
    }

    [Fact]
    public void Get_Unregistered_Throws()
    {
        var registry = new EmojiRegistry();
        Assert.Throws<KeyNotFoundException>(() => registry.Get("nonexistent"));
    }

    [Fact]
    public void GetOrDefault_Unregistered_ReturnsNull()
    {
        var registry = new EmojiRegistry();
        Assert.Null(registry.GetOrDefault("nope"));
    }

    [Fact]
    public void TryGet_Registered_ReturnsTrue()
    {
        var registry = new EmojiRegistry();
        registry.Register("star", "⭐");

        Assert.True(registry.TryGet("star", out var emoji));
        Assert.Equal("⭐", emoji);
    }

    [Fact]
    public void TryGet_Unregistered_ReturnsFalse()
    {
        var registry = new EmojiRegistry();
        Assert.False(registry.TryGet("nope", out _));
    }

    [Fact]
    public void Load_RegistersMultiple()
    {
        var registry = new EmojiRegistry();
        registry.Load(new Dictionary<string, string>
        {
            ["fire"] = "🔥",
            ["heart"] = "❤️"
        });

        Assert.Equal("🔥", registry.Get("fire"));
        Assert.Equal("❤️", registry.Get("heart"));
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        var registry = new EmojiRegistry();
        registry.Register("a", "1");
        registry.Register("b", "2");

        var all = registry.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var registry = new EmojiRegistry();
        registry.Register("Rocket", "🚀");

        Assert.Equal("🚀", registry.Get("ROCKET"));
        Assert.Equal("🚀", registry.Get("rocket"));
    }

    [Fact]
    public void ResolveEmojis_ReplacesTokens()
    {
        var registry = new EmojiRegistry();
        registry.Register("fire", "🔥");
        registry.Register("star", "⭐");

        var result = registry.ResolveEmojis("Hello {{emoji:fire}} World {{emoji:star}}!");

        Assert.Equal("Hello 🔥 World ⭐!", result);
    }

    [Fact]
    public void ResolveEmojis_UnknownEmoji_KeepsPlaceholder()
    {
        var registry = new EmojiRegistry();

        var result = registry.ResolveEmojis("Hello {{emoji:unknown}}!");

        Assert.Equal("Hello {{emoji:unknown}}!", result);
    }

    [Fact]
    public void ResolveEmojis_NoTokens_ReturnsSameString()
    {
        var registry = new EmojiRegistry();
        var result = registry.ResolveEmojis("No emojis here");
        Assert.Equal("No emojis here", result);
    }

    [Fact]
    public void ResolveEmojis_Empty_ReturnsEmpty()
    {
        var registry = new EmojiRegistry();
        Assert.Equal("", registry.ResolveEmojis(""));
    }

    [Fact]
    public void ResolveEmojis_Null_ReturnsNull()
    {
        var registry = new EmojiRegistry();
        Assert.Null(registry.ResolveEmojis(null!));
    }
}
