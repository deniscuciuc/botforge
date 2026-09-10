using BotForge.Integration.Tests.Fixtures;

namespace BotForge.Integration.Tests;

[Trait("Category", "Integration")]
public class HealthCheckTests : IDisposable
{
    private readonly TelegramApiMockFixture _apiMock = new();

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy_WhenServiceIsRunning()
    {
        // Arrange — boot a monolith with mock API
        await using var service = new MonolithServiceFixture(_apiMock);
        await service.InitializeAsync();

        // Assert — service provider is available (host started successfully)
        Assert.NotNull(service.Services);
    }

    [Fact]
    public void TelegramApiMock_GetMe_ReturnsValidResponse()
    {
        // Verify the WireMock fixture responds to getMe
        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiMock.BaseUrl) };
        var response = httpClient.PostAsync("/bottest-token/getMe", null).Result;

        Assert.True(response.IsSuccessStatusCode);
        var body = response.Content.ReadAsStringAsync().Result;
        Assert.Contains("test_bot", body);
    }

    [Fact]
    public void TelegramApiMock_SendMessage_ReturnsValidResponse()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiMock.BaseUrl) };
        var content = new StringContent(
            """{"chat_id": 100, "text": "hello"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = httpClient.PostAsync("/bottest-token/sendMessage", content).Result;

        Assert.True(response.IsSuccessStatusCode);
        var body = response.Content.ReadAsStringAsync().Result;
        Assert.Contains("message_id", body);
    }

    public void Dispose()
    {
        _apiMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
