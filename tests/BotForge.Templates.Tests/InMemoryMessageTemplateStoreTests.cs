namespace BotForge.Templates.Tests;

public class InMemoryMessageTemplateStoreTests
{
    [Fact]
    public void Get_WhenEmpty_ReturnsNull()
    {
        var store = new InMemoryMessageTemplateStore();
        Assert.Null(store.Get("nonexistent"));
    }

    [Fact]
    public void AddOrReplace_ThenGet_ReturnsTemplate()
    {
        var store = new InMemoryMessageTemplateStore();
        var template = new MessageTemplate { Name = "welcome" };

        store.AddOrReplace(template);

        Assert.Same(template, store.Get("welcome"));
    }

    [Fact]
    public void AddOrReplace_OverwritesExisting()
    {
        var store = new InMemoryMessageTemplateStore();
        var original = new MessageTemplate { Name = "welcome", Category = "old" };
        var updated = new MessageTemplate { Name = "welcome", Category = "new" };

        store.AddOrReplace(original);
        store.AddOrReplace(updated);

        Assert.Equal("new", store.Get("welcome")!.Category);
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(new MessageTemplate { Name = "Welcome" });

        Assert.NotNull(store.Get("WELCOME"));
        Assert.NotNull(store.Get("welcome"));
    }

    [Fact]
    public void Load_AddsMultipleTemplates()
    {
        var store = new InMemoryMessageTemplateStore();
        var templates = new[]
        {
            new MessageTemplate { Name = "t1" },
            new MessageTemplate { Name = "t2" },
            new MessageTemplate { Name = "t3" }
        };

        store.Load(templates);

        Assert.Equal(3, store.GetAll().Count);
    }

    [Fact]
    public void GetAll_ReturnsAllTemplates()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(new MessageTemplate { Name = "a" });
        store.AddOrReplace(new MessageTemplate { Name = "b" });

        var all = store.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Remove_DeletesExistingTemplate()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(new MessageTemplate { Name = "toRemove" });

        Assert.True(store.Remove("toRemove"));
        Assert.Null(store.Get("toRemove"));
    }

    [Fact]
    public void Remove_NonExistent_ReturnsFalse()
    {
        var store = new InMemoryMessageTemplateStore();
        Assert.False(store.Remove("nope"));
    }
}
