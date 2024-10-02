namespace Microsoft.Extensions.DependencyInjection;

internal static class EmbeddedResourceHelpers
{

    public static string GetResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceFullname = $"NanoDbProfiler.AspNetCore.Resources.{resourceName}";
        using var stream = assembly.GetManifestResourceStream(resourceFullname);

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = new StreamReader(stream);
        var toolbarHtml = reader.ReadToEnd();
        return toolbarHtml;
    }
}