using FluentAssertions;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Domain.Identity;
using Xunit;

namespace IsoDocs.Application.UnitTests.Identity.Roles;

public class RoleDtoMapperTests
{
    [Fact]
    public void ParsePermissions_With_Valid_Json_Should_Return_Array()
    {
        var result = RoleDtoMapper.ParsePermissions("[\"roles.read\",\"users.read\"]");
        result.Should().BeEquivalentTo(new[] { "roles.read", "users.read" });
    }

    [Fact]
    public void ParsePermissions_With_Null_Or_Empty_Should_Return_Empty()
    {
        RoleDtoMapper.ParsePermissions(null).Should().BeEmpty();
        RoleDtoMapper.ParsePermissions("").Should().BeEmpty();
        RoleDtoMapper.ParsePermissions("   ").Should().BeEmpty();
    }

    [Fact]
    public void ParsePermissions_With_Malformed_Json_Should_Return_Empty()
    {
        RoleDtoMapper.ParsePermissions("{not valid}").Should().BeEmpty();
        RoleDtoMapper.ParsePermissions("['unquoted-single']").Should().BeEmpty();
    }

    [Fact]
    public void ToDto_Should_Project_All_Fields()
    {
        var role = new Role(Guid.NewGuid(), "X", "[\"roles.read\"]", isSystemRole: true);
        role.Update("X", "desc", "[\"roles.read\"]");

        var dto = RoleDtoMapper.ToDto(role);

        dto.Id.Should().Be(role.Id);
        dto.Name.Should().Be("X");
        dto.Description.Should().Be("desc");
        dto.Permissions.Should().BeEquivalentTo(new[] { "roles.read" });
        dto.IsSystemRole.Should().BeTrue();
        dto.IsActive.Should().BeTrue();
    }
}
