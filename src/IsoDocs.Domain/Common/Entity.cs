namespace IsoDocs.Domain.Common;

/// <summary>
/// 領域實體基底類別。所有領域聚合根與實體都應繼承此類，提供識別碼比較與時間戳。
/// </summary>
public abstract class Entity<TId> : IHasTimestamps
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; protected set; }

    /// <summary>
    /// 供 Infrastructure 層攜截器設定 UpdatedAt。實體內部仍以業務語意的方法設定為主。
    /// </summary>
    void IHasTimestamps.SetUpdatedAt(DateTimeOffset updatedAt) => UpdatedAt = updatedAt;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}
