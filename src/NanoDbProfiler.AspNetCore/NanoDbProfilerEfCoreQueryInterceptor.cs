
using System.Data.Common;

//using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NanoDbProfiler.AspNetCore;

internal class NanoDbProfilerEfCoreQueryInterceptor
{
    private static Metric metricFactory(object eventData)
    {
        var metric = new Metric();

        // Use reflection to get the type of the eventData object
        var eventDataType = eventData.GetType();

        // Get the "Duration" property
        var durationProperty = eventDataType.GetProperty("Duration");
        if (durationProperty != null)
        {
            var duration = durationProperty.GetValue(eventData); // Get the value of the Duration property
            if (duration is TimeSpan durationTimeSpan)
            {
                metric.Duration = durationTimeSpan.TotalMilliseconds; // Convert to milliseconds
            }
        }

        // Get the "Command" property
        var commandProperty = eventDataType.GetProperty("Command");
        if (commandProperty != null)
        {
            var command = commandProperty.GetValue(eventData); // Get the value of the Command property
            if (command is System.Data.Common.DbCommand dbCommand)
            {
                metric.Query = dbCommand.CommandText; // Extract the CommandText from the DbCommand
            }
        }

        return metric;
    }

    public int NonQueryExecuted(System.Data.Common.DbCommand command, object eventData, int result)
    {
        // Use reflection to pass the eventData to the metric factory
        var metric = metricFactory(eventData);

        // Call the singleton instance and add the metric
        EfCoreMetrics.GetInstance().Add(metric);

        // Return the result directly without calling base method
        return result;
    }


    public ValueTask<int> NonQueryExecutedAsync(DbCommand command, object eventData, int result, CancellationToken cancellationToken = default)
    {
        return ExecuteWithReflection<ValueTask<int>>("NonQueryExecutedAsync", command, eventData, result, cancellationToken);
    }

    public DbDataReader ReaderExecuted(DbCommand command, object eventData, DbDataReader result)
    {
        return ExecuteWithReflection<DbDataReader>("ReaderExecuted", command, eventData, result);
    }

    public ValueTask<object?> ScalarExecutedAsync(DbCommand command, object eventData, object? result, CancellationToken cancellationToken = default)
    {
        return ExecuteWithReflection<ValueTask<object?>>("ScalarExecutedAsync", command, eventData, result, cancellationToken);
    }

    public object? ScalarExecuted(DbCommand command, object eventData, object? result)
    {
        return ExecuteWithReflection<object?>("ScalarExecuted", command, eventData, result);
    }

    public ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, object eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        return ExecuteWithReflection<ValueTask<DbDataReader>>("ReaderExecutedAsync", command, eventData, result, cancellationToken);
    }

    private T ExecuteWithReflection<T>(string methodName, params object [] parameters)
    {
        // Get the method by name from the current type
        var method = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // If the method is not found, throw an exception
        if (method == null)
        {
            throw new InvalidOperationException($"Method {methodName} not found.");
        }

        // Dynamically access properties of eventData (assuming eventData is the second parameter)
        var eventData = parameters [1];
        var eventDataType = eventData.GetType();

        // Get the "metricFactory" method using reflection
        var metricFactoryMethod = this.GetType().GetMethod("metricFactory", BindingFlags.NonPublic | BindingFlags.Static);

        if (metricFactoryMethod == null)
        {
            throw new InvalidOperationException("metricFactory method not found.");
        }

        // Invoke the metricFactory method and get the metric
        var metric = metricFactoryMethod.Invoke(null, new [] { eventData });

        // Cast the returned object to the expected type (Metric)
        if (metric is Metric validMetric)
        {
            // Add the metric to the EfCoreMetrics instance
            EfCoreMetrics.GetInstance().Add(validMetric);
        }
        else
        {
            throw new InvalidOperationException("The metric factory did not return the expected type.");
        }

        // Invoke the original method with the parameters and return the result
        return (T) method.Invoke(this, parameters);
    }


}

