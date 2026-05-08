using IsoDocs.Application.Auth;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Api.IntegrationTests.Fakes;

/// <summary>
/// 測試替身：以記憶體字典模擬 <see cref="IUserSyncService"/>，不接 SQL Server。
///
/// 預設行為與真實 <c>UserSyncService</c> 一致：upsert 時若使用者曾被停用會自動 reactivate
/// (Azure AD 為 source of truth)；測試若需「永久停用」場景，呼叫
/// <see cref="ForcePermanentDeactivation(string)"/> 強制 upsert 後仍保持 inactive。
/// </summary>
public sealed class FakeUserSyncService : IUserSyncService
{
    private readonly Dictionary<string, User> _store = new();
    private readonly HashSet<string> _forcedInactive = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>預先放入一個 User，方便測試「先 seed 再打 endpoint」流程。</summary>
    public User Seed(string azureAdObjectId, string email, string displayName, bool isActive = true)
    {
        var user = new User(Guid.NewGuid(), azureAdObjectId, email, displayName);
        user.UpdateProfile(email, displayName, department: null, jobTitle: null);
        if (!isActive)
        {
            user.Deactivate();
        }
        _store[azureAdObjectId] = user;
        return user;
    }

    /// <summary>
    /// 強制標記某個使用者為「不可重新啟用」。下一次 upsert 後仍保持 IsActive=false，
    /// 模擬「人為停用且未走重新啟用流程」場景，用於驗 /api/me 的 403 路徑。
    /// </summary>
    public void ForcePermanentDeactivation(string azureAdObjectId)
    {
        _forcedInactive.Add(azureAdObjectId);
    }

    public Task<User> UpsertFromAzureAdAsync(
        AzureAdUserPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(principal.AzureAdObjectId, out var user))
        {
            user = new User(
                Guid.NewGuid(),
                principal.AzureAdObjectId,
                principal.Email,
                principal.DisplayName);
            _store[principal.AzureAdObjectId] = user;
        }

        user.UpdateProfile(
            principal.Email,
            principal.DisplayName,
            principal.Department,
            principal.JobTitle);

        if (_forcedInactive.Contains(principal.AzureAdObjectId))
        {
            if (user.IsActive)
            {
                user.Deactivate();
            }
        }
        else if (!user.IsActive)
        {
            // 真實行為：登入即 reactivate (Azure AD source of truth)
            user.Activate();
        }

        return Task.FromResult(user);
    }

    public Task<bool> DeactivateByAzureObjectIdAsync(
        string azureAdObjectId,
        CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(azureAdObjectId, out var user))
        {
            return Task.FromResult(false);
        }
        if (!user.IsActive)
        {
            return Task.FromResult(true);
        }
        user.Deactivate();
        return Task.FromResult(true);
    }

    public Task<bool> ReactivateByAzureObjectIdAsync(
        string azureAdObjectId,
        CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(azureAdObjectId, out var user))
        {
            return Task.FromResult(false);
        }
        if (user.IsActive)
        {
            return Task.FromResult(true);
        }
        user.Activate();
        return Task.FromResult(true);
    }
}
