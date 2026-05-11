namespace IsoDocs.Domain.Common;

/// <summary>
/// 提供給基礎設施層攜截器使用的時間戳設定入口。
/// Entity&lt;T&gt; 實作此介面，使 SaveChanges 攜截器可更新 UpdatedAt。
/// </summary>
public interface IHasTimestamps
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
    void SetUpdatedAt(DateTimeOffset updatedAt);
}
