using BotForge.Integration.Tests.Fixtures;

namespace BotForge.Integration.Tests;

[Trait("Category", "Integration")]
public class TelegramApiMockTests : IDisposable
{
    private readonly TelegramApiMockFixture _apiMock = new();

    [Fact]
    public void Mock_SetupRateLimitResponse_Returns429()
    {
        _apiMock.SetupRateLimitResponse(10);

        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiMock.BaseUrl) };
        var content = new StringContent(
            """{"chat_id": 100, "text": "hello"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = httpClient.PostAsync("/bottest-token/sendMessage", content).Result;

        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("10", response.Headers.GetValues("Retry-After").First());
    }

    [Fact]
    public void Mock_DeleteWebhook_Succeeds()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiMock.BaseUrl) };
        var response = httpClient.PostAsync("/bottest-token/deleteWebhook", null).Result;

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public void Mock_GetUpdates_ReturnsEmptyArray()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiMock.BaseUrl) };
        var response = httpClient.PostAsync("/bottest-token/getUpdates", null).Result;

        Assert.True(response.IsSuccessStatusCode);
        var body = response.Content.ReadAsStringAsync().Result;
        Assert.Contains("\"result\":[]", body);
    }

    public void Dispose()
    {
        _apiMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
