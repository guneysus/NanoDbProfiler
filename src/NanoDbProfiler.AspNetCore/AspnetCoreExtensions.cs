using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public static class AspnetCoreExtensions
{
    public static IServiceCollection AddNanoDbProfiler(this IServiceCollection s)
    {
        const string EfCoreRelationalAssemblyString = "Microsoft.EntityFrameworkCore.Relational";
        const string DiagnosticLoggerFullname = "Microsoft.EntityFrameworkCore.Diagnostics.Internal.RelationalCommandDiagnosticsLogger";

        _ = AppDomain.CurrentDomain.Load(EfCoreRelationalAssemblyString);
        _ = Assembly.Load(EfCoreRelationalAssemblyString);

        s.AddSingleton<EfCoreMetrics>();

        var h = new Harmony("id");

        Assembly [] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        // {Microsoft.EntityFrameworkCore.Diagnostics.Internal.RelationalCommandDiagnosticsLogger}

        var diagnosticsLoggerTypes = (
            from t in assemblies.SelectMany(t => t.GetTypes())
            where t.GetInterfaces().Any(t => t.Name.Contains("IRelationalCommandDiagnosticsLogger"))
            select t);

        var efCoreRelationAsm = (
            from asm in assemblies
            where asm.GetName().Name == EfCoreRelationalAssemblyString
            select asm).Single();

        Type [] efCoreRelationalTypes = efCoreRelationAsm.GetTypes();

        var diagnosticsLoggerType = (
            from t in efCoreRelationalTypes
            where t.FullName == DiagnosticLoggerFullname
            select t).Single();

        MethodInfo [] diagnosticsLoggerMethods = diagnosticsLoggerType.GetMethods(AccessTools.all);

        var d = (
            from m in diagnosticsLoggerMethods
            where m.Name.Contains("Executed")
            select m);

#if ENABLE_GENERIC_HOOKS
        var genericHook = typeof(Hooks).GetMethod(nameof(Hooks.GenericHook));

        foreach (var method in diagnosticsLoggerMethods)
        {
            h.Patch(prefix: new HarmonyMethod(genericHook), original: method);
        }
#endif

        patch(h, d, "CommandReaderExecuted");
        patch(h, d, "CommandScalarExecuted");
        patch(h, d, "CommandNonQueryExecuted");
        patch(h, d, "CommandReaderExecutedAsync");
        patch(h, d, "CommandScalarExecutedAsync");
        patch(h, d, "CommandNonQueryExecutedAsync");

        var relationCommandType = efCoreRelationalTypes.Single(x => x.Name == "RelationalCommand");
        var methods = relationCommandType.GetMethods(AccessTools.all);
        var original = methods.Single(x => x.Name == "ExecuteReader");
        var prefix = typeof(Hooks).GetMethod(nameof(Hooks.ExecuteReaderPrefix));
        var postfix = typeof(Hooks).GetMethod(nameof(Hooks.ExecuteReaderPostfix));

        h.Patch(original, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));

        return s;
    }

    private static void patch(Harmony harmony, IEnumerable<MethodInfo> methods, string name) => harmony.Patch(methods.Single(x => x.Name == name), new HarmonyMethod(typeof(Hooks).GetMethod(name)));

    public static WebApplication UseNanodbProfilerToolbar(this WebApplication app, string route = "query-log")
    {
        EfQueryLog.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

        app.MapGet(route, ([FromServices] EfCoreMetrics metrics, HttpRequest h) =>
        {
            MediaTypeHeaderValue.TryParseList(h.Headers ["Accept"], out var accept);

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