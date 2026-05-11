using FluentAssertions;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Identity.Roles.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Identity.Roles;

public class UpdateRoleCommandHandlerTests
{
    [Fact]
    public async Task Update_With_Existing_Role_Should_Modify_Fields()
    {
        var repo = new FakeRoleRepository();
        var role = repo.Seed("原名稱");
        var handler = new UpdateRoleCommandHandler(repo);
        var cmd = new UpdateRoleCommand(
            RoleId: role.Id,
            Name: "新名稱",
            Description: "新描述",
            Permissions: new[] { Permissions.RolesWrite });

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Name.Should().Be("新名稱");
        dto.Description.Should().Be("新描述");
        dto.Permissions.Should().BeEquivalentTo(new[] { Permissions.RolesWrite });
        repo.Store[role.Id].Name.Should().Be("新名稱");
    }

    [Fact]
    public async Task Update_NonExistent_Role_Should_Throw_NotFound()
    {
        var repo = new FakeRoleRepository();
        var handler = new UpdateRoleCommandHandler(repo);
        var cmd = new UpdateRoleCommand(Guid.NewGuid(), "x", null, Array.Empty<string>());

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("ROLE/NOT_FOUND");
    }

    [Fact]
    public async Task Update_With_Conflicting_Name_Should_Throw_NameDuplicate()
    {
        var repo = new FakeRoleRepository();
        repo.Seed("已存在名稱");
        var target = repo.Seed("原名稱");
        var handler = new UpdateRoleCommandHandler(repo);
        var cmd = new UpdateRoleCommand(target.Id, "已存在名稱", null, Array.Empty<string>());

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("ROLE/NAME_DUPLICATE");
    }

    [Fact]
    public async Task Update_With_Same_Name_Should_Not_Trigger_Duplicate_Check()
    {
        // 同一個 role 保持名稱不變時不應評為重複。
        var repo = new FakeRoleRepository();
        var role = repo.Seed("不變名稱");
        var handler = new UpdateRoleCommandHandler(repo);
        var cmd = new UpdateRoleCommand(role.Id, "不變名稱", "新描述", Array.Empty<string>());

        var dto = await handler.Handle(cmd, CancellationToken.None);
        dto.Description.Should().Be("新描述");
    }
}
