using IsoDocs.Application.Auth;
using IsoDocs.Domain.Identity;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IsoDocs.Infrastructure.Auth;

/// <summary>
/// <see cref="IUserSyncService"/> 的 EF Core 實作。與 <see cref="IsoDocsDbContext"/> 同生命週期（Scoped）。
/// </summary>
internal sealed class UserSyncService : IUserSyncService
{
    private readonly IsoDocsDbContext _dbContext;
    private readonly ILogger<UserSyncService> _logger;

    public UserSyncService(IsoDocsDbContext dbContext, ILogger<UserSyncService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<User> UpsertFromAzureAdAsync(
        AzureAdUserPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (!principal.IsAuthenticated)
        {
            // 上層讓 ASP.NET Core authn pipeline 擋掊未認證；這裡則以參數驗證代表程式錯誤。
            throw new ArgumentException(
                $"AzureAdUserPrincipal.AzureAdObjectId 無法為空 ({AuthErrorCodes.MissingObjectId})",
                nameof(principal));
        }

        var existing = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == principal.AzureAdObjectId, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateProfile(
                email: principal.Email,
                displayName: principal.DisplayName,
                department: principal.Department,
                jobTitle: principal.JobTitle);

            // 之前被停用 (例如離職後重新邀請) 時重新啟用。這裡讓 Azure AD 是 single source of truth。
            if (!existing.IsActive)
            {
                existing.Activate();
                _logger.LogInformation(
                    "使用者 {AzureAdObjectId} 原為停用狀態，本次登入重新啟用。",
                    principal.AzureAdObjectId);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var newUser = new User(
            id: Guid.NewGuid(),
            azureAdObjectId: principal.AzureAdObjectId,
            email: principal.Email,
            displayName: principal.DisplayName);

        // 建構子只吃 4 個必要欄位，剩下 department/jobTitle 在 UpdateProfile 裡一次設入（仍為同一個 SaveChanges 象內）。
        newUser.UpdateProfile(
            email: principal.Email,
            displayName: principal.DisplayName,
            department: principal.Department,
            jobTitle: principal.JobTitle);

        await _dbContext.Users.AddAsync(newUser, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "新增使用者 {AzureAdObjectId} ({Email})。",
            principal.AzureAdObjectId, principal.Email);

        return newUser;
    }

    public async Task<bool> DeactivateByAzureObjectIdAsync(
        string azureAdObjectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(azureAdObjectId))
        {
            return false;
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (!user.IsActive)
        {
            return true; // 幂等：已為停用。
        }

        user.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "使用者 {AzureAdObjectId} 被停用（例如離職同步）。",
            azureAdObjectId);

        return true;
    }

    public async Task<bool> ReactivateByAzureObjectIdAsync(
        string azureAdObjectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(azureAdObjectId))
        {
            return false;
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.IsActive)
        {
            return true; // 幂等。
        }

        user.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "使用者 {AzureAdObjectId} 被重新啟用。",
            azureAdObjectId);

        return true;
    }
}
