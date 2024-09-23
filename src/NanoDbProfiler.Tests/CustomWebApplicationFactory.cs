using Microsoft.AspNetCore.Mvc.Testing;

namespace NanoDbProfiler.Tests;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{

}
