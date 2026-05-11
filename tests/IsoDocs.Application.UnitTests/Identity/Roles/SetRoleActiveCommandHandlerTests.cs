using FluentAssertions;
using IsoDocs.Application.Identity.Roles.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Identity.Roles;

public class SetRoleActiveCommandHandlerTests
{
    [Fact]
    public async Task Deactivate_Active_Role_Should_Set_IsActive_False()
    {
        var repo = new FakeRoleRepository();
        var role = repo.Seed("可停用");
        var handler = new SetRoleActiveCommandHandler(repo);

        await handler.Handle(new SetRoleActiveCommand(role.Id, IsActive: false), CancellationToken.None);

        repo.Store[role.Id].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Activate_Deactivated_Role_Should_Set_IsActive_True()
    {
        var repo = new FakeRoleRepository();
        var role = repo.Seed("已停用", isActive: false);
        var handler = new SetRoleActiveCommandHandler(repo);

        await handler.Handle(new SetRoleActiveCommand(role.Id, IsActive: true), CancellationToken.None);

        repo.Store[role.Id].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_SystemRole_Should_Throw_DomainException()
    {
        var repo = new FakeRoleRepository();
        var role = repo.Seed("系統管理者", isSystemRole: true);
        var handler = new SetRoleActiveCommandHandler(repo);

        var act = async () => await handler.Handle(new SetRoleActiveCommand(role.Id, IsActive: false), CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Activate_NonExistent_Role_Should_Throw_NotFound()
    {
        var repo = new FakeRoleRepository();
        var handler = new SetRoleActiveCommandHandler(repo);

        var act = async () => await handler.Handle(new SetRoleActiveCommand(Guid.NewGuid(), IsActive: true), CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("ROLE/NOT_FOUND");
    }
}
