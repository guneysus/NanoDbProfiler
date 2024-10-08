namespace Microsoft.Extensions.DependencyInjection;

public class EfCoreMetrics
{
    private static EfCoreMetrics? _instance = null;
    private static readonly object _lock = new object();
    // Private constructor to prevent instantiation
    private EfCoreMetrics()
    {
    }

    public static EfCoreMetrics GetInstance()
    {
        if (_instance != null)
            return _instance;

        // Locking for thread safety
        lock (_lock)
            _instance ??= new EfCoreMetrics();

        return _instance;
    }

    public ConcurrentDictionary<string, ConcurrentBag<double>> Data = new();

    public void Add(Metric metric)
    {
        var item = Data.GetOrAdd(metric.Query.Trim(), new ConcurrentBag<double>());
        item.Add(metric.Duration);
    }

    public void Clear() => Data.Clear();
}
