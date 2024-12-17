namespace Microsoft.Extensions.DependencyInjection;

public class NanoDbProfilerConfiguration
{
    public bool ToolbarEnabled { get; set; }
    public string ProfilerPageUrl { get; set; } = "query-log";
}