using FluentAssertions;
using Xunit;

namespace IsoDocs.Api.IntegrationTests.Endpoints;

public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Should_Return_200_Without_Authentication()
    {
        // [AllowAnonymous] 應跳過 FallbackPolicy.RequireAuthenticatedUser
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"status\":\"ok\"");
    }
}
