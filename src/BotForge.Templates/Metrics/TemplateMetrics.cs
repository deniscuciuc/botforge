using System.Diagnostics;
using System.Diagnostics.Metrics;
using BotForge.Core;

namespace BotForge.Templates.Metrics;

internal static class TemplateMetrics
{
    private const string MeterName = "BotForge.Templates";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> TemplateRenderTotal =
        Meter.CreateCounter<long>("botforge.telegram.templates.render.total", "{render}",
            "Template render operations.");

    private static readonly Histogram<double> TemplateRenderDurationMs =
        Meter.CreateHistogram<double>("botforge.telegram.templates.render.duration", "ms",
            "Template render duration in milliseconds.");

    private static readonly Counter<long> KeyboardBuildTotal =
        Meter.CreateCounter<long>("botforge.telegram.templates.keyboard.build.total", "{build}",
            "Inline keyboard build operations.");

    private static readonly Histogram<double> KeyboardBuildDurationMs =
        Meter.CreateHistogram<double>("botforge.telegram.templates.keyboard.build.duration", "ms",
            "Inline keyboard build duration in milliseconds.");

    public static void RecordRender(string templateName, string language, string status, TimeSpan elapsed)
    {
        if (!TelegramMetricsRuntime.TemplatesEnabled)
            return;

        var tags = new TagList
        {
            { "template", templateName },
            { "language", language },
            { "status", status }
        };

        TemplateRenderTotal.Add(1, tags);
        TemplateRenderDurationMs.Record(elapsed.TotalMilliseconds, tags);
    }

    public static void RecordKeyboardBuild(string templateName, bool hasDynamicButtons, string status, TimeSpan elapsed)
    {
        if (!TelegramMetricsRuntime.TemplatesEnabled)
            return;

        var tags = new TagList
        {
            { "template", templateName },
            { "dynamic", hasDynamicButtons ? "true" : "false" },
            { "status", status }
        };

        KeyboardBuildTotal.Add(1, tags);
        KeyboardBuildDurationMs.Record(elapsed.TotalMilliseconds, tags);
    }
}
