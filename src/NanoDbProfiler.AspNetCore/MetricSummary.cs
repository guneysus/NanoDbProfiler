namespace NanoDbProfiler.AspNetCore;

public struct MetricSummary
{
    public string Query { get; set; }
    public long Total { get; set; }

    /// <summary>
    /// TODO: Implement
    /// </summary>
    public double P95 { get; set; }
}

