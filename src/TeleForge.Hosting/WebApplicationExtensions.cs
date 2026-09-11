using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TeleForge.Consumer.Webhook;
using Telegram.Bot.Types;

namespace TeleForge.Hosting;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps the standard Telegram webhook endpoint: POST /api/telegram/webhook/{botId}.
    /// Validates the <c>X-Telegram-Bot-Api-Secret-Token</c> header and enqueues the update.
    /// </summary>
    public static IEndpointRouteBuilder MapTelegramWebhookEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/api/telegram/webhook/{botId}")
    {
        endpoints.MapPost(pattern, async (
            string botId,
            HttpRequest request,
            WebhookUpdateHandler handler,
            CancellationToken ct) =>
        {
            var secretHeader = request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
            Update? update;

            try
            {
                update = await JsonSerializer.DeserializeAsync<Update>(
                    request.Body,
                    cancellationToken: ct).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return Results.BadRequest();
            }

            if (update is null)
                return Results.BadRequest();

            await handler.HandleAsync(botId, update, secretHeader, ct).ConfigureAwait(false);
            return Results.Ok();
        });

        return endpoints;
    }

    /// <summary>
    /// Maps standard health check endpoints: <c>/healthz</c> (liveness) and <c>/readyz</c> (readiness).
    /// </summary>
    public static IEndpointRouteBuilder MapTelegramHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/healthz", async (HealthCheckService healthCheckService, CancellationToken ct) =>
        {
            var result = await healthCheckService.CheckHealthAsync(ct).ConfigureAwait(false);
            return result.Status == HealthStatus.Healthy
                ? Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow })
                : Results.Json(
                    new { status = result.Status.ToString().ToLowerInvariant(), timestamp = DateTimeOffset.UtcNow },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        endpoints.MapGet("/readyz", async (HealthCheckService healthCheckService, CancellationToken ct) =>
        {
            var result = await healthCheckService.CheckHealthAsync(
                r => r.Tags.Contains("ready"),
                ct).ConfigureAwait(false);
            return result.Status == HealthStatus.Healthy
                ? Results.Ok(new { status = "ready", timestamp = DateTimeOffset.UtcNow })
                : Results.Json(
                    new { status = "not-ready", timestamp = DateTimeOffset.UtcNow },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        return endpoints;
    }
}
