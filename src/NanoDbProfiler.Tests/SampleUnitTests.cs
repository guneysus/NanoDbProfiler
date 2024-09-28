using System.Net.Http.Json;

using Example;

using Microsoft.Extensions.DependencyInjection;

using NanoDbProfiler.AspNetCore;

using Shouldly;

using Xunit.Abstractions;

namespace NanoDbProfiler.Tests;

public class SampleUnitTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper @out;

    public SampleUnitTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper @out)
    {
        _factory = factory;
        this.@out = @out;
    }

    public CustomWebApplicationFactory<Program> _factory { get; }

    [Fact(DisplayName = "Check plain text query log")]
    public async Task Simplest_Test_Ever()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        _ = await client.GetAsync("/");
        var response = await client.GetAsync("/query-log");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        response.Content.Headers.ContentType.MediaType.ShouldBe("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("/insert", @"INSERT INTO ""Todos"" (""Title"")
VALUES (@p0)
RETURNING ""Id"";")]
    [InlineData("/update", @"UPDATE ""Todos"" SET ""Title"" = @p0
WHERE ""Id"" = @p1
RETURNING 1;")]
    [InlineData("/select/scalar", @"SELECT ""t"".""Id""
FROM ""Todos"" AS ""t""")]
    [InlineData("/select/single/1", @"SELECT ""t"".""Id"", ""t"".""Title""
FROM ""Todos"" AS ""t""
LIMIT 1")]
    [InlineData("/select/single/2", @"SELECT ""t"".""Id"", ""t"".""Title""
FROM ""Todos"" AS ""t""
LIMIT 1 OFFSET @__p_0")]
    [InlineData("/select/all", @"SELECT ""t"".""Id"", ""t"".""Title""
FROM ""Todos"" AS ""t""")]
    [InlineData("/delete/single", @"DELETE FROM ""Todos""
WHERE ""Id"" = @p0
RETURNING 1;")]
    [InlineData("/delete/all", @"DELETE FROM ""Todos"" AS ""t""")]
    public async Task Insert_QueryAsync(string url, string expectedQuery)
    {
        // Arrange
        var client = _factory.CreateClient();
        await client.GetAsync("/insert");
        await client.DeleteAsync("/query-log");
        client.DefaultRequestHeaders.Add("accept", "application/json");

        // Act
        _ = await client.GetAsync(url);
        //_ = await client.GetAsync("/");

        // Assert
        var response = await client.GetAsync("/query-log");
        var data = await response.Content.ReadFromJsonAsync<DashboardData>();
        @out.WriteLine(EfQueryLog.TextRepr(data));

        data.Summaries.ShouldNotBeEmpty();
        data.Summaries.Last().Query.ShouldBe(expectedQuery);

    }
}