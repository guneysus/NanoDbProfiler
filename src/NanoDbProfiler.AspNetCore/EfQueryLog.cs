namespace Microsoft.Extensions.DependencyInjection;

public static class EfQueryLog
{
    static ConcurrentDictionary<string, ConcurrentBag<double>> _metrics = new();
    public static void AddMetric(Metric metric)
    {
        var item = _metrics.GetOrAdd(metric.Query.Trim(), new ConcurrentBag<double>());
        item.Add(metric.Duration);
    }

    static DashboardData GetMetricsSummary()
    {
        return new DashboardData(_metrics);
    }

    public static IEndpointConventionBuilder UseQueryDashboard(this IEndpointRouteBuilder app,
        string route = "query-log")
    {
        //app.MapGet($"{route}/json", () => );

        RouteHandlerBuilder routeHandlerBuilder = app.MapGet("{route}", (HttpRequest h) =>
        {
            MediaTypeHeaderValue.TryParseList(h.Headers["Accept"], out var accept);

            IResult resp = accept switch
            {
                null => TextResult(),
                var a when a.Any(x => x.MatchesMediaType("text/html")) => HtmlResult(),
                var a when a.Any(x => x.MatchesMediaType("text/plain")) => TextResult(),
                var a when a.Any(x => x.MatchesMediaType("application/json")) => JsonResult(),
                _ => TextResult()
            };

            return resp;
        });

        return routeHandlerBuilder;
    }

    private static IResult HtmlResult () {
        var htmlBuilder = new StringBuilder();

        htmlBuilder.Append(@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>EF Core DB Profiler</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f4f4f9;
        }
        .metric-card {
            background: #fff;
            border: 1px solid #ddd;
            border-radius: 8px;
            margin-bottom: 16px;
            padding: 16px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }
        .metric-title {
            font-size: 18px;
            margin-bottom: 8px;
            color: #333;
        }
        .metric-details {
            font-size: 14px;
            color: #666;
        }
        .metric-query {
            background: #f9f9f9;
            padding: 10px;
            border-radius: 4px;
            overflow-x: auto;
            font-family: monospace;
            white-space: pre-wrap;
            margin-top: 10px;
            border: 1px solid #e0e0e0;
        }
    </style>
</head>
<body>

    <h1>EF Core DB Profiler</h1>
    <div id='metrics-container'>");

        var s = new DashboardData(_metrics);


        foreach (var summary in s.Summaries) {
            htmlBuilder.Append($@"
        <div class='metric-card'>
            <div class='metric-title'>P95: {summary.P95}ms, Total: {summary.Total}</div>
            <div class='metric-query'>{summary.Query}</div>
        </div>");
        }

        htmlBuilder.Append(@"
    </div>

</body>
</html>");

        return Results.Text(htmlBuilder.ToString(), "text/html");
    }

    private static IResult JsonResult()
    {
        return Results.Json(GetMetricsSummary());
    }

    private static IResult TextResult()
    {
        var sb = new StringBuilder();

        var summary = new DashboardData(_metrics);

        foreach (var item in summary.Summaries)
        {
            sb
                .AppendFormat(@"P95: ""{0}ms"" Total: {1}", item.P95, item.Total)
                .AppendLine().AppendLine("-")
                .AppendLine(item.Query)
                .AppendLine("---").AppendLine();

        }

        string plainText = sb.ToString();
        return Results.Text(plainText);
    }

    public static IServiceCollection AddQueryLog(this IServiceCollection services)
    {
        EfQueryLog.UseSqlQueryLogDashboard();
        return services;
    }

    static void UseSqlQueryLogDashboard()
    {
        var h = new Harmony("id");

        #region Hooking
        /* Stub layer
        * ----------
        */

        Assembly[] assemblies = AppDomain
            .CurrentDomain
            .GetAssemblies();


        // {Microsoft.EntityFrameworkCore.Diagnostics.Internal.RelationalCommandDiagnosticsLogger}
        var diagnosticsLoggerTypes = (from t in assemblies.SelectMany(t => t.GetTypes())
                                      where t.GetInterfaces().Any(t => t.Name.Contains("IRelationalCommandDiagnosticsLogger"))
                                      select t).ToList();

        var efCoreRelationAsm = assemblies
            .Single(x => x.GetName().Name == "Microsoft.EntityFrameworkCore.Relational");

        Type[] efCoreRelationalTypes = efCoreRelationAsm.GetTypes();
        var diagnosticsLogger = efCoreRelationalTypes.Single(x => x.FullName == "Microsoft.EntityFrameworkCore.Diagnostics.Internal.RelationalCommandDiagnosticsLogger");

        var d = diagnosticsLogger
            .GetMethods(AccessTools.all)
            .Where(x => x.Name.Contains("Executed"));

        var genericHook = typeof(Hooks).GetMethod(nameof(Hooks.GenericHook));

        // foreach (var method in diagnosticsLoggerMethods) {
        //     h.Patch(prefix: new HarmonyMethod(genericHook), original: method);
        // }

        h.Patch(original: d.Single(x => x.Name == "CommandReaderExecuted"), prefix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CommandReaderExecuted))));
        h.Patch(original: d.Single(x => x.Name == "CommandScalarExecuted"), prefix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CommandScalarExecuted))));
        h.Patch(original: d.Single(x => x.Name == "CommandNonQueryExecuted"), prefix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CommandNonQueryExecuted))));
        h.Patch(original: d.Single(x => x.Name == "CommandReaderExecutedAsync"), prefix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CommandReaderExecutedAsync))));
        h.Patch(original: d.Single(x => x.Name == "CommandScalarExecutedAsync"), prefix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CommandScalarExecutedAsync))));
        h.Patch(original: d.Single(x => x.Name == "CommandNonQueryExecutedAsync"), prefix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.CommandNonQueryExecutedAsync))));

        var relationCommandType = efCoreRelationalTypes
            .Single(x => x.Name == "RelationalCommand");


        var methods = relationCommandType.GetMethods(AccessTools.all);
        var original = methods.Single(x => x.Name == "ExecuteReader");
        var prefix = typeof(Hooks).GetMethod(nameof(Hooks.ExecuteReaderPrefix));
        var postfix = typeof(Hooks).GetMethod(nameof(Hooks.ExecuteReaderPostfix));

        h.Patch(original, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));

        #endregion
    }
}
