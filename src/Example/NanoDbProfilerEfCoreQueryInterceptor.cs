
using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

using NanoDbProfiler.AspNetCore;

namespace Example;


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