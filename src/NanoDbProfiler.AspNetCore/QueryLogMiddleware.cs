namespace Microsoft.Extensions.DependencyInjection;

public class QueryLogMiddleware
{
    public static string? QUERY_LOG_ROUTE;

    private readonly RequestDelegate _next;

    public QueryLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var path = context.Request.Path;

            if (path == QUERY_LOG_ROUTE)
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

                toolbarHtml = EmbeddedResourceHelpers.GetResource("Toolbar.html");

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
            context.Response.Headers.ContentLength = body.Length;
            await context.Response.WriteAsync(body);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (!context.Response.HasStarted)
                await _next(context);
        }
    }
}