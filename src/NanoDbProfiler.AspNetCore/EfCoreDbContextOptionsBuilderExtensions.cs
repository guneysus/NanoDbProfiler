namespace Microsoft.EntityFrameworkCore;

public static class EfCoreDbContextOptionsBuilderExtensions
{
    public static object AddNanoDbProfilerEfCoreInterceptor(object dbContextOptionsBuilder)
    {
        // Load the DbContextOptionsBuilder type dynamically
        var dbContextOptionsBuilderType = dbContextOptionsBuilder.GetType();

        // Load the DbCommandInterceptor type dynamically
        var dbCommandInterceptorType = Type.GetType("Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor");

        if (dbCommandInterceptorType == null)
        {
            throw new InvalidOperationException("Unable to find DbCommandInterceptor type.");
        }

        // Check if your class NanoDbProfilerEfCoreQueryInterceptor inherits from DbCommandInterceptor
        var nanoDbProfilerEfCoreQueryInterceptorType = typeof(NanoDbProfilerEfCoreQueryInterceptor);

        if (!dbCommandInterceptorType.IsAssignableFrom(nanoDbProfilerEfCoreQueryInterceptorType))
        {
            throw new InvalidOperationException($"{nanoDbProfilerEfCoreQueryInterceptorType.Name} does not inherit from DbCommandInterceptor.");
        }

        // Create an instance of NanoDbProfilerEfCoreQueryInterceptor dynamically
        var interceptorInstance = Activator.CreateInstance(nanoDbProfilerEfCoreQueryInterceptorType);

        // Get the AddInterceptors method of DbContextOptionsBuilder dynamically
        var addInterceptorsMethod = dbContextOptionsBuilderType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "AddInterceptors" && m.GetParameters().Length == 1);

        if (addInterceptorsMethod == null)
        {
            throw new InvalidOperationException("Unable to find the AddInterceptors method.");
        }

        // Invoke the AddInterceptors method dynamically
        return addInterceptorsMethod.Invoke(dbContextOptionsBuilder, new [] { interceptorInstance });
    }
}

