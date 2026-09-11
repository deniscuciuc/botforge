using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Reporting;

public interface IReportWriter
{
    string Format { get; }
    Task WriteAsync(ScenarioResult result, string outputDirectory);
}
