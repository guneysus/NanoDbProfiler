namespace Microsoft.Extensions.DependencyInjection;


public static class EfQueryLog
{
    public static IServiceScopeFactory? ServiceScopeFactory { get; internal set; }

    public static EfCoreMetrics GetMetricsDb()
    {
        ArgumentNullException.ThrowIfNull(ServiceScopeFactory);
        using var scope = ServiceScopeFactory.CreateScope();
        EfCoreMetrics metrics = scope.ServiceProvider.GetRequiredService<EfCoreMetrics>();
        return metrics;
    }

    public static void AddMetric(Metric metric)
    {
        EfCoreMetrics? metrics = GetMetricsDb();
        metrics.Add(metric);
    }

    static DashboardData GetMetricsSummary()
    {
        EfCoreMetrics? metrics = GetMetricsDb();

        return new DashboardData(metrics.Data);
    }

    internal static IResult HtmlResult(EfCoreMetrics metrics)
    {
        var htmlBuilder = new StringBuilder();

        htmlBuilder.Append(@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>NanoDbProfiler for Entity Framework Core</title>
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

/* Global Scrollbar Styling for Webkit (Chrome, Safari, Edge) */
::-webkit-scrollbar {
    width: 12px; /* Width of the scrollbar */
    height: 12px; /* Height for horizontal scrollbars */
}

::-webkit-scrollbar-track {
    background: #f1f1f1; /* Background of the track */
}

::-webkit-scrollbar-thumb {
    background-color: #888; /* Color of the scroll thumb */
    border-radius: 10px; /* Rounded corners */
    border: 2px solid #f1f1f1; /* Adds spacing between thumb and track */
}

::-webkit-scrollbar-thumb:hover {
    background-color: #555; /* Darker color on hover */
}

/* Global Scrollbar Styling for Firefox */
* {
    scrollbar-width: thin; /* Makes the scrollbar thinner */
    scrollbar-color: #888 #f1f1f1; /* Color of thumb and track */
}


    </style>
</head>
<body>
    <div id='metrics-container'>");

        var s = new DashboardData(metrics.Data);

        foreach (var summary in s.Summaries)
        {
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

    internal static IResult JsonResult(EfCoreMetrics metrics)
    {
        return Results.Json(GetMetricsSummary());
    }

    internal static IResult TextResult(EfCoreMetrics metrics)
    {
        var summary = new DashboardData(metrics.Data);

        string plainText = TextRepr(summary);
        return Results.Text(plainText);
    }

    public static string TextRepr(DashboardData summary)
    {
        var sb = new StringBuilder();

        foreach (var item in summary.Summaries)
        {
            sb
                .AppendFormat(@"P95: ""{0}ms"" Total: {1}", item.P95, item.Total)
                .AppendLine().AppendLine("-")
                .AppendLine(item.Query)
                .AppendLine("---").AppendLine();

        }

        string plainText = sb.ToString();
        return plainText;
    }
}
