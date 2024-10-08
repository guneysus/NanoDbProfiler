
using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NanoDbProfiler.AspNetCore;

internal class NanoDbProfilerEfCoreQueryInterceptor : DbCommandInterceptor
{
    private static Metric metricFactory(CommandExecutedEventData eventData)
    {
        return new Metric()
        {
            Duration = eventData.Duration.TotalMilliseconds,
            Query = eventData.Command.CommandText
        };
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        EfCoreMetrics.GetInstance().Add(metricFactory(eventData));
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        EfCoreMetrics.GetInstance().Add(metricFactory(eventData));
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        EfCoreMetrics.GetInstance().Add(metricFactory(eventData));
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        EfCoreMetrics.GetInstance().Add(metricFactory(eventData));
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        EfCoreMetrics.GetInstance().Add(metricFactory(eventData));
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        EfCoreMetrics.GetInstance().Add(metricFactory(eventData));
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}

public struct DashboardData
{
    public List<MetricSummary> Summaries { get; set; }
    public DashboardData(IDictionary<string, ConcurrentBag<double>> metrics)
    {
        this.Summaries = metrics.Keys.Select(query =>
        {
            var values = metrics [query];

            return new MetricSummary()
            {
                Query = query,
                Total = values.Count(),
                P95 = Math.Round(CalculatePercentile(values.ToArray(), 95), 3)
            };
        }).ToList();

    }

    public static double CalculatePercentile(double [] values, double percentile)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Array cannot be null or empty.");

        if (percentile < 0 || percentile > 100)
            throw new ArgumentException("Percentile must be between 0 and 100.");

        int n = values.Length;
        if (n == 1)
            return values [0];

        // Sort the array in place
        Array.Sort(values);

        // Calculate the rank
        double rank = (percentile / 100.0) * (n - 1);

        // Determine the integer and fractional part of the rank
        int lowerIndex = (int) Math.Floor(rank);
        int upperIndex = (int) Math.Ceiling(rank);

        if (lowerIndex == upperIndex)
            return values [lowerIndex];

        double lowerValue = values [lowerIndex];
        double upperValue = values [upperIndex];

        // Interpolate
        double fraction = rank - lowerIndex;
        return lowerValue + fraction * (upperValue - lowerValue);
    }
}

