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
<style>
    /* General scrollbar styling for the page */
    ::-webkit-scrollbar {
        width: 8px;
        height: 8px;
    }

    ::-webkit-scrollbar-track {
        background: #333;
    }

    ::-webkit-scrollbar-thumb {
        background-color: #555;
        border-radius: 4px;
    }

    ::-webkit-scrollbar-thumb:hover {
        background-color: #777;
    }

    /* Toolbar scrollbar styling */
    #query-log::-webkit-scrollbar {
        width: 8px;
    }

    #query-log::-webkit-scrollbar-track {
        background: #222;
    }

    #query-log::-webkit-scrollbar-thumb {
        background-color: #555;
        border-radius: 4px;
    }

    #query-log::-webkit-scrollbar-thumb:hover {
        background-color: #777;
    }

    /* Style for visually separating records */
    .query-record {
        border: 1px solid #555;
        padding: 10px;
        border-radius: 5px;
        margin-bottom: 10px;
        background-color: #2c2c2c;
        position: relative;
    }

    /* Duration bar styling */
    .duration-bar {
        height: 10px;
        background-color: #4CAF50; /* Default color for short queries */
        border-radius: 5px;
        margin-top: 5px;
    }

    /* Style for the buttons */
    .toolbar-button {
        background-color: #4CAF50;
        color: white;
        border: none;
        padding: 5px 10px;
        cursor: pointer;
        border-radius: 3px;
    }

    .toolbar-button.clear {
        background-color: #ff5555;
    }

    .toolbar-button:hover {
        opacity: 0.9;
    }

    /* Toolbar header and content styling */
    #debug-toolbar {
        font-family: 'Roboto', sans-serif;
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        height: 40px;
        background-color: #333;
        color: white;
        z-index: 9999;
        transition: height 0.3s;
    }

    #toolbar-header {
        padding: 10px;
        display: flex;
        justify-content: space-between;
        cursor: pointer;
    }

    #debug-content {
        padding: 10px;
        height: 0;
        overflow: hidden;
        transition: height 0.3s ease-in-out;
    }
</style>

<div id=""debug-toolbar"">
    <div id=""toolbar-header"">
        <span style=""font-size: 16px; font-weight: 500;"">NanoDBProfiler Toolbar</span>
    </div>
    <div id=""debug-content"">
        <div style=""display: flex; justify-content: flex-start; gap: 10px; margin-bottom: 10px;"">
            <button id=""clear-log"" class=""toolbar-button clear"">Clear Log</button>
            <button id=""refresh-log"" class=""toolbar-button"">Refresh Logs</button>
        </div>

        <div id=""query-log"" style=""max-height: 200px; overflow-y: auto; background-color: #333; padding: 10px; border-radius: 5px;"">
            <!-- Logs will appear here -->
        </div>
    </div>
</div>

<script>
    document.getElementById('toolbar-header').addEventListener('click', function () {
        var content = document.getElementById('debug-content');
        var toolbar = document.getElementById('debug-toolbar');

        // Toggling the toolbar height and content visibility
        if (toolbar.style.height === '340px') {
            // Collapse
            content.style.height = '0';
            toolbar.style.height = '40px';
        } else {
            // Expand
            toolbar.style.height = '340px';
            content.style.height = '300px';
            loadQueryLog(); // Load the query log when expanding
        }
    });

    // Function to load the query log via XHR
    function loadQueryLog() {
        var queryLogElement = document.getElementById('query-log');
        queryLogElement.innerHTML = 'Loading...'; // Show loading text while fetching data

        var xhr = new XMLHttpRequest();
        xhr.open('GET', '/query-log', true);
        xhr.setRequestHeader('Accept', 'application/json');
        xhr.onload = function () {
            if (xhr.status >= 200 && xhr.status < 300) {
                var data = JSON.parse(xhr.responseText);
                renderQueryLog(data);
            } else {
                queryLogElement.innerHTML = 'Failed to load query log.';
            }
        };
        xhr.onerror = function () {
            queryLogElement.innerHTML = 'Failed to load query log.';
        };
        xhr.send();
    }

    // Function to render the query log data
    function renderQueryLog(data) {
        var queryLogElement = document.getElementById('query-log');
        queryLogElement.innerHTML = ''; // Clear previous content

        if (data.summaries && data.summaries.length > 0) {
            // Find the max duration for scaling the visual representation
            var maxDuration = Math.max(...data.summaries.map(s => s.p95));

            data.summaries.forEach(function (summary) {
                var queryDiv = document.createElement('div');
                queryDiv.classList.add('query-record'); // Apply the record styling

                var queryText = document.createElement('pre');
                queryText.textContent = summary.query;
                queryText.style.whiteSpace = 'pre-wrap'; // Allow queries to wrap

                // Visual duration bar
                var durationBar = document.createElement('div');
                durationBar.classList.add('duration-bar');
                durationBar.style.width = (summary.p95 / maxDuration * 100) + '%';

                // Change bar color based on duration
                if (summary.p95 > maxDuration * 0.75) {
                    durationBar.style.backgroundColor = '#ff5555'; // Red for long queries
                } else if (summary.p95 > maxDuration * 0.5) {
                    durationBar.style.backgroundColor = '#ffbf00'; // Yellow for moderate queries
                }

                var durationText = document.createElement('span');
                durationText.textContent = summary.p95.toFixed(3) + 'ms';
                durationText.style.position = 'absolute';
                durationText.style.right = '10px';
                durationText.style.top = '10px';
                durationText.style.fontWeight = 'bold';

                queryDiv.appendChild(queryText);
                queryDiv.appendChild(durationBar);
                queryDiv.appendChild(durationText);
                queryLogElement.appendChild(queryDiv);
            });
        } else {
            queryLogElement.innerHTML = 'No query summaries available.';
        }
    }

    // Function to clear the query log via DELETE request
    document.getElementById('clear-log').addEventListener('click', function () {
        var xhr = new XMLHttpRequest();
        xhr.open('DELETE', '/query-log', true);
        xhr.onload = function () {
            if (xhr.status >= 200 && xhr.status < 300) {
                // Clear the log in the UI after successful delete
                document.getElementById('query-log').innerHTML = 'Query log cleared.';
            } else {
                alert('Failed to clear query log.');
            }
        };
        xhr.onerror = function () {
            alert('Failed to clear query log.');
        };
        xhr.send();
    });

    // Function to refresh the query log via XHR
    document.getElementById('refresh-log').addEventListener('click', function () {
        loadQueryLog(); // Refresh the log by reloading the data
    });
</script>
";
    }

}