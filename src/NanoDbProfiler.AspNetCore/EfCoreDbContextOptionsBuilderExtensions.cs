namespace Microsoft.EntityFrameworkCore;

public static class EfCoreDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder AddNanoDbProfilerEfCoreInterceptor(this DbContextOptionsBuilder o)
    {
        return o.AddInterceptors(new NanoDbProfilerEfCoreQueryInterceptor());
    }
}
