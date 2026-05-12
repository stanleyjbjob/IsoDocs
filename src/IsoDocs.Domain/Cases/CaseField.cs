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
    /// <summary>
    /// 繼承來源案件 ID（issue [5.3.2]）。非 null 表示此欄位值係從主單繼承，
    /// 前端可據此顯示繼承來源標示。子流程修改後仍保留此欄位以供追溯。
    /// </summary>
    public Guid? InheritedFromCaseId { get; protected set; }

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

    /// <summary>
    /// 建立一個從主單繼承欄位值的 CaseField（issue [5.3.2]）。
    /// </summary>
    public static CaseField CreateInherited(
        Guid id, Guid caseId, Guid fieldDefinitionId, int fieldVersion,
        string fieldCode, string valueJson, Guid inheritedFromCaseId)
    {
        var field = new CaseField(id, caseId, fieldDefinitionId, fieldVersion, fieldCode, valueJson);
        field.InheritedFromCaseId = inheritedFromCaseId;
        return field;
    }

    public void UpdateValue(string valueJson)
    {
        ValueJson = valueJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
