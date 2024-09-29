namespace NanoDbProfiler.AspNetCore;

public static class Hooks
{
    public static void ExecuteReaderPrefix(object __instance, object[] __args, out Stopwatch __state)
    {
        __state = new Stopwatch();
        __state.Start();
    }

    public static void CommandReaderExecuted(object [] __args, TimeSpan duration, object command) => processLog(duration, command);

    public static void CommandScalarExecuted(TimeSpan duration, object command) => processLog(duration, command);

    public static void CommandNonQueryExecuted(TimeSpan duration, object command) => processLog(duration, command);

    public static void CommandReaderExecutedAsync(TimeSpan duration, object command) => processLog(duration, command);

    public static void CommandScalarExecutedAsync(TimeSpan duration, object command) => processLog(duration, command);

    public static void CommandNonQueryExecutedAsync(TimeSpan duration, object command) => processLog(duration, command);

    private static void processLog(TimeSpan duration, object command)
    {
        Type type = command.GetType();
        PropertyInfo? cmdTextProp = type.GetProperty("CommandText");

        if (cmdTextProp == null) return;
        var cmdText = cmdTextProp.GetValue(command) as string;
        var ms = duration.TotalMilliseconds;

        var metric = new Metric
        {
            Duration = ms,
            Query = cmdText,
#if ENABLE_PARAMETERS_DICT
                Parameters = parameterValuesDict 
#endif
        };

        EfQueryLog.AddMetric(metric);
    }

    public static void ExecuteReaderPostfix(object __instance,
        object[] __args,
        Stopwatch __state)
    {
        __state.Stop();

        if (__instance == null || __instance.GetType().Name != "RelationalCommand")
            return;

        Type type = __instance.GetType();
        PropertyInfo? cmdTextProp = type.GetProperty("CommandText");

        if (cmdTextProp == null) return;
        var cmdText = cmdTextProp.GetValue(__instance) as string;

#if ENABLE_PARAMETERS_DICT
        IDictionary<string, object?>? parameterValuesDict = default;

        if (__args != null && __args.Length > 0)
        {
            var parameters = __args[0];
            Type parameterValuesType = parameters.GetType();
            if (parameterValuesType is IDictionary<string, object?>)
            {
                PropertyInfo? parameterValuesProp = parameterValuesType.GetProperty("ParameterValues");
                if (parameterValuesProp != null)
                {
                    object? parameterValue = parameterValuesProp.GetValue(parameters);
                    parameterValuesDict = parameterValue as IDictionary<string, object?>;
                }
            }
        } 
#endif

        if (cmdText == null) return;

        var metric = new Metric
        {
            Duration = __state.Elapsed.TotalMilliseconds,
            Query = cmdText,
#if ENABLE_PARAMETERS_DICT
                Parameters = parameterValuesDict 
#endif
        };

        EfQueryLog.AddMetric(metric);
    }
}

