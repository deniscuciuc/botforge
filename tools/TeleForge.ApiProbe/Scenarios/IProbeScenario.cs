using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public interface IProbeScenario
{
    string Name { get; }
    string Description { get; }
    Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct);
}
