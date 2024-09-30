namespace Microsoft.Extensions.DependencyInjection;

public class QueryLogMiddleware
{
    private readonly RequestDelegate _next;

    public QueryLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (path == "/query-log")
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var newBodyStream = new MemoryStream();
        context.Response.Body = newBodyStream;

        await _next(context);

        newBodyStream.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(newBodyStream).ReadToEndAsync();

        // Inject toolbar before closing body tag if the response is HTML
        if (context.Response.ContentType != null && context.Response.ContentType.Contains("text/html"))
        {
            string? toolbarHtml = default;

            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames();
            var resourceName = "NanoDbProfiler.AspNetCore.Resources.Toolbar.html";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                toolbarHtml = reader.ReadToEnd();
            }


            if (body.Contains("<body>"))
            {
                body = body.Replace("</body>", $"{toolbarHtml}</body>");
            }
            else
            {
                body += toolbarHtml;
            }
        }

        context.Response.Body = originalBodyStream;
        await context.Response.WriteAsync(body);
    }
}