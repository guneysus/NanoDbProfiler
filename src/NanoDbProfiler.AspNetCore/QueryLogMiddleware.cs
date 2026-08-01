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
        if (!string.IsNullOrEmpty(QUERY_LOG_ROUTE) && context.Request.Path == QUERY_LOG_ROUTE)
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        await using var newBodyStream = new MemoryStream();
        context.Response.Body = newBodyStream;

        try
        {
            // Call the next middleware
            await _next(context);

            // If the response is HTML, modify it
            if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
            {
                newBodyStream.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(newBodyStream).ReadToEndAsync();

                var toolbarHtml = EmbeddedResourceHelpers.GetResource("Toolbar.html");

                if (body.Contains("</body>", StringComparison.OrdinalIgnoreCase))
                {
                    body = body.Replace("</body>", $"{toolbarHtml}</body>", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    body += toolbarHtml;
                }

                var bodyBytes = Encoding.UTF8.GetBytes(body);

                // Reset the response body
                context.Response.ContentLength = bodyBytes.Length;
                newBodyStream.SetLength(0);
                await newBodyStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                await newBodyStream.FlushAsync();
                newBodyStream.Seek(0, SeekOrigin.Begin);
            }

            // Copy the modified or original response body back to the original stream
            newBodyStream.Seek(0, SeekOrigin.Begin);
            await newBodyStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in QueryLogMiddleware: {ex}");
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
