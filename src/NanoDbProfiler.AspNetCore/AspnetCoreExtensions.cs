using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public static class AspnetCoreExtensions
{
    public static IServiceCollection AddNanoDbProfiler(this IServiceCollection s)
    {
        const string EfCoreRelationalAssemblyString = "Microsoft.EntityFrameworkCore.Relational";
        const string DiagnosticLoggerFullname = "Microsoft.EntityFrameworkCore.Diagnostics.Internal.RelationalCommandDiagnosticsLogger";
        const string DiagnosticLoggerInterfaceName = "IRelationalCommandDiagnosticsLogger";

        var efCoreRelationalAsm = Assembly.Load(EfCoreRelationalAssemblyString);
        ArgumentNullException.ThrowIfNull(efCoreRelationalAsm);

        s.AddSingleton<EfCoreMetrics>();

        var h = new Harmony("id");

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var diagnosticsLoggerTypes = (
            from t in assemblies.SelectMany(t => t.GetTypes())
            where t.GetInterfaces().Any(t => t.Name.Contains(DiagnosticLoggerInterfaceName))
            select t);

        var efCoreRelationAsm = (
            from asm in assemblies
            where asm.GetName().Name == EfCoreRelationalAssemblyString
            select asm).Single();

        Type[] efCoreRelationalTypes = efCoreRelationAsm.GetTypes();

        var diagnosticsLoggerType = (
            from t in efCoreRelationalTypes
            where t.FullName == DiagnosticLoggerFullname
            select t).Single();

        MethodInfo[] diagnosticsLoggerMethods = diagnosticsLoggerType.GetMethods(AccessTools.all);

        var diagLoggerMethods = (
            from m in diagnosticsLoggerMethods
            where m.Name.Contains("Executed")
            select m);

        patch("CommandReaderExecuted", nameof(Hooks.CommandReaderExecuted), diagLoggerMethods, h);
        patch("CommandScalarExecuted", nameof(Hooks.CommandScalarExecuted), diagLoggerMethods, h);
        patch("CommandNonQueryExecuted", nameof(Hooks.CommandNonQueryExecuted), diagLoggerMethods, h);
        patch("CommandReaderExecutedAsync", nameof(Hooks.CommandReaderExecutedAsync), diagLoggerMethods, h);
        patch("CommandScalarExecutedAsync", nameof(Hooks.CommandScalarExecutedAsync), diagLoggerMethods, h);
        patch("CommandNonQueryExecutedAsync", nameof(Hooks.CommandNonQueryExecutedAsync), diagLoggerMethods, h);

        var relationCommandType = efCoreRelationalTypes.Single(x => x.Name == "RelationalCommand");
        var methods = relationCommandType.GetMethods(AccessTools.all);

        patch("ExecuteReader", methods, h,
            prefix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ExecuteReaderPrefix))),
            postfix: new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.ExecuteReaderPostfix))));

        return s;
    }

    private static void patch(string name, IEnumerable<MethodInfo> methods, Harmony harmony, HarmonyMethod prefix, HarmonyMethod postfix)
    {
        var method = (
            from m in methods
            where m.Name == name
            select m).Single();

        var replacement = harmony.Patch(method, prefix: prefix, postfix: postfix);
        ArgumentNullException.ThrowIfNull(replacement);
    }

    private static void patch(string name, string hookName, IEnumerable<MethodInfo> methods, Harmony harmony)
    {
        var replacement = harmony.Patch(methods.Single(x => x.Name == name), new HarmonyMethod(typeof(Hooks).GetMethod(name)));
        ArgumentNullException.ThrowIfNull(replacement);
    }

    public static WebApplication UseNanodbProfilerToolbar(this WebApplication app, string route = "query-log")
    {
        EfQueryLog.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

        app.MapGet(route, ([FromServices] EfCoreMetrics metrics, HttpRequest h) =>
        {
            MediaTypeHeaderValue.TryParseList(h.Headers["Accept"], out var accept);

            IResult resp = accept switch
            {
                null => EfQueryLog.TextResult(metrics),
                var a when a.Any(x => x.MatchesMediaType("text/html")) => EfQueryLog.HtmlResult(metrics),
                var a when a.Any(x => x.MatchesMediaType("text/plain")) => EfQueryLog.TextResult(metrics),
                var a when a.Any(x => x.MatchesMediaType("application/json")) => EfQueryLog.JsonResult(metrics),
                _ => EfQueryLog.TextResult(metrics)
            };

            return resp;
        });

        app.MapDelete(route, ([FromServices] EfCoreMetrics metrics, HttpRequest h) =>
        {
            metrics.Clear();
            return Results.NoContent();
        });

        app.UseMiddleware<QueryLogMiddleware>();

        return app;
    }
}