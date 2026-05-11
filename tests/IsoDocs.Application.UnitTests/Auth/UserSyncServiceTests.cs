using FluentAssertions;
using IsoDocs.Application.Auth;
using IsoDocs.Domain.Identity;
using IsoDocs.Infrastructure.Auth;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IsoDocs.Application.UnitTests.Auth;

/// <summary>
/// UserSyncService 的行為測試。使用 EF Core InMemory 提供者快速驗證 upsert 邏輯。
/// </summary>
public class UserSyncServiceTests
{
    private static IsoDocsDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<IsoDocsDbContext>()
            // 每次測試一個全新的 InMemory database，避免跨測試污染。
            .UseInMemoryDatabase(databaseName: $"isodocs-{Guid.NewGuid()}")
            .Options;
        return new IsoDocsDbContext(options);
    }

    private static AzureAdUserPrincipal MakePrincipal(
        string oid = "00000000-0000-0000-0000-000000000001",
        string email = "alice@example.com",
        string name = "Alice",
        string? department = null,
        string? jobTitle = null,
        string tenantId = "tenant-1",
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? scopes = null)
        => new(
            AzureAdObjectId: oid,
            TenantId: tenantId,
            Email: email,
            DisplayName: name,
            Department: department,
            JobTitle: jobTitle,
            Roles: roles ?? Array.Empty<string>(),
            Scopes: scopes ?? Array.Empty<string>());

    [Fact]
    public async Task UpsertFromAzureAdAsync_NewUser_ShouldCreateAndPersist()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        var principal = MakePrincipal(department: "質管部", jobTitle: "工程師");
        var user = await sut.UpsertFromAzureAdAsync(principal);

        user.Should().NotBeNull();
        user.AzureAdObjectId.Should().Be(principal.AzureAdObjectId);
        user.Email.Should().Be(principal.Email);
        user.DisplayName.Should().Be(principal.DisplayName);
        user.Department.Should().Be("質管部");
        user.JobTitle.Should().Be("工程師");
        user.IsActive.Should().BeTrue();

        var stored = await ctx.Users.SingleAsync();
        stored.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task UpsertFromAzureAdAsync_ExistingUser_ShouldUpdateProfile()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        // Arrange: 預先存一個使用者
        var existing = await sut.UpsertFromAzureAdAsync(MakePrincipal(
            email: "alice.old@example.com",
            name: "Alice Old",
            department: "舊部門",
            jobTitle: "舊職稱"));

        // Act: 同 oid 再 upsert，更新資料
        var refreshed = await sut.UpsertFromAzureAdAsync(MakePrincipal(
            email: "alice.new@example.com",
            name: "Alice New",
            department: "新部門",
            jobTitle: "新職稱"));

        refreshed.Id.Should().Be(existing.Id);
        refreshed.Email.Should().Be("alice.new@example.com");
        refreshed.DisplayName.Should().Be("Alice New");
        refreshed.Department.Should().Be("新部門");
        refreshed.JobTitle.Should().Be("新職稱");

        (await ctx.Users.CountAsync()).Should().Be(1, "同 oid 不該被重複建立");
    }

    [Fact]
    public async Task UpsertFromAzureAdAsync_PreviouslyDeactivatedUser_ShouldReactivate()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        // Arrange
        var u = await sut.UpsertFromAzureAdAsync(MakePrincipal());
        await sut.DeactivateByAzureObjectIdAsync(u.AzureAdObjectId);

        // Act: 同使用者重新登入→ upsert
        var refreshed = await sut.UpsertFromAzureAdAsync(MakePrincipal());

        refreshed.IsActive.Should().BeTrue("Azure AD 是 single source of truth，重新出現 = 重新啟用");
    }

    [Fact]
    public async Task UpsertFromAzureAdAsync_MissingOid_ShouldThrow()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        var act = () => sut.UpsertFromAzureAdAsync(MakePrincipal(oid: ""));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{AuthErrorCodes.MissingObjectId}*");
    }

    [Fact]
    public async Task DeactivateByAzureObjectIdAsync_UserNotFound_ShouldReturnFalse()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        (await sut.DeactivateByAzureObjectIdAsync("unknown-oid")).Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateByAzureObjectIdAsync_AlreadyDeactivated_ShouldBeIdempotent()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        var u = await sut.UpsertFromAzureAdAsync(MakePrincipal());
        (await sut.DeactivateByAzureObjectIdAsync(u.AzureAdObjectId)).Should().BeTrue();
        (await sut.DeactivateByAzureObjectIdAsync(u.AzureAdObjectId)).Should().BeTrue();

        (await ctx.Users.SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateByAzureObjectIdAsync_ActiveUser_ShouldDeactivate()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        var u = await sut.UpsertFromAzureAdAsync(MakePrincipal());
        u.IsActive.Should().BeTrue();

        var ok = await sut.DeactivateByAzureObjectIdAsync(u.AzureAdObjectId);
        ok.Should().BeTrue();

        (await ctx.Users.SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateByAzureObjectIdAsync_DeactivatedUser_ShouldReactivate()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        var u = await sut.UpsertFromAzureAdAsync(MakePrincipal());
        await sut.DeactivateByAzureObjectIdAsync(u.AzureAdObjectId);

        (await sut.ReactivateByAzureObjectIdAsync(u.AzureAdObjectId)).Should().BeTrue();
        (await ctx.Users.SingleAsync()).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateByAzureObjectIdAsync_UserNotFound_ShouldReturnFalse()
    {
        await using var ctx = NewContext();
        var sut = new UserSyncService(ctx, NullLogger<UserSyncService>.Instance);

        (await sut.ReactivateByAzureObjectIdAsync("unknown-oid")).Should().BeFalse();
    }
}
