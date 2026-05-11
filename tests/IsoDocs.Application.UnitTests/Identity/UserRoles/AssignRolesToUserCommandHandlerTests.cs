using FluentAssertions;
using IsoDocs.Application.Identity.UserRoles.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Identity.UserRoles;

public class AssignRolesToUserCommandHandlerTests
{
    [Fact]
    public async Task Assign_Multiple_Roles_Should_Persist_All_UserRoles()
    {
        var roles = new FakeRoleRepository();
        var r1 = roles.Seed("A");
        var r2 = roles.Seed("B");
        var userRoles = new FakeUserRoleRepository();
        var handler = new AssignRolesToUserCommandHandler(roles, userRoles);
        var userId = Guid.NewGuid();

        var cmd = new AssignRolesToUserCommand(
            UserId: userId,
            RoleIds: new[] { r1.Id, r2.Id },
            EffectiveFrom: null,
            EffectiveTo: null,
            AssignedByUserId: null);
        await handler.Handle(cmd, CancellationToken.None);

        userRoles.Store.Should().HaveCount(2);
        userRoles.Store.Should().AllSatisfy(ur => ur.UserId.Should().Be(userId));
        userRoles.Store.Select(ur => ur.RoleId).Should().BeEquivalentTo(new[] { r1.Id, r2.Id });
    }

    [Fact]
    public async Task Assign_With_Missing_RoleId_Should_Throw_AssignmentFailed()
    {
        var roles = new FakeRoleRepository();
        var existing = roles.Seed("A");
        var userRoles = new FakeUserRoleRepository();
        var handler = new AssignRolesToUserCommandHandler(roles, userRoles);

        var cmd = new AssignRolesToUserCommand(
            UserId: Guid.NewGuid(),
            RoleIds: new[] { existing.Id, Guid.NewGuid() }, // 第二個不存在
            EffectiveFrom: null,
            EffectiveTo: null,
            AssignedByUserId: null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("ROLE/ASSIGNMENT_FAILED");
        userRoles.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Assign_With_Inactive_Role_Should_Throw_AssignmentFailed()
    {
        var roles = new FakeRoleRepository();
        var inactive = roles.Seed("已停用", isActive: false);
        var userRoles = new FakeUserRoleRepository();
        var handler = new AssignRolesToUserCommandHandler(roles, userRoles);

        var cmd = new AssignRolesToUserCommand(
            UserId: Guid.NewGuid(),
            RoleIds: new[] { inactive.Id },
            EffectiveFrom: null,
            EffectiveTo: null,
            AssignedByUserId: null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("ROLE/ASSIGNMENT_FAILED");
    }

    [Fact]
    public async Task Assign_Without_EffectiveFrom_Should_Default_To_Now()
    {
        var roles = new FakeRoleRepository();
        var role = roles.Seed("A");
        var userRoles = new FakeUserRoleRepository();
        var handler = new AssignRolesToUserCommandHandler(roles, userRoles);
        var before = DateTimeOffset.UtcNow;

        var cmd = new AssignRolesToUserCommand(
            UserId: Guid.NewGuid(),
            RoleIds: new[] { role.Id },
            EffectiveFrom: null,
            EffectiveTo: null,
            AssignedByUserId: null);
        await handler.Handle(cmd, CancellationToken.None);

        var after = DateTimeOffset.UtcNow;
        userRoles.Store.Should().HaveCount(1);
        userRoles.Store[0].EffectiveFrom.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        userRoles.Store[0].EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task Assign_With_Explicit_EffectiveTo_Should_Be_Stored()
    {
        var roles = new FakeRoleRepository();
        var role = roles.Seed("A");
        var userRoles = new FakeUserRoleRepository();
        var handler = new AssignRolesToUserCommandHandler(roles, userRoles);
        var until = DateTimeOffset.UtcNow.AddDays(7);

        var cmd = new AssignRolesToUserCommand(
            UserId: Guid.NewGuid(),
            RoleIds: new[] { role.Id },
            EffectiveFrom: null,
            EffectiveTo: until,
            AssignedByUserId: null);
        await handler.Handle(cmd, CancellationToken.None);

        userRoles.Store[0].EffectiveTo.Should().Be(until);
    }
}
