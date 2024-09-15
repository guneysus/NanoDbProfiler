using Example;

using Microsoft.AspNetCore.Mvc.Testing;

using Shouldly;

namespace NanoDbProfiler.Tests
{
    public class SampleUnitTests : IClassFixture<WebApplicationFactory<Program>>
    {
        public SampleUnitTests (WebApplicationFactory<Program> factory) {
            _factory = factory;
        }

        public WebApplicationFactory<Program> _factory { get; }

        [Fact(DisplayName = "Check plain text query log")]
        public async Task Simplest_Test_Ever () {
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
    }
}