
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.DependencyInjection;

public class EfCoreMetrics
{
    public int Id => RuntimeHelpers.GetHashCode(this);

    public ConcurrentDictionary<string, ConcurrentBag<double>> Data = new();

    public void Add (Metric metric) {
        var item = Data.GetOrAdd(metric.Query.Trim(), new ConcurrentBag<double>());
        item.Add(metric.Duration);
    }

    public void Clear () => Data.Clear();
}
