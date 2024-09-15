﻿using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AspnetCoreExtensions
    {

        internal static MethodInfo GetHook (string name) {
            return typeof(Hooks).GetMethod(name);
        }

        public static IServiceCollection AddNanoDbProfiler (this IServiceCollection services) {

            AppDomain.CurrentDomain.Load("Microsoft.EntityFrameworkCore.Relational");

            Assembly.Load("Microsoft.EntityFrameworkCore.Relational");
            AppDomain.CurrentDomain.Load(new AssemblyName("Microsoft.EntityFrameworkCore.Relational"));

            services.AddSingleton<EfCoreMetrics>();


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

            AppDomain.CurrentDomain.Load("Microsoft.EntityFrameworkCore.Relational");

            Assembly.Load("Microsoft.EntityFrameworkCore.Relational");

            var efCoreRelationAsm = assemblies.FirstOrDefault(x => x.GetName().Name == "Microsoft.EntityFrameworkCore.Relational");
            if (efCoreRelationAsm == null)
                return services;

            Type[] efCoreRelationalTypes = efCoreRelationAsm.GetTypes();
            var diagnosticsLogger = efCoreRelationalTypes.Single(x => x.FullName == "Microsoft.EntityFrameworkCore.Diagnostics.Internal.RelationalCommandDiagnosticsLogger");

            var d = diagnosticsLogger
            .GetMethods(AccessTools.all)
            .Where(x => x.Name.Contains("Executed"));

            var genericHook = typeof(Hooks).GetMethod(nameof(Hooks.GenericHook));

            // foreach (var method in diagnosticsLoggerMethods) {
            //     h.Patch(prefix: new HarmonyMethod(genericHook), original: method);
            // }

            h.Patch(d.Single(x => x.Name == "CommandReaderExecuted"), new HarmonyMethod(GetHook((nameof(Hooks.CommandReaderExecuted)))));
            h.Patch(d.Single(x => x.Name == "CommandScalarExecuted"), new HarmonyMethod(GetHook(nameof(Hooks.CommandScalarExecuted))));
            h.Patch(d.Single(x => x.Name == "CommandNonQueryExecuted"), new HarmonyMethod(GetHook(nameof(Hooks.CommandNonQueryExecuted))));
            h.Patch(d.Single(x => x.Name == "CommandReaderExecutedAsync"), new HarmonyMethod(GetHook(nameof(Hooks.CommandReaderExecutedAsync))));
            h.Patch(d.Single(x => x.Name == "CommandScalarExecutedAsync"), new HarmonyMethod(GetHook(nameof(Hooks.CommandScalarExecutedAsync))));
            h.Patch(d.Single(x => x.Name == "CommandNonQueryExecutedAsync"), new HarmonyMethod(GetHook(nameof(Hooks.CommandNonQueryExecutedAsync))));

            var relationCommandType = efCoreRelationalTypes.Single(x => x.Name == "RelationalCommand");
            var methods = relationCommandType.GetMethods(AccessTools.all);
            var original = methods.Single(x => x.Name == "ExecuteReader");
            var prefix = GetHook(nameof(Hooks.ExecuteReaderPrefix));
            var postfix = GetHook(nameof(Hooks.ExecuteReaderPostfix));

            h.Patch(original, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));

            #endregion

            return services;
        }

        public static WebApplication UseNanodbProfilerToolbar (this WebApplication app, string route = "query-log") {

            EfQueryLog.App = app;
            app.MapGet(route, ([FromServices] EfCoreMetrics metrics, HttpRequest h) => {
                MediaTypeHeaderValue.TryParseList(h.Headers["Accept"], out var accept);

                IResult resp = accept switch {
                    null => EfQueryLog.TextResult(metrics),
                    var a when a.Any(x => x.MatchesMediaType("text/html")) => EfQueryLog.HtmlResult(metrics),
                    var a when a.Any(x => x.MatchesMediaType("text/plain")) => EfQueryLog.TextResult(metrics),
                    var a when a.Any(x => x.MatchesMediaType("application/json")) => EfQueryLog.JsonResult(metrics),
                    _ => EfQueryLog.TextResult(metrics)
                };

                return resp;
            });

            app.MapDelete(route, ([FromServices] EfCoreMetrics metrics, HttpRequest h) => {
                metrics.Clear();
                return Results.NoContent();
            });


            app.UseMiddleware<QueryLogMiddleware>();

            return app;
        }
    }

}