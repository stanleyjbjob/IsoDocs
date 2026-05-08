using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using IsoDocs.Application.Auth;
using Xunit;

namespace IsoDocs.Api.IntegrationTests.Endpoints;

public class MeEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MeEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_Without_Authorization_Header_Should_Return_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_With_Valid_Token_Should_Upsert_User_And_Return_CurrentUserDto()
    {
        var oid = Guid.NewGuid().ToString();
        var client = AuthenticatedClient(oid, email: "alice@example.com", name: "Alice");

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        dto.Should().NotBeNull();
        dto!.AzureAdObjectId.Should().Be(oid);
        dto.Email.Should().Be("alice@example.com");
        dto.DisplayName.Should().Be("Alice");
        dto.IsActive.Should().BeTrue();
        dto.TenantId.Should().Be("00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public async Task Me_With_Force_Deactivated_User_Should_Return_403_With_AuthErrorCode()
    {
        var oid = Guid.NewGuid().ToString();
        _factory.UserSync.Seed(oid, "bob@example.com", "Bob");
        _factory.UserSync.ForcePermanentDeactivation(oid);

        var client = AuthenticatedClient(oid, email: "bob@example.com", name: "Bob");
        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(AuthErrorCodes.UserDeactivated);
    }

    [Fact]
    public async Task Me_With_Token_Missing_Oid_Should_Return_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.SchemeName,
            TestAuthHandler.EncodeClaims(new[]
            {
                new Claim("name", "NoOidUser")
                // 故意不放 oid
            }));

        var response = await client.GetAsync("/api/me");

        // ToAzureAdUserPrincipal 解析後 IsAuthenticated=false → MeController 回 401 + AUTH/MISSING_OID
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(AuthErrorCodes.MissingObjectId);
    }

    private HttpClient AuthenticatedClient(string oid, string email, string name)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.SchemeName,
            TestAuthHandler.EncodeClaims(new[]
            {
                new Claim("oid", oid),
                new Claim("tid", "00000000-0000-0000-0000-000000000001"),
                new Claim("email", email),
                new Claim("name", name)
            }));
        return client;
    }
}
