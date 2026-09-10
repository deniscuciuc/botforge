using System.Globalization;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace BotForge.Integration.Tests.Fixtures;

/// <summary>
/// WireMock-based mock of the Telegram Bot API.
/// Provides endpoints for getMe, sendMessage, sendPhoto, etc.
/// </summary>
public class TelegramApiMockFixture : IDisposable
{
    public WireMockServer Server { get; }

    public string BaseUrl => Server.Url!;

    public TelegramApiMockFixture()
    {
        Server = WireMockServer.Start();
        SetupDefaultEndpoints();
    }

    private void SetupDefaultEndpoints()
    {
        // getMe — returns a valid bot user
        Server.Given(Request.Create()
                .WithPath("/bot*/getMe")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    ok = true,
                    result = new
                    {
                        id = 123456789L,
                        is_bot = true,
                        first_name = "TestBot",
                        username = "test_bot",
                        can_join_groups = true,
                        can_read_all_group_messages = false,
                        supports_inline_queries = false
                    }
                })));

        // sendMessage — returns a message object
        Server.Given(Request.Create()
                .WithPath("/bot*/sendMessage")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    ok = true,
                    result = new
                    {
                        message_id = 1,
                        from = new { id = 123456789L, is_bot = true, first_name = "TestBot" },
                        chat = new { id = 100L, type = "private" },
                        date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        text = "ok"
                    }
                })));

        // sendPhoto — returns a message with photo
        Server.Given(Request.Create()
                .WithPath("/bot*/sendPhoto")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    ok = true,
                    result = new
                    {
                        message_id = 2,
                        from = new { id = 123456789L, is_bot = true, first_name = "TestBot" },
                        chat = new { id = 100L, type = "private" },
                        date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        photo = new[]
                        {
                            new
                            {
                                file_id = "photo_1", file_unique_id = "u1", width = 100, height = 100, file_size = 1024
                            }
                        }
                    }
                })));

        // deleteWebhook — always succeeds
        Server.Given(Request.Create()
                .WithPath("/bot*/deleteWebhook")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { ok = true, result = true })));

        // setWebhook — always succeeds
        Server.Given(Request.Create()
                .WithPath("/bot*/setWebhook")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { ok = true, result = true })));

        // getUpdates — returns empty (for polling mode tests)
        Server.Given(Request.Create()
                .WithPath("/bot*/getUpdates")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { ok = true, result = Array.Empty<object>() })));
    }

    /// <summary>
    /// Configures a rate-limit (429) response for sendMessage.
    /// </summary>
    public void SetupRateLimitResponse(int retryAfterSeconds = 5)
    {
        Server.Reset();
        SetupDefaultEndpoints();

        Server.Given(Request.Create()
                .WithPath("/bot*/sendMessage")
                .UsingPost())
            // WireMock resolves lower priority numbers first, and SetupDefaultEndpoints
            // registers its 200 mapping at the default priority of 0. At priority 1 this
            // override never won, so the fixture always returned OK.
            .AtPriority(-1)
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Retry-After", retryAfterSeconds.ToString(CultureInfo.InvariantCulture))
                .WithBody(JsonSerializer.Serialize(new
                {
                    ok = false,
                    error_code = 429,
                    description = "Too Many Requests: retry after " + retryAfterSeconds,
                    parameters = new { retry_after = retryAfterSeconds }
                })));
    }

    public void Dispose()
    {
        Server.Dispose();
        GC.SuppressFinalize(this);
    }
}
