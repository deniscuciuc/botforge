using Microsoft.Extensions.Logging.Abstractions;
using TeleForge.Core;

namespace TeleForge.Templates.Benchmarks;

/// <summary>
/// Deterministic fixture data for benchmarks. All templates and parameters are stable
/// across runs to ensure reproducible measurements.
/// </summary>
public static class BenchmarkFixtures
{
    // ─── Simple template ───────────────────────────────────────────────

    public static MessageTemplate SimpleTemplate()
    {
        return new MessageTemplate
        {
            Name = "greeting",
            Text = Text("Hello, {UserName}! Welcome to {BotName}.")
        };
    }

    public static Dictionary<string, string> SimpleParameters()
    {
        return new Dictionary<string, string>
        {
            ["UserName"] = "Alex",
            ["BotName"] = "TestBot"
        };
    }

    // ─── Conditional template ──────────────────────────────────────────

    public static MessageTemplate ConditionalTemplate()
    {
        return new MessageTemplate
        {
            Name = "status",
            Text = Text(
                "{#if IsVip}⭐ VIP Member{#else}Regular User{/if}\n" +
                "Balance: {Balance}\n" +
                "{#if HasNotifications}You have {NotificationCount} notifications{/if}\n" +
                "{#if Level > 10}High level player!{/if}\n" +
                "{#if Status == active}Account is active{#else}Account is inactive{/if}")
        };
    }

    public static Dictionary<string, string> ConditionalParameters()
    {
        return new Dictionary<string, string>
        {
            ["IsVip"] = "true",
            ["Balance"] = "1500",
            ["HasNotifications"] = "true",
            ["NotificationCount"] = "5",
            ["Level"] = "25",
            ["Status"] = "active"
        };
    }

    // ─── Deeply nested conditionals ────────────────────────────────────

    public static MessageTemplate NestedConditionalTemplate()
    {
        var text = "Start\n";
        for (var i = 0; i < 5; i++)
            text += $"{{#if Cond{i}}}Block {i}: {{#if Inner{i}}}inner-true{{#else}}inner-false{{/if}}{{/if}}\n";
        text += "End";

        return new MessageTemplate { Name = "nested_cond", Text = Text(text) };
    }

    public static Dictionary<string, string> NestedConditionalParameters()
    {
        var p = new Dictionary<string, string>();
        for (var i = 0; i < 5; i++)
        {
            p[$"Cond{i}"] = i % 2 == 0 ? "true" : "false";
            p[$"Inner{i}"] = i % 3 == 0 ? "true" : "false";
        }

        return p;
    }

    // ─── Formatter-heavy template ──────────────────────────────────────

    public static MessageTemplate FormatterTemplate()
    {
        return new MessageTemplate
        {
            Name = "stats",
            Text = Text(
                "Revenue: {Revenue | number}\n" +
                "Growth: {Growth | percent}\n" +
                "Description: {Description | truncate:50}\n" +
                "Name: {Name | upper}\n" +
                "Tag: {Tag | lower}\n" +
                "Created: {CreatedAt | date:yyyy-MM-dd}\n" +
                "Duration: {PlayTime | duration}\n" +
                "Progress: {Progress | progressbar:20}\n" +
                "Items: {ItemCount | plural:предмет,предмета,предметов}")
        };
    }

    public static Dictionary<string, string> FormatterParameters()
    {
        return new Dictionary<string, string>
        {
            ["Revenue"] = "1234567.89",
            ["Growth"] = "0.156",
            ["Description"] = "This is a fairly long description that should be truncated at some point",
            ["Name"] = "denis cuciuc",
            ["Tag"] = "PREMIUM_USER",
            ["CreatedAt"] = "2025-06-15T10:30:00Z",
            ["PlayTime"] = "7890",
            ["Progress"] = "0.73",
            ["ItemCount"] = "42"
        };
    }

    // ─── Loop template ─────────────────────────────────────────────────

    public static MessageTemplate LoopTemplate()
    {
        return new MessageTemplate
        {
            Name = "leaderboard",
            Text = Text(
                "🏆 Leaderboard\n\n" +
                "{#each players}" +
                "{Index}. {Name} — {Score} pts" +
                "{#if IsFirst} 👑{/if}\n" +
                "{/each}")
        };
    }

    public static IDictionary<string, IReadOnlyList<IDictionary<string, string>>> LoopData(int count)
    {
        var items = new List<IDictionary<string, string>>(count);
        for (var i = 0; i < count; i++)
            items.Add(new Dictionary<string, string>
            {
                ["Name"] = $"Player_{i + 1}",
                ["Score"] = (1000 - i * 10).ToString()
            });
        return new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["players"] = items
        };
    }

    // ─── Partial / Layout template ─────────────────────────────────────

    public static InMemoryMessageTemplateStore PartialStore()
    {
        var store = new InMemoryMessageTemplateStore();

        store.AddOrReplace(new MessageTemplate
        {
            Name = "base_layout",
            IsLayout = true,
            Text = Text("📋 {block:header}\n\n{block:content}\n\n— {block:footer}")
        });

        store.AddOrReplace(new MessageTemplate
        {
            Name = "_header_partial",
            IsPartial = true,
            Text = Text("{{emoji:star}} {Title}")
        });

        store.AddOrReplace(new MessageTemplate
        {
            Name = "_footer_partial",
            IsPartial = true,
            Text = Text("Powered by {BotName}")
        });

        store.AddOrReplace(new MessageTemplate
        {
            Name = "page_with_layout",
            Extends = "base_layout",
            Blocks = new Dictionary<string, MessageTemplateText>
            {
                ["header"] = Text("{> _header_partial}"),
                ["content"] = Text("Welcome, {UserName}! Here is your dashboard."),
                ["footer"] = Text("{> _footer_partial}")
            },
            Text = Text("")
        });

        return store;
    }

    // ─── Parameter-heavy template ──────────────────────────────────────

    public static MessageTemplate ParameterHeavyTemplate(int paramCount)
    {
        var parts = new string[paramCount];
        for (var i = 0; i < paramCount; i++)
            parts[i] = $"Field{i}: {{Param{i}}}";

        return new MessageTemplate
        {
            Name = "param_heavy",
            Text = Text(string.Join("\n", parts))
        };
    }

    public static Dictionary<string, string> ParameterHeavyData(int paramCount)
    {
        var p = new Dictionary<string, string>(paramCount);
        for (var i = 0; i < paramCount; i++)
            p[$"Param{i}"] = $"Value_{i}_{new string('x', 20)}";

        return p;
    }

    // ─── Large template (stress) ───────────────────────────────────────

    public static MessageTemplate LargeTemplate()
    {
        var sections = new List<string>(20);
        for (var s = 0; s < 20; s++)
            sections.Add(
                $"== Section {s} ==\n" +
                $"{{#if Show{s}}}" +
                $"{{Name{s}}} — {{Value{s} | number}}\n" +
                $"{{Description{s} | truncate:80}}\n" +
                "{/if}");

        sections.Add(
            "{#each items}" +
            "{Index}. {ItemName} — {ItemPrice | number}\n" +
            "{/each}");

        return new MessageTemplate
        {
            Name = "large_stress",
            Text = Text(string.Join("\n\n", sections))
        };
    }

    public static Dictionary<string, string> LargeTemplateParameters()
    {
        var p = new Dictionary<string, string>();
        for (var s = 0; s < 20; s++)
        {
            p[$"Show{s}"] = s % 3 != 0 ? "true" : "false";
            p[$"Name{s}"] = $"Section_{s}_Name";
            p[$"Value{s}"] = (s * 12345.67m).ToString();
            p[$"Description{s}"] = new string('A', 100);
        }

        return p;
    }

    public static IDictionary<string, IReadOnlyList<IDictionary<string, string>>> LargeTemplateLoopData()
    {
        var items = new List<IDictionary<string, string>>(50);
        for (var i = 0; i < 50; i++)
            items.Add(new Dictionary<string, string>
            {
                ["ItemName"] = $"Product_{i}",
                ["ItemPrice"] = (i * 99.99m).ToString()
            });
        return new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
        {
            ["items"] = items
        };
    }

    // ─── Keyboard fixtures ─────────────────────────────────────────────

    public static MessageTemplate KeyboardTemplate()
    {
        return new MessageTemplate
        {
            Name = "shop_menu",
            Text = Text("Select an option:"),
            Buttons =
            [
                new MessageTemplateButton { Text = Text("📦 Inventory"), Value = "inventory", Row = 0, Order = 0 },
                new MessageTemplateButton { Text = Text("🛒 Shop"), Value = "shop", Row = 0, Order = 1 },
                new MessageTemplateButton { Text = Text("⚙️ Settings"), Value = "settings", Row = 1, Order = 0 },
                new MessageTemplateButton { Text = Text("❓ Help"), Value = "help", Row = 1, Order = 1 },
                new MessageTemplateButton
                    { Text = Text("💎 VIP"), Value = "vip", Row = 2, Order = 0, ShowIf = "IsVip" },
                new MessageTemplateButton
                    { Text = Text("🎁 Bonus"), Value = "bonus", Row = 2, Order = 1, ShowIf = "HasBonus" }
            ],
            DynamicButtons =
            [
                new MessageTemplateDynamicButton
                {
                    Name = "products",
                    Text = Text("{ProductName} — {ProductPrice}"),
                    Value = "buy:{ProductId}",
                    RowStart = 3,
                    ItemsPerRow = 2,
                    ItemsPerPage = 6,
                    PaginationRow = 10,
                    PaginationCallbackPrefix = "page",
                    PaginationButtons = new MessageTemplatePaginationButtons
                    {
                        Previous = new Dictionary<string, string> { ["en"] = "◀️ Prev" },
                        Next = new Dictionary<string, string> { ["en"] = "Next ▶️" },
                        Counter = "{Page}/{TotalPages}"
                    }
                }
            ]
        };
    }

    public static List<KeyboardItemData> KeyboardDynamicItems(int count)
    {
        var items = new List<KeyboardItemData>(count);
        for (var i = 0; i < count; i++)
            items.Add(KeyboardItemData.FromValues(
                ("ProductId", (i + 1).ToString()),
                ("ProductName", $"Item_{i + 1}"),
                ("ProductPrice", $"${(i + 1) * 9.99m:F2}")));
        return items;
    }

    // ─── YAML startup fixtures ─────────────────────────────────────────

    public static string GenerateYamlTemplates(int count)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < count; i++)
        {
            sb.AppendLine($"- Name: template_{i}");
            sb.AppendLine($"  Category: bench");
            sb.AppendLine($"  Text:");
            sb.AppendLine($"    Translations:");
            sb.AppendLine($"      en: \"Hello {{UserName}}, this is template {i}\"");
            sb.AppendLine($"      ru: \"Привет {{UserName}}, это шаблон {i}\"");
        }

        return sb.ToString();
    }

    // ─── Emoji fixtures ────────────────────────────────────────────────

    public static EmojiRegistry CreateEmojiRegistry()
    {
        var registry = new EmojiRegistry();
        registry.Load(new Dictionary<string, string>
        {
            ["star"] = "⭐",
            ["fire"] = "🔥",
            ["check"] = "✅",
            ["cross"] = "❌",
            ["trophy"] = "🏆",
            ["gem"] = "💎",
            ["rocket"] = "🚀",
            ["heart"] = "❤️",
            ["warning"] = "⚠️",
            ["info"] = "ℹ️"
        });
        return registry;
    }

    // ─── Factory helpers ───────────────────────────────────────────────

    public static TemplateRenderer CreateRenderer(InMemoryMessageTemplateStore store, EmojiRegistry? emojis = null)
    {
        emojis ??= CreateEmojiRegistry();
        var conditionals = new ConditionalEvaluator();
        var loops = new LoopProcessor();
        var partials = new PartialResolver(store);
        var formatters = new FormatterPipeline([]);
        var options = new TelegramTemplateOptions();
        var logger = NullLogger<TemplateRenderer>.Instance;

        return new TemplateRenderer(store, emojis, conditionals, loops, partials, formatters, options, logger);
    }

    public static KeyboardBuilder CreateKeyboardBuilder(EmojiRegistry? emojis = null)
    {
        emojis ??= CreateEmojiRegistry();
        var conditionals = new ConditionalEvaluator();
        return new KeyboardBuilder(emojis, conditionals);
    }

    private static MessageTemplateText Text(string en)
    {
        return new MessageTemplateText
        {
            Translations = new Dictionary<string, string> { ["en"] = en }
        };
    }
}
