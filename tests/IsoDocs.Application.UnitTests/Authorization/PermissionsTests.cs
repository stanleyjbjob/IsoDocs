using FluentAssertions;
using IsoDocs.Application.Authorization;
using Xunit;

namespace IsoDocs.Application.UnitTests.Authorization;

public class PermissionsTests
{
    [Theory]
    [InlineData("roles.read", true)]
    [InlineData("roles.write", true)]
    [InlineData("users.read", true)]
    [InlineData("users.assign_roles", true)]
    [InlineData("admin.full_access", true)]
    [InlineData("unknown.permission", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsKnown_Should_Match_All_Constants(string permission, bool expected)
    {
        Permissions.IsKnown(permission).Should().Be(expected);
    }

    [Fact]
    public void All_Should_Contain_Every_Constant_Field()
    {
        Permissions.All.Should().Contain(new[]
        {
            Permissions.RolesRead,
            Permissions.RolesWrite,
            Permissions.UsersRead,
            Permissions.UsersAssignRoles,
            Permissions.UsersInvite,
            Permissions.AdminFullAccess
        });
        Permissions.All.Distinct().Count().Should().Be(Permissions.All.Count, "All 不可含重複");
    }

    [Fact]
    public void SystemAdminDefaults_Should_Contain_AdminFullAccess()
    {
        Permissions.SystemAdminDefaults.Should().Contain(Permissions.AdminFullAccess);
    }
}
