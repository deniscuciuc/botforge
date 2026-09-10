# WebhookBot

An ASP.NET Core bot that receives Telegram updates via **webhooks** instead of long polling, demonstrating the
`WebhookUpdateHandler` and webhook configuration.

## Features

- **Webhook Transport** — Receives updates via HTTPS POST instead of polling
- **Secret Token Validation** — Validates `X-Telegram-Bot-Api-Secret-Token` header
- **ASP.NET Core Minimal API** — Uses `MapPost` for the webhook endpoint
- **Health Check Endpoint** — `/health` endpoint for monitoring
- **Command Handling** — `/start`, `/ping`, `/info` commands
- **Text Echo** — Echoes all text messages back

## Bot Commands

| Command  | Description              |
|----------|--------------------------|
| `/start` | Welcome message          |
| `/ping`  | Check bot responsiveness |
| `/info`  | Show bot and server info |

## How It Works

### Webhook Flow

1. Telegram sends HTTPS POST requests to your server at `/api/telegram/webhook/{botId}`
2. The endpoint deserializes the `Update` and validates the secret token header
3. `WebhookUpdateHandler.HandleAsync` enqueues the update into the internal channel
4. The consumer worker picks up the update and routes it through the pipeline

### Key Differences from Polling

| Aspect         | Long Polling              | Webhook                        |
|----------------|---------------------------|--------------------------------|
| Transport      | Bot pulls from Telegram   | Telegram pushes to you         |
| Hosting        | Console app (`Host`)      | Web app (`WebApplication`)     |
| SDK            | `Microsoft.NET.Sdk`       | `Microsoft.NET.Sdk.Web`        |
| HTTPS          | Not required              | Required                       |
| Latency        | Polling interval          | Near real-time                 |
| Infrastructure | No public endpoint needed | Public HTTPS endpoint required |

### Configuration

```csharp
builder.Services.AddTelegramConsumer(consumer =>
{
    consumer.DefaultTransport = UpdateTransport.Webhook;
    consumer.ConfigureWebhook(webhook =>
    {
        webhook.Path = "/api/telegram/webhook/{botId}";
        webhook.SecretToken = "my-secret-token";
        webhook.MaxConnections = 40;
    });
    consumer.EnableHealthChecks();
});
```

Bot transport must also be set to `Webhook`:

```csharp
messaging.AddBot("main", bot =>
{
    bot.Token = "...";
    bot.Transport = UpdateTransport.Webhook;
});
```

## Project Structure

```text
WebhookBot/
├── Program.cs                      # WebApplication setup with webhook endpoint
├── WebhookBot.csproj               # Web SDK project file
├── appsettings.json                # Bot token, secret, HTTPS URL
├── Handlers/
│   └── WebhookHandlers.cs         # /start, /ping, /info, echo
└── README.md
```

## How to Run

### Local Development (with ngrok)

1. Set your bot token and webhook secret in `appsettings.json`
2. Start the app:

    ```bash
   dotnet run --project examples/WebhookBot
   ```

3. In another terminal, expose your local server with [ngrok](https://ngrok.com/):

    ```bash
   ngrok http 5001
   ```

4. Set the webhook URL with Telegram:

    ```bash
   curl -X POST "https://api.telegram.org/bot<YOUR_TOKEN>/setWebhook" \
     -d "url=https://<NGROK_URL>/api/telegram/webhook/main" \
     -d "secret_token=my-secret-token-change-me"
   ```

### Production Deployment

1. Deploy to a server with a valid HTTPS certificate
2. Set the webhook URL to your production domain
3. Set a strong, random webhook secret
4. Configure the `Urls` setting to bind to the correct port

## Key Framework Features Demonstrated

- `UpdateTransport.Webhook` — Webhook transport mode
- `WebhookUpdateHandler` — Processes incoming webhook updates
- `WebhookOptions.SecretToken` — Secret token validation
- `TelegramConsumerOptions.ConfigureWebhook()` — Webhook configuration
- `TelegramConsumerOptions.EnableHealthChecks()` — Health check registration
- `Microsoft.NET.Sdk.Web` — ASP.NET Core web application hosting
