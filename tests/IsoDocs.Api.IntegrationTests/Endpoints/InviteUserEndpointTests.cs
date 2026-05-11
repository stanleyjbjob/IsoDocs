using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using IsoDocs.Application.Users.Commands.InviteUser;
using Xunit;

namespace IsoDocs.Api.IntegrationTests.Endpoints;

public class InviteUserEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public InviteUserEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Invite_Without_Auth_Should_Return_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/invite", new
        {
            email = "test@example.com",
            displayName = "Test User",
            roleId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invite_By_Non_Admin_Should_Return_422()
    {
        var oid = Guid.NewGuid().ToString();
        _factory.UserRepo.SeedUser(oid, "nonadmin@example.com", "Non Admin", isSystemAdmin: false);
        var role = _factory.UserRepo.SeedRole("Viewer");

        var client = AuthenticatedClient(oid);
        var response = await client.PostAsJsonAsync("/api/users/invite", new
        {
            email = "newuser@example.com",
            displayName = "New User",
            roleId = role.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("users.invite.not_admin");
    }

    [Fact]
    public async Task Invite_By_Admin_Should_Return_200_And_Create_User()
    {
        var adminOid = Guid.NewGuid().ToString();
        _factory.UserRepo.SeedUser(adminOid, "admin@example.com", "Admin", isSystemAdmin: true);
        var role = _factory.UserRepo.SeedRole("Member");

        var client = AuthenticatedClient(adminOid);
        var response = await client.PostAsJsonAsync("/api/users/invite", new
        {
            email = "invited@example.com",
            displayName = "Invited User",
            roleId = role.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InviteUserResult>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("invited@example.com");
        result.InviteRedeemUrl.Should().NotBeNullOrEmpty();

        _factory.GraphInvitation.SentInvitations
            .Should().ContainSingle(i => i.Email == "invited@example.com");

        _factory.UserRepo.Users
            .Should().Contain(u => u.Email == "invited@example.com");
    }

    [Fact]
    public async Task Invite_Duplicate_Email_Should_Return_422()
    {
        var adminOid = Guid.NewGuid().ToString();
        _factory.UserRepo.SeedUser(adminOid, "admin2@example.com", "Admin2", isSystemAdmin: true);
        _factory.UserRepo.SeedUser(Guid.NewGuid().ToString(), "existing@example.com", "Existing");
        var role = _factory.UserRepo.SeedRole("Viewer2");

        var client = AuthenticatedClient(adminOid);
        var response = await client.PostAsJsonAsync("/api/users/invite", new
        {
            email = "existing@example.com",
            displayName = "Existing User",
            roleId = role.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("users.invite.email_exists");
    }

    [Fact]
    public async Task Invite_With_Invalid_Email_Should_Return_400()
    {
        var adminOid = Guid.NewGuid().ToString();
        _factory.UserRepo.SeedUser(adminOid, "admin3@example.com", "Admin3", isSystemAdmin: true);

        var client = AuthenticatedClient(adminOid);
        var response = await client.PostAsJsonAsync("/api/users/invite", new
        {
            email = "not-an-email",
            displayName = "Bad User",
            roleId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invite_With_Nonexistent_Role_Should_Return_422()
    {
        var adminOid = Guid.NewGuid().ToString();
        _factory.UserRepo.SeedUser(adminOid, "admin4@example.com", "Admin4", isSystemAdmin: true);

        var client = AuthenticatedClient(adminOid);
        var response = await client.PostAsJsonAsync("/api/users/invite", new
        {
            email = "noroler@example.com",
            displayName = "No Role",
            roleId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("users.invite.role_not_found");
    }

    private HttpClient AuthenticatedClient(string oid)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.SchemeName,
            TestAuthHandler.EncodeClaims(new[]
            {
                new Claim("oid", oid),
                new Claim("tid", "00000000-0000-0000-0000-000000000001"),
                new Claim("email", "user@example.com"),
                new Claim("name", "Test User"),
            }));
        return client;
    }
}
