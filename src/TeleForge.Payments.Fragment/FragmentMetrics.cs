namespace TeleForge.Payments.Fragment;

/// <summary>
/// Simple metrics recorder for Fragment API operations.
/// Can be subclassed to integrate with Prometheus, Application Insights, etc.
/// </summary>
public class FragmentMetrics
{
    /// <summary>Record a Fragment API request with endpoint, status, and duration.</summary>
    public virtual void RecordRequest(string endpoint, string status, double durationMs)
    {
    }

    /// <summary>Record a stars order amount for histogram tracking.</summary>
    public virtual void RecordStarsAmount(double amount)
    {
    }

    /// <summary>Record an authentication event.</summary>
    public virtual void RecordAuth(bool success)
    {
    }
}
