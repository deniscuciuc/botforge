namespace BotForge.Templates.Tests;

public class PartialResolverTests
{
    private static InMemoryMessageTemplateStore CreateStore(params MessageTemplate[] templates)
    {
        var store = new InMemoryMessageTemplateStore();
        store.Load(templates);
        return store;
    }

    [Fact]
    public void Resolve_NoPartials_ReturnsSameText()
    {
        var store = CreateStore();
        var resolver = new PartialResolver(store);

        var result = resolver.Resolve("Hello world", "en");

        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void Resolve_WithPartial_InjectsPartialContent()
    {
        var store = CreateStore(
            new MessageTemplate
            {
                Name = "footer",
                IsPartial = true,
                Text = new MessageTemplateText { Translations = { ["en"] = "-- Footer --" } }
            });
        var resolver = new PartialResolver(store);

        var result = resolver.Resolve("Content {> footer}", "en");

        Assert.Equal("Content -- Footer --", result);
    }

    [Fact]
    public void Resolve_MissingPartial_ShowsError()
    {
        var store = CreateStore();
        var resolver = new PartialResolver(store);

        var result = resolver.Resolve("Content {> missing_partial}", "en");

        Assert.Contains("[Missing partial: missing_partial]", result);
    }

    [Fact]
    public void Resolve_CircularPartial_ShowsError()
    {
        var store = CreateStore(
            new MessageTemplate
            {
                Name = "self_ref",
                IsPartial = true,
                Text = new MessageTemplateText { Translations = { ["en"] = "loop {> self_ref}" } }
            });
        var resolver = new PartialResolver(store);

        var result = resolver.Resolve("{> self_ref}", "en");

        Assert.Contains("[Circular partial: self_ref]", result);
    }

    [Fact]
    public void Resolve_NonPartialTemplate_ShowsMissingError()
    {
        var store = CreateStore(
            new MessageTemplate
            {
                Name = "not_partial",
                IsPartial = false,
                Text = new MessageTemplateText { Translations = { ["en"] = "content" } }
            });
        var resolver = new PartialResolver(store);

        var result = resolver.Resolve("{> not_partial}", "en");

        Assert.Contains("[Missing partial: not_partial]", result);
    }

    [Fact]
    public void ResolveLayout_WithBlockSubstitution()
    {
        var store = CreateStore(
            new MessageTemplate
            {
                Name = "base_layout",
                IsLayout = true,
                Text = new MessageTemplateText { Translations = { ["en"] = "Header\n{block:content}\nFooter" } }
            });
        var resolver = new PartialResolver(store);

        var template = new MessageTemplate
        {
            Name = "page",
            Extends = "base_layout",
            Blocks = new Dictionary<string, MessageTemplateText>
            {
                ["content"] = new() { Translations = { ["en"] = "My Page Content" } }
            }
        };

        var result = resolver.ResolveLayout(template, "en");

        Assert.Contains("Header", result);
        Assert.Contains("My Page Content", result);
        Assert.Contains("Footer", result);
    }

    [Fact]
    public void ResolveLayout_WithoutExtends_ReturnsOwnText()
    {
        var store = CreateStore();
        var resolver = new PartialResolver(store);

        var template = new MessageTemplate
        {
            Name = "standalone",
            Text = new MessageTemplateText { Translations = { ["en"] = "Standalone content" } }
        };

        var result = resolver.ResolveLayout(template, "en");

        Assert.Equal("Standalone content", result);
    }

    [Fact]
    public void ResolveLayout_MissingLayout_ReturnsOwnText()
    {
        var store = CreateStore();
        var resolver = new PartialResolver(store);

        var template = new MessageTemplate
        {
            Name = "orphan",
            Extends = "missing_layout",
            Text = new MessageTemplateText { Translations = { ["en"] = "Fallback" } }
        };

        var result = resolver.ResolveLayout(template, "en");

        Assert.Equal("Fallback", result);
    }
}
