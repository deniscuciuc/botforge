using BotForge.ApiProbe.Core;
using Spectre.Console;

namespace BotForge.ApiProbe.Scenarios;

public interface IProbeScenario
{
    string Name { get; }
    string Description { get; }
    Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct);
}
