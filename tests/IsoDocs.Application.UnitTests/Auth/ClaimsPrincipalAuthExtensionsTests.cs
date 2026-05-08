using System.Security.Claims;
using FluentAssertions;
using IsoDocs.Application.Auth;

namespace IsoDocs.Application.UnitTests.Auth;

public class ClaimsPrincipalAuthExtensionsTests
{
    private static ClaimsPrincipal Make(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.Type, c.Value)),
            authenticationType: "TestBearer");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void ToAzureAdUserPrincipal_NoClaims_ShouldReturnEmptyButNotNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var dto = principal.ToAzureAdUserPrincipal();

        dto.AzureAdObjectId.Should().BeEmpty();
        dto.IsAuthenticated.Should().BeFalse();
        dto.Roles.Should().BeEmpty();
        dto.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void ToAzureAdUserPrincipal_FullClaims_ShouldMapAllFields()
    {
        var p = Make(
            ("oid", "00000000-0000-0000-0000-000000000001"),
            ("tid", "tenant-abc"),
            ("name", "Alice"),
            ("email", "alice@example.com"),
            ("department", "質管部"),
            ("jobTitle", "工程師"),
            ("roles", "Admin"),
            ("roles", "Auditor"),
            ("scp", "Cases.Read Cases.Write"));

        var dto = p.ToAzureAdUserPrincipal();

        dto.AzureAdObjectId.Should().Be("00000000-0000-0000-0000-000000000001");
        dto.TenantId.Should().Be("tenant-abc");
        dto.Email.Should().Be("alice@example.com");
        dto.DisplayName.Should().Be("Alice");
        dto.Department.Should().Be("質管部");
        dto.JobTitle.Should().Be("工程師");
        dto.Roles.Should().BeEquivalentTo(new[] { "Admin", "Auditor" });
        dto.Scopes.Should().BeEquivalentTo(new[] { "Cases.Read", "Cases.Write" });
        dto.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ToAzureAdUserPrincipal_OidOnlyAsSchemaUri_ShouldStillResolve()
    {
        var p = Make(
            ("http://schemas.microsoft.com/identity/claims/objectidentifier", "oid-from-uri"),
            ("http://schemas.microsoft.com/identity/claims/tenantid", "tid-from-uri"));

        var dto = p.ToAzureAdUserPrincipal();

        dto.AzureAdObjectId.Should().Be("oid-from-uri");
        dto.TenantId.Should().Be("tid-from-uri");
    }

    [Fact]
    public void ToAzureAdUserPrincipal_EmailFallbacks_ShouldRespectOrder()
    {
        // 只有 preferred_username，沒有 email，仍要能拿到
        var p = Make(
            ("oid", "abc"),
            ("preferred_username", "alice@upn.example.com"));

        p.ToAzureAdUserPrincipal().Email.Should().Be("alice@upn.example.com");

        // email 與 preferred_username 兩者都有時以 email 為先
        var p2 = Make(
            ("oid", "abc"),
            ("email", "primary@example.com"),
            ("preferred_username", "alice@upn.example.com"));

        p2.ToAzureAdUserPrincipal().Email.Should().Be("primary@example.com");
    }

    [Fact]
    public void ToAzureAdUserPrincipal_BuiltinRoleClaims_AreMerged()
    {
        var p = Make(
            ("oid", "abc"),
            ("roles", "Admin"),
            (ClaimTypes.Role, "Admin"),    // 重複 → 只保留一個
            (ClaimTypes.Role, "Reviewer"));

        var dto = p.ToAzureAdUserPrincipal();

        dto.Roles.Should().BeEquivalentTo(new[] { "Admin", "Reviewer" });
    }

    [Fact]
    public void ToAzureAdUserPrincipal_ScpWithExtraSpaces_ShouldTrimAndSkipEmpty()
    {
        var p = Make(
            ("oid", "abc"),
            ("scp", "  Cases.Read    Cases.Write   "));

        var dto = p.ToAzureAdUserPrincipal();

        dto.Scopes.Should().BeEquivalentTo(new[] { "Cases.Read", "Cases.Write" });
    }

    [Fact]
    public void ToAzureAdUserPrincipal_NullPrincipal_ShouldThrow()
    {
        ClaimsPrincipal? p = null;
        var act = () => p!.ToAzureAdUserPrincipal();

        act.Should().Throw<ArgumentNullException>();
    }
}
