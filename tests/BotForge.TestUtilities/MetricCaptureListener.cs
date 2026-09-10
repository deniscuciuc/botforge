using System.Diagnostics.Metrics;
using System.Globalization;

namespace BotForge.TestUtilities;

public sealed class MetricCaptureListener : IDisposable
{
    private readonly HashSet<string> _meterNames;
    private readonly MeterListener _listener;
    private readonly object _sync = new();
    private readonly List<MetricMeasurement> _measurements = [];

    public MetricCaptureListener(params string[] meterNames)
    {
        _meterNames = meterNames.ToHashSet(StringComparer.Ordinal);

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (_meterNames.Contains(instrument.Meter.Name))
                    listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>(OnLongMeasurement);
        _listener.SetMeasurementEventCallback<double>(OnDoubleMeasurement);
        _listener.Start();
    }

    public IReadOnlyList<MetricMeasurement> Snapshot()
    {
        lock (_sync)
        {
            return _measurements.ToArray();
        }
    }

    public IReadOnlyList<MetricMeasurement> ForInstrument(string instrumentName)
    {
        return Snapshot().Where(m => string.Equals(m.InstrumentName, instrumentName, StringComparison.Ordinal))
            .ToArray();
    }

    private void OnLongMeasurement(Instrument instrument, long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _)
    {
        Record(instrument, measurement, tags);
    }

    private void OnDoubleMeasurement(Instrument instrument, double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _)
    {
        Record(instrument, measurement, tags);
    }

    private void Record<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        where T : struct
    {
        var capturedTags = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var tag in tags)
            capturedTags[tag.Key] = tag.Value?.ToString();

        lock (_sync)
        {
            _measurements.Add(new MetricMeasurement(
                instrument.Meter.Name,
                instrument.Name,
                Convert.ToDouble(measurement, CultureInfo.InvariantCulture),
                capturedTags));
        }
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}

public sealed record MetricMeasurement(
    string MeterName,
    string InstrumentName,
    double Value,
    IReadOnlyDictionary<string, string?> Tags);
