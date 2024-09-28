
using System.Net.Http.Json;

using Example;

using Microsoft.AspNetCore.Mvc.Testing;

using NanoDbProfiler.AspNetCore;

using Shouldly;

namespace NanoDbProfiler.Specs.StepDefinitions;

[Binding]
public sealed class QueryTestsStepDefinitions : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private string _url;

    public QueryTestsStepDefinitions(WebApplicationFactory<Program> factory)
    {
        this._factory = factory;
    }

    [Given(@"the endpoint is (.*)")]
    public async Task GivenTheEndpointIsInsertAsync(string url)
    {
        // Arrange
        this._client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        this._url = url;
        await _client.GetAsync("/insert");
        await _client.DeleteAsync("/query-log");
        _ = await _client.GetAsync("/");
    }


    [When(@"executed")]
    public async Task WhenExecutedAsync()
    {
        // Act

        _ = await _client.GetAsync(_url);
    }


    [Then(@"profiled query should be")]
    public async Task ThenLastSummaryShouldBeAsync(string expectedQuery)
    {
        // Assert
        var response = await _client.GetAsync("/query-log");
        var data = await response.Content.ReadFromJsonAsync<DashboardData>();

        //@out.WriteLine(EfQueryLog.TextRepr(data));

        data.Summaries.ShouldNotBeEmpty();
        data.Summaries.Last().Query.ShouldBe(expectedQuery);
    }

    public void Dispose()
    {

    }
}