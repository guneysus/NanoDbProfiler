namespace Microsoft.Extensions.DependencyInjection;

public static class EfQueryLog
{
    public static IServiceScopeFactory? ServiceScopeFactory { get; internal set; }

    public static EfCoreMetrics GetMetricsDb()
    {
        ArgumentNullException.ThrowIfNull(ServiceScopeFactory);
        using var scope = ServiceScopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<EfCoreMetrics>();
    }

    public static void AddMetric(Metric metric)
    {
        EfCoreMetrics? metrics = GetMetricsDb();
        metrics.Add(metric);
    }

    internal static IResult HtmlResult(EfCoreMetrics metrics)
    {
        // Load header and footer HTML from resources folder
        var headerHtml = File.ReadAllText("Resources/header.html");
        var footerHtml = File.ReadAllText("Resources/footer.html");
        var metricCardTemplate = File.ReadAllText("Resources/metric-card.html");

        var htmlBuilder = new StringBuilder();

        htmlBuilder.Append(headerHtml);

        var s = new DashboardData(metrics.Data);

        foreach (var summary in s.Summaries)
        {
            var metricCardHtml = metricCardTemplate
                .Replace("{P95}", summary.P95.ToString())
                .Replace("{Total}", summary.Total.ToString())
                .Replace("{Query}", summary.Query);

            htmlBuilder.Append(metricCardHtml);
        }

        htmlBuilder.Append(footerHtml); // Add the footer

        return Results.Text(htmlBuilder.ToString(), "text/html");
    }

    internal static IResult JsonResult(EfCoreMetrics metrics) => Results.Json(new DashboardData(GetMetricsDb().Data));

    internal static IResult TextResult(EfCoreMetrics metrics) => Results.Text(TextRepr(new DashboardData(metrics.Data)));

    public static string TextRepr(DashboardData summary)
    {
        var sb = new StringBuilder();

        foreach (MetricSummary item in summary.Summaries)
        {
            sb.AppendFormat(@"P95: ""{0}ms"" Total: {1}", item.P95, item.Total)
                .AppendLine().AppendLine("-")
                .AppendLine(item.Query)
                .AppendLine("---").AppendLine();
        }

        string plainText = sb.ToString();
        return plainText;
    }
}
