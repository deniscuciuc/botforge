using BotForge.ApiProbe.Core;

namespace BotForge.ApiProbe.Reporting;

public interface IReportWriter
{
    string Format { get; }
    Task WriteAsync(ScenarioResult result, string outputDirectory);
}
