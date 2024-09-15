using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public class QueryLogMiddleware
{
    private readonly RequestDelegate _next;

    public QueryLogMiddleware (RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync (HttpContext context) {
        var path = context.Request.Path;

        if (path == "/query-log") {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using (var newBodyStream = new MemoryStream()) {
            context.Response.Body = newBodyStream;

            await _next(context);

            newBodyStream.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(newBodyStream).ReadToEndAsync();

            // Inject toolbar before closing body tag if the response is HTML
            if (context.Response.ContentType != null && context.Response.ContentType.Contains("text/html")) {
                var toolbarHtml = GetDebugToolbarHtml();
                if (body.Contains("<body>")) {
                    body = body.Replace("</body>", $"{toolbarHtml}</body>");
                } else {
                    body += toolbarHtml;
                }
            }

            context.Response.Body = originalBodyStream;
            await context.Response.WriteAsync(body);
        }
    }

    private string GetDebugToolbarHtml () {
        // Define the toolbar's HTML content

        return @"
<!-- Load the Roboto font from Google Fonts -->
<link href=""https://fonts.googleapis.com/css2?family=Roboto:wght@400;500&display=swap"" rel=""stylesheet"">

<div id=""debug-toolbar"" style=""position: fixed; bottom: 0; left: 0; right: 0; height: 40px; background-color: #333; color: white; z-index: 9999; transition: height 0.3s; font-family: 'Roboto', sans-serif;"">
    <div id=""toolbar-header"" style=""padding: 10px; display: flex; justify-content: space-between; cursor: pointer;"">
        <span style=""font-size: 16px; font-weight: 500;"">NanoDBProfiler Toolbar</span>
    </div>
    <div id=""debug-content"" style=""display: none; background-color: #444; height: 0; overflow: hidden; transition: height 0.3s;"">
        <iframe src=""/query-log"" style=""width: 100%; height: 100%; border: none; overflow: auto;""></iframe>
    </div>
</div>

<script>
    document.getElementById('toolbar-header').addEventListener('click', function() {
        var content = document.getElementById('debug-content');
        var toolbar = document.getElementById('debug-toolbar');
        
        if (content.style.display === 'none' || content.style.height === '0px') {
            // Expand the toolbar
            content.style.display = 'block';
            content.style.height = '300px'; // Set the desired height
            toolbar.style.height = '340px'; // Adjust the toolbar height accordingly
        } else {
            // Collapse the toolbar
            content.style.height = '0';
            setTimeout(() => content.style.display = 'none', 300); // Wait for the transition to complete
            toolbar.style.height = '40px'; // Reset the toolbar height
        }
    });
</script>

";
    }

}