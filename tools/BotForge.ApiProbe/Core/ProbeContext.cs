using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using BotForge.ApiProbe.Configuration;
using Spectre.Console;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotForge.ApiProbe.Core;

public sealed class ProbeContext
{
    private readonly ConcurrentBag<int> _sentMessageIds = [];

    public required IReadOnlyDictionary<string, TelegramBotClient> Bots { get; init; }
    public required ProbeConfiguration Config { get; init; }

    public TelegramBotClient PrimaryBot => Bots.Values.First();
    public string PrimaryBotName => Bots.Keys.First();

    public void TrackMessageId(int messageId)
    {
        _sentMessageIds.Add(messageId);
    }

    public IReadOnlyCollection<int> SentMessageIds => _sentMessageIds;

    public static async Task<ProbeContext> CreateAsync(
        ProbeConfiguration config,
        IAnsiConsole console,
        CancellationToken ct)
    {
        if (config.Bots.Count == 0)
            throw new InvalidOperationException("At least one bot must be configured in Probe:Bots.");

        var bots = new Dictionary<string, TelegramBotClient>();

        foreach (var entry in config.Bots)
        {
            if (string.IsNullOrWhiteSpace(entry.Token) || entry.Token == "YOUR_BOT_TOKEN_HERE")
            {
                console.MarkupLine($"[red]Bot '{entry.Name}' has no valid token. Set it in appsettings.json.[/]");
                throw new InvalidOperationException($"Invalid token for bot '{entry.Name}'.");
            }

            var client = new TelegramBotClient(entry.Token);

            try
            {
                var me = await client.GetMe(ct);
                console.MarkupLine($"[green]✓[/] Bot [bold]{entry.Name}[/] → @{me.Username} (id: {me.Id})");
                bots[entry.Name] = client;
            }
            catch (Exception ex)
            {
                console.MarkupLine($"[red]✗[/] Bot '{entry.Name}' failed GetMe: {ex.Message}");
                throw;
            }
        }

        return new ProbeContext
        {
            Bots = bots,
            Config = config
        };
    }

    public async Task<RequestRecord> SendProbeMessageAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var text = $"[Probe #{index} @ {timestamp:HH:mm:ss.fff}]";
        var sw = Stopwatch.StartNew();

        try
        {
            var msg = await bot.SendMessage(chatId, text, cancellationToken: ct);
            sw.Stop();

            TrackMessageId(msg.Id);

            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.SendMessage
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            var retryAfter = apiEx.Parameters?.RetryAfter;

            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = retryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName
            };
        }
    }

    public async Task<RequestRecord> EditProbeMessageAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        int messageId,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var text = $"[Edit #{index} @ {timestamp:HH:mm:ss.fff}]";
        var sw = Stopwatch.StartNew();

        try
        {
            await bot.EditMessageText(chatId, messageId, text, cancellationToken: ct);
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = messageId,
                ApiMethod = ApiMethod.EditMessage
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.EditMessage
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.EditMessage
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.EditMessage
            };
        }
    }

    public async Task<RequestRecord> SendDiceAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        int index,
        CancellationToken ct,
        string? emoji = null)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            var msg = await bot.SendDice(chatId, emoji, cancellationToken: ct);
            sw.Stop();
            TrackMessageId(msg.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.SendDice
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendDice
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendDice
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendDice
            };
        }
    }

    public async Task<RequestRecord> DeleteProbeMessageAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        int messageId,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            await bot.DeleteMessage(chatId, messageId, ct);
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = messageId,
                ApiMethod = ApiMethod.DeleteMessage
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.DeleteMessage
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.DeleteMessage
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.DeleteMessage
            };
        }
    }

    public async Task<RequestRecord> ForwardProbeMessageAsync(
        TelegramBotClient bot,
        string botName,
        long fromChatId,
        long toChatId,
        int messageId,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            var msg = await bot.ForwardMessage(toChatId, fromChatId, messageId, cancellationToken: ct);
            sw.Stop();
            TrackMessageId(msg.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = toChatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.ForwardMessage
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = toChatId,
                BotName = botName,
                ApiMethod = ApiMethod.ForwardMessage
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = toChatId,
                BotName = botName,
                ApiMethod = ApiMethod.ForwardMessage
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = toChatId,
                BotName = botName,
                ApiMethod = ApiMethod.ForwardMessage
            };
        }
    }

    public async Task<RequestRecord> SendPhotoByStreamAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        byte[] photoBytes,
        string? caption,
        long payloadBytes,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            using var photoStream = new MemoryStream(photoBytes, false);
            var msg = await bot.SendPhoto(chatId, InputFile.FromStream(photoStream, "probe.jpg"),
                caption, cancellationToken: ct);
            sw.Stop();
            TrackMessageId(msg.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.SendPhoto,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto,
                PayloadBytes = payloadBytes
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto,
                PayloadBytes = payloadBytes
            };
        }
    }

    public async Task<RequestRecord> SendPhotoByUrlAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        string url,
        string? caption,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            var msg = await bot.SendPhoto(chatId, InputFile.FromUri(url),
                caption, cancellationToken: ct);
            sw.Stop();
            TrackMessageId(msg.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
    }

    public async Task<RequestRecord> SendPhotoByFileIdAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        string fileId,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            var msg = await bot.SendPhoto(chatId, InputFile.FromFileId(fileId),
                cancellationToken: ct);
            sw.Stop();
            TrackMessageId(msg.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendPhoto
            };
        }
    }

    public async Task<RequestRecord> SendDocumentByStreamAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        byte[] docBytes,
        string fileName,
        long payloadBytes,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            using var docStream = new MemoryStream(docBytes, false);
            var msg = await bot.SendDocument(chatId, InputFile.FromStream(docStream, fileName),
                cancellationToken: ct);
            sw.Stop();
            TrackMessageId(msg.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.SendDocument,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendDocument,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendDocument,
                PayloadBytes = payloadBytes
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendDocument,
                PayloadBytes = payloadBytes
            };
        }
    }

    public async Task<RequestRecord> SendCustomTextAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        string text,
        int index,
        CancellationToken ct,
        string? parseMode = null,
        InlineKeyboardMarkup? replyMarkup = null)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();
        var payloadBytes = (long)System.Text.Encoding.UTF8.GetByteCount(text);

        try
        {
            var pm = parseMode switch
            {
                "Markdown" => ParseMode.MarkdownV2,
                "HTML" => ParseMode.Html,
                _ => (ParseMode?)null
            };
            var msg = await bot.SendMessage(chatId, text,
                pm ?? default,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            sw.Stop();
            TrackMessageId(msg.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = msg.Id,
                ApiMethod = ApiMethod.SendMessage,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMessage,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMessage,
                PayloadBytes = payloadBytes
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMessage,
                PayloadBytes = payloadBytes
            };
        }
    }

    public async Task<RequestRecord> SendMediaGroupAsync(
        TelegramBotClient bot,
        string botName,
        long chatId,
        IEnumerable<IAlbumInputMedia> album,
        long payloadBytes,
        int index,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            var messages = await bot.SendMediaGroup(chatId, album, cancellationToken: ct);
            sw.Stop();
            foreach (var m in messages) TrackMessageId(m.Id);
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                MessageId = messages.FirstOrDefault()?.Id,
                ApiMethod = ApiMethod.SendMediaGroup,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMediaGroup,
                PayloadBytes = payloadBytes
            };
        }
        catch (ApiRequestException apiEx)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                IsRateLimited = false,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMediaGroup,
                PayloadBytes = payloadBytes
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.InternalServerError,
                IsRateLimited = false,
                ErrorMessage = ex.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMediaGroup,
                PayloadBytes = payloadBytes
            };
        }
    }

    public async Task CleanupAsync(long chatId, IAnsiConsole console, CancellationToken ct)
    {
        var ids = _sentMessageIds.ToList();
        if (ids.Count == 0) return;

        console.MarkupLine($"[yellow]Cleaning up {ids.Count} messages...[/]");

        var deleted = 0;
        var failed = 0;

        foreach (var chunk in ids.Chunk(30))
        {
            var tasks = chunk.Select(async msgId =>
            {
                try
                {
                    await PrimaryBot.DeleteMessage(chatId, msgId, ct);
                    Interlocked.Increment(ref deleted);
                }
                catch
                {
                    Interlocked.Increment(ref failed);
                }
            });
            await Task.WhenAll(tasks);
            await Task.Delay(1100, ct); // respect rate limits during cleanup
        }

        console.MarkupLine($"[green]Deleted {deleted}[/], [red]failed {failed}[/]");
    }
}
