
namespace NanoDbProfiler.AspNetCore;

public struct Metric
{
    public double Duration { get; set; }
    public string Query { get; set; }

#if ENABLE_PARAMETERS_DICT
    public IDictionary<string, object?> Parameters { get; set; }
#endif
    }

