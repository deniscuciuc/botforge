# MediaBot

Demonstrates handling different media types (photos, documents, voice messages, videos, stickers, GIFs).

## Features

- **Photo handling** — receives photos, reports file ID and caption presence
- **Document handling** — processes documents with file name display
- **Voice messages** — handles voice messages with duration info
- **Video handling** — processes videos with duration info
- **Sticker handling** — receives stickers and shows associated emoji
- **Animation/GIF handling** — processes GIF animations

## How It Works

Each media type has a dedicated handler decorated with `[MediaMessage(MediaType)]`:

```csharp
[MediaMessage(MediaType.Photo)]
public class PhotoHandler(ITelegramMessageService messages) : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        // context.FileId, context.Caption, context.FileName, etc.
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId)
            .WithHtml("📷 <b>Photo received!</b>...")
            .SendAsync(ct);
    }
}
```

The `MediaContext` provides:

- `FileId` — Telegram file identifier
- `Caption` — optional media caption
- `FileName` — file name (for documents)
- `Duration` — duration in seconds (for voice/video)
- `Emoji` — associated emoji (for stickers)
- `MediaType` — the detected media type

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather)
2. Update `appsettings.json` with your bot token
3. Run:

    ```bash
   dotnet run
   ```

4. Send different media types to the bot and observe the responses

## Project Structure

```text
MediaBot/
├── Program.cs
├── Handlers/
│   └── MediaHandlers.cs   # Photo, Document, Voice, Video, Sticker, Animation handlers
├── appsettings.json
└── MediaBot.csproj
```
