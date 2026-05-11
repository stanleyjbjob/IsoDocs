using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace IsoDocs.Api.IntegrationTests.Endpoints;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Should_Return_OIDC_Settings()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("authority");
        body.Should().Contain("clientId");
        body.Should().Contain("audience");
        body.Should().Contain("access_as_user");
    }

    [Fact]
    public async Task Logout_Without_Auth_Should_Return_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_With_Test_Auth_Should_Return_200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.SchemeName,
            TestAuthHandler.EncodeClaims(new[]
            {
                new Claim("oid", Guid.NewGuid().ToString()),
                new Claim("name", "Test User")
            }));

        var response = await client.PostAsync("/api/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
