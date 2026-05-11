using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Workflows;

/// <summary>
/// 自訂欄位定義（issue [3.1.1]）。欄位異動時版本累加，建立中的案件以 FieldVersion 快照凍結欄位定義，
/// 確保異動不影響既有紀錄。
/// </summary>
public class FieldDefinition : Entity<Guid>, IAggregateRoot
{
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public int Version { get; protected set; } = 1;
    public FieldType Type { get; protected set; }
    public bool IsRequired { get; protected set; }
    /// <summary>欄位驗證設定（min、max、regex 等），以 JSON 儲存。</summary>
    public string? ValidationJson { get; protected set; }
    /// <summary>下拉選單、單選、多選等的選項，以 JSON 陣列儲存。</summary>
    public string? OptionsJson { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    private FieldDefinition() { }

    public FieldDefinition(Guid id, string code, string name, FieldType type, bool isRequired,
        string? validationJson = null, string? optionsJson = null)
    {
        Id = id;
        Code = code;
        Name = name;
        Type = type;
        IsRequired = isRequired;
        ValidationJson = validationJson;
        OptionsJson = optionsJson;
    }

    /// <summary>更新欄位定義，Version 自動 +1，進行中案件的 CaseField.FieldVersion 不受影響。</summary>
    public void Update(string name, FieldType type, bool isRequired, string? validationJson, string? optionsJson)
    {
        Name = name;
        Type = type;
        IsRequired = isRequired;
        ValidationJson = validationJson;
        OptionsJson = optionsJson;
        Version += 1;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum FieldType
{
    Text = 1,
    LongText = 2,
    Number = 3,
    Decimal = 4,
    Date = 5,
    DateTime = 6,
    Boolean = 7,
    SingleSelect = 8,
    MultiSelect = 9,
    User = 10,
    Customer = 11,
    File = 12,
    Json = 13
}
