
using System.Net.Http.Json;

using Example;

using Microsoft.AspNetCore.Mvc.Testing;

using NanoDbProfiler.AspNetCore;

using Shouldly;

namespace NanoDbProfiler.Specs.StepDefinitions;

[Binding]
public sealed class QueryTestsStepDefinitions : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient client;

    public QueryTestsStepDefinitions(WebApplicationFactory<Program> factory)
    {
        this._factory = factory;
        this.client = _factory.CreateClient();
    }

    [When(@"(.*)")]
    public async Task WhenExecutedAsync(string url)
    {
        // Arrange
        client.DefaultRequestHeaders.Add("accept", "application/json");
        await client.GetAsync("/insert");
        await client.DeleteAsync("/query-log");
        _ = await client.GetAsync("/");

        // Act
        await client.GetAsync(url);
    }

    [Then(@"query should be")]
    public async Task ThenLastSummaryShouldBeAsync(string expectedQuery)
    {
        // Assert
        var response = await client.GetAsync("/query-log");
        var data = await response.Content.ReadFromJsonAsync<DashboardData>();

        //@out.WriteLine(EfQueryLog.TextRepr(data));

        data.Summaries.ShouldNotBeEmpty();
        data.Summaries.Last().Query.ShouldBe(expectedQuery);
    }
}