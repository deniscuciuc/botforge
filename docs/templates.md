# Templates

## Overview

The template engine provides YAML-based message templates with inline translations, optional external localization key resolution, mustache-like syntax, partials, loops, conditionals, formatters, emoji injection, dynamic keyboards with pagination, and layout inheritance.

## Registration

```csharp
builder.Services.AddTelegramTemplates(options =>
{
    options.DefaultLanguage = "en";
    options.FallbackLanguage = "en";
    options.AddDirectory("Templates");
    options.AddDirectory("Templates/Partials");
    options.HotReload = true;       // Enable file watching
    options.PreCompile = true;      // Pre-compile at startup
    options.AddFormatter<MyCustomFormatter>();
});
```

### TelegramTemplateOptions

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `Directories` | `List<string>` | `["Templates"]` | Directories to scan for YAML files |
| `DefaultLanguage` | `string` | `"ru"` | Default rendering language |
| `FallbackLanguage` | `string` | `"en"` | Fallback when translation is missing |
| `EmojiFile` | `string?` | `null` | Dedicated emoji YAML file path |
| `HotReload` | `bool` | `false` | Watch for file changes |
| `PreCompile` | `bool` | `false` | Pre-compile templates at startup |

## External Localization

If you want templates to read strings from `TeleForge.Localization` instead of duplicating per-language text blocks, add the bridge package and register it after `AddTelegramTemplates()`:

```csharp
builder.Services.AddTelegramTemplates(options =>
{
  options.AddDirectory("Templates");
});

builder.Services.AddTelegramTemplateLocalization();
```

With the bridge enabled:

- `Text.LocalizationKey` resolves the entire template body from `ILocalizer`
- `Text.LocalizationKey` on buttons resolves button captions from `ILocalizer`
- `{@key}` resolves inline localization tokens inside template text
- `{@key | plural:Count}` resolves pluralized tokens using the `count` parameter

## YAML Template Format

```yaml
Name: welcome
Category: common
ParseMode: Html
Text:
  Translations:
    en: "Hello, <b>{UserName}</b>! {{emoji:wave}}"
    ru: "Привет, <b>{UserName}</b>! {{emoji:wave}}"
Buttons:
  - Text:
      Translations:
        en: "{{emoji:gear}} Settings"
        ru: "{{emoji:gear}} Настройки"
    Type: Callback
    Value: "settings"
    Style: Primary
    Row: 0
    Order: 0
  - Text:
      Translations:
        en: "Help"
        ru: "Помощь"
    Type: Callback
    Value: "help"
    Row: 0
    Order: 1
    ShowIf: "IsNewUser"
```

When using external localization, you can replace inline translations with a localization key:

```yaml
Name: welcome
ParseMode: Html
Text:
  LocalizationKey: "telegram.welcome.body"
Buttons:
  - Text:
      LocalizationKey: "telegram.welcome.settings"
      Translations:
        en: "Settings"
    Type: Callback
    Value: "settings"
    Row: 0
    Order: 0
```

Or mix inline text with referenced keys:

```yaml
Text:
  Translations:
    en: "{@telegram.greeting} <b>{UserName}</b>"
```

### MessageTemplate Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Name` | `string` | Unique template name |
| `Category` | `string?` | Optional grouping |
| `ParseMode` | `TelegramParseMode` | `Html` (default), `Markdown`, `MarkdownV2`, `None` |
| `IsPartial` | `bool` | If true, can be included via `{> name}` |
| `IsLayout` | `bool` | If true, used as a layout base |
| `Extends` | `string?` | Layout template to inherit from |
| `Text` | `MessageTemplateText` | Template text with `Translations` and optional `LocalizationKey` |
| `Buttons` | `List<MessageTemplateButton>` | Static buttons |
| `DynamicButtons` | `List<MessageTemplateDynamicButton>` | Dynamic paginated buttons |
| `Blocks` | `Dictionary<string, MessageTemplateText>?` | Layout block overrides |

## Template Syntax

### Variable Substitution

```text
Hello, {UserName}! You have {Balance} stars.
```

### Emoji Injection

```text
{{emoji:star}} Your rating: {Rating}
{{emoji:check}} Task completed
```

### Partials

Include reusable template fragments:

```text
{> header}
Your content here...
{> footer}
```

The referenced template must have `IsPartial: true`. Circular references are detected and prevented.

### Conditionals

```text
{#if IsVip}
  Welcome, VIP member!
{#else}
  Upgrade to VIP for exclusive features.
{/if}
```

**Supported operators:**

| Operator | Example | Description |
| -------- | ------- | ----------- |
| truthy | `{#if Name}` | Non-empty, not "false", not "0" |
| negation | `{#if !Name}` | Falsy check |
| `exists` | `{#if Name exists}` | Key exists in parameters |
| `!exists` | `{#if Name !exists}` | Key does not exist |
| `==` | `{#if Role == admin}` | String equality |
| `!=` | `{#if Status != banned}` | String inequality |
| `>` `>=` `<` `<=` | `{#if Score > 100}` | Numeric comparison |
| `in` | `{#if Role in admin,mod}` | Value in comma-separated list |
| `contains` | `{#if Tags contains vip}` | Substring match |
| `&&` | `{#if IsVip && HasPaid}` | Logical AND |
| `\|\|` | `{#if IsAdmin \|\| IsMod}` | Logical OR |

Conditionals support nesting — inner blocks are evaluated first.

### Loops

```text
{#each products}
  {Index}. {Name} — {Price} ⭐
{/each}
```

**Loop variables:**

| Variable | Description |
| -------- | ----------- |
| `{Index}` | 1-based index |
| `{Index0}` | 0-based index |
| `{IsFirst}` | `"true"` for first item |
| `{IsLast}` | `"true"` for last item |
| `{Count}` | Total items |

Loop data is passed via the rendering API:

```csharp
var loopData = new Dictionary<string, IReadOnlyList<IDictionary<string, string>>>
{
    ["products"] = products.Select(p => new Dictionary<string, string>
    {
        ["Name"] = p.Name,
        ["Price"] = p.Price.ToString()
    } as IDictionary<string, string>).ToList()
};

string rendered = renderer.Render("catalog", parameters, "en", loopData);
```

### Formatters (Pipes)

```text
Balance: {Balance | number:N2}
Progress: {Ratio | progressbar:10}
Created: {Date | date:yyyy-MM-dd}
```

**Built-in formatters:**

| Name | Syntax | Example Output |
| ---- | ------ | -------------- |
| `number` | `{X \| number}` or `{X \| number:N2}` | `1,234.56` |
| `percent` | `{X \| percent}` or `{X \| percent:P2}` | `85.50%` |
| `truncate` | `{X \| truncate:50}` | `Long text...` |
| `upper` | `{X \| upper}` | `HELLO` |
| `lower` | `{X \| lower}` | `hello` |
| `date` | `{X \| date}` or `{X \| date:yyyy-MM-dd}` | `2025-01-15` |
| `duration` | `{X \| duration}` | `2д 3ч 15м 0с` |
| `progressbar` | `{X \| progressbar:10}` | `[████░░░░░░]` |
| `plural` | `{X \| plural:apple,apples,apples}` | `5 apples` |

### Custom Formatters

```csharp
public class CurrencyFormatter : ITemplateFormatter
{
    public string Name => "currency";

    public string Format(string value, string? args, CultureInfo culture)
    {
        var amount = decimal.Parse(value, culture);
        var symbol = args ?? "$";
        return $"{symbol}{amount:N2}";
    }
}

// Register
options.AddFormatter<CurrencyFormatter>();
```

See [Metrics and Observability](metrics.md) for template render and keyboard metrics.

## Layout Inheritance

Define a base layout:

```yaml
Name: base_layout
IsLayout: true
Text:
  Translations:
    en: |
      === {AppName} ===
      {block:content}
      ——————————————
      {block:footer}
```

Extend it in a child template:

```yaml
Name: dashboard
Extends: base_layout
Blocks:
  content:
    Translations:
      en: "Welcome back, {UserName}!"
  footer:
    Translations:
      en: "Last login: {LastLogin | date}"
```

## Keyboards

### Static Buttons

```yaml
Buttons:
  - Text:
      Translations:
        en: "✅ Accept"
    Type: Callback    # Callback | Url | WebApp | Login | SwitchInline | SwitchInlineCurrentChat | Pay
    Value: "accept:{ItemId}"
    Style: Success    # Primary | Success | Danger
    IconCustomEmojiId: "5395601891781224729"
    Row: 0
    Order: 0
    ShowIf: "CanAccept"   # Conditional display
```

Supported inline button fields:

| Field | Type | Description |
| ----- | ---- | ----------- |
| `Type` | `TelegramButtonType` | Button behavior: `Callback`, `Url`, `WebApp`, `Login`, `SwitchInline`, `SwitchInlineCurrentChat`, `Pay` |
| `Value` | `string` | Callback payload, URL, or Web App URL depending on `Type`; ignored for `Pay` |
| `Style` | `TelegramButtonStyle?` | Optional visual style: `Primary`, `Success`, `Danger` |
| `IconCustomEmojiId` | `string?` | Optional premium/custom emoji shown before the button text |
| `LoginForwardText` | `string?` | Optional `Login` button forward label |
| `LoginBotUsername` | `string?` | Optional `Login` button bot username override |
| `LoginRequestWriteAccess` | `bool` | Optional `Login` button write-access request |

Notes:

- `WebApp` and `Login` buttons require a non-empty `Value`.
- `Pay` buttons ignore `Value` and are only valid in invoice messages.
- `IconCustomEmojiId` requires Telegram support and the bot/account capabilities described by Telegram Bot API.

### Dynamic Buttons with Pagination

```yaml
DynamicButtons:
  - Name: items
    Text:
      Translations:
        en: "{ItemName}"
    Type: Callback
    Value: "item:{ItemId}"
    Style: Primary
    IconCustomEmojiId: "{ItemEmojiId}"
    RowStart: 1
    ItemsPerRow: 2
    ItemsPerPage: 5
    PaginationCallbackPrefix: "items_page"
    PaginationRow: 10
    PaginationButtons:
      Previous: { en: "◀️", ru: "◀️" }
      Next: { en: "▶️", ru: "▶️" }
      Counter: "{Page}/{TotalPages}"
```

Pass dynamic data when sending:

```csharp
await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithTemplate("catalog")
    .WithParameters(parameters)
    .WithLanguage("en")
    .WithKeyboardItems("items", products.Select(p =>
        KeyboardItemData.FromValues(
            ("ItemId", p.Id.ToString()),
            ("ItemName", p.Name)
        )).ToList(), page: 1)
    .SendAsync(ct);
```

## Emoji Registry

### YAML Emoji File

```yaml
Emojis:
  star: "⭐"
  fire: "🔥"
  check: "✅"
  cross: "❌"
  wave: "👋"
  gear: "⚙️"
```

### Programmatic Registration

```csharp
var emojiRegistry = serviceProvider.GetRequiredService<IEmojiRegistry>();
emojiRegistry.Register("rocket", "🚀");
emojiRegistry.Load(new Dictionary<string, string>
{
    ["star"] = "⭐",
    ["fire"] = "🔥"
});
```

### Usage in Templates

```text
{{emoji:star}} Rating: {Rating}/5
```

Missing emojis are left as-is (`{{emoji:unknown}}`).

When `ParseMode: Html` is used, the emoji registry can also serve Telegram custom emoji tags directly. This allows premium/custom emoji in template text without changing the `{{emoji:name}}` syntax:

```yaml
Emojis:
  premium_star: '<tg-emoji emoji-id="5395601891781224729">⭐</tg-emoji>'
```

```yaml
Text:
  Translations:
    en: "{{emoji:premium_star}} Premium unlocked"
```

## Programmatic Template Store

```csharp
var store = serviceProvider.GetRequiredService<IMessageTemplateStore>();

store.AddOrReplace(new MessageTemplate
{
    Name = "greeting",
    Text = new MessageTemplateText
    {
        Translations = new Dictionary<string, string>
        {
            ["en"] = "Hello, {UserName}!",
            ["ru"] = "Привет, {UserName}!"
        }
    }
});
```

## Rendering Pipeline

Templates are processed in this order:

1. **Layout resolution** — `Extends` → block substitution
2. **Partial resolution** — `{> partial_name}` → recursive inclusion
3. **Emoji resolution** — `{{emoji:name}}` → emoji characters
4. **Loop processing** — `{#each}...{/each}` → expanded items
5. **Conditional processing** — `{#if}...{/if}` → evaluated blocks
6. **Formatter processing** — `{Param | formatter:args}` → formatted values
7. **Localization resolution** — `LocalizationKey` and `{@key}` tokens resolved via `ILocalizationKeyResolver`
8. **Parameter substitution** — `{Param}` → values
9. **Cleanup** — consecutive empty lines collapsed

## Using Templates with Messages

```csharp
// Template-based message
await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithTemplate("welcome")
    .WithParameter("UserName", "Alice")
    .WithParameter("Balance", "100")
    .WithLanguage("en")
    .SendAsync(ct);

// Direct text (no template)
await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithHtml("<b>Hello!</b>")
    .SendAsync(ct);
```
