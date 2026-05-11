using System.Text.Json;
using FluentAssertions;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Identity.Roles.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Identity.Roles;

public class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Create_With_Valid_Input_Should_Persist_Role_And_Return_Dto()
    {
        var repo = new FakeRoleRepository();
        var handler = new CreateRoleCommandHandler(repo);
        var cmd = new CreateRoleCommand(
            Name: "質保主管",
            Description: "質保部門主管",
            Permissions: new[] { Permissions.RolesRead, Permissions.UsersRead });

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Name.Should().Be("質保主管");
        dto.Description.Should().Be("質保部門主管");
        dto.Permissions.Should().BeEquivalentTo(new[] { Permissions.RolesRead, Permissions.UsersRead });
        dto.IsSystemRole.Should().BeFalse();
        dto.IsActive.Should().BeTrue();
        repo.Store.Should().ContainKey(dto.Id);
        // 確保 PermissionsJson 實際被序列化存入
        var stored = repo.Store[dto.Id];
        var deserialized = JsonSerializer.Deserialize<string[]>(stored.PermissionsJson);
        deserialized.Should().BeEquivalentTo(new[] { Permissions.RolesRead, Permissions.UsersRead });
    }

    [Fact]
    public async Task Create_With_Duplicate_Name_Should_Throw_DomainException()
    {
        var repo = new FakeRoleRepository();
        repo.Seed(name: "重複名");
        var handler = new CreateRoleCommandHandler(repo);
        var cmd = new CreateRoleCommand("重複名", null, Array.Empty<string>());

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("ROLE/NAME_DUPLICATE");
    }

    [Fact]
    public async Task Create_With_Empty_Permissions_Should_Succeed()
    {
        var repo = new FakeRoleRepository();
        var handler = new CreateRoleCommandHandler(repo);
        var cmd = new CreateRoleCommand("無權限角色", null, Array.Empty<string>());

        var dto = await handler.Handle(cmd, CancellationToken.None);
        dto.Permissions.Should().BeEmpty();
    }
}
