using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件欄位值快照。以 FieldVersion 凍結欄位定義。
/// </summary>
public class CaseField : Entity<Guid>
{
    public Guid CaseId { get; protected set; }
    public Guid FieldDefinitionId { get; protected set; }
    public int FieldVersion { get; protected set; }
    public string FieldCode { get; protected set; } = string.Empty;
    /// <summary>欄位值 JSON，需依 FieldType 解析。</summary>
    public string ValueJson { get; protected set; } = "null";

    private CaseField() { }

    public CaseField(Guid id, Guid caseId, Guid fieldDefinitionId, int fieldVersion, string fieldCode, string valueJson)
    {
        Id = id;
        CaseId = caseId;
        FieldDefinitionId = fieldDefinitionId;
        FieldVersion = fieldVersion;
        FieldCode = fieldCode;
        ValueJson = valueJson;
    }

    public void UpdateValue(string valueJson)
    {
        ValueJson = valueJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
