using System.Text.Json;
using System.Text.Json.Serialization;
using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Workflows;

/// <summary>
/// 流程範本下的節點定義。一個範本可包含多個節點，依 NodeOrder 線性流轉，
/// 並可由 RequiredRoleId 限制可承辦的角色。
/// </summary>
public class WorkflowNode : Entity<Guid>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Guid WorkflowTemplateId { get; protected set; }
    public int TemplateVersion { get; protected set; }
    public int NodeOrder { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public WorkflowNodeType NodeType { get; protected set; }
    public Guid? RequiredRoleId { get; protected set; }

    /// <summary>
    /// 節點額外設定（如可繼承欄位、是否需簽核等），以 JSON 儲存。
    /// </summary>
    public string ConfigJson { get; protected set; } = "{}";

    private WorkflowNode() { }

    public WorkflowNode(Guid id, Guid workflowTemplateId, int templateVersion, int nodeOrder, string name, WorkflowNodeType nodeType, Guid? requiredRoleId)
    {
        Id = id;
        WorkflowTemplateId = workflowTemplateId;
        TemplateVersion = templateVersion;
        NodeOrder = nodeOrder;
        Name = name;
        NodeType = nodeType;
        RequiredRoleId = requiredRoleId;
    }

    /// <summary>
    /// 取得此節點設定為可繼承的欄位定義 ID 清單（issue [5.3.2]）。
    /// 子流程建立時將自動帶入主單中這些欄位的值。
    /// </summary>
    public IReadOnlyList<Guid> GetInheritableFieldIds()
    {
        var config = JsonSerializer.Deserialize<NodeConfigData>(ConfigJson, _jsonOptions);
        return config?.InheritableFieldIds?.AsReadOnly()
               ?? (IReadOnlyList<Guid>)Array.Empty<Guid>();
    }

    /// <summary>
    /// 設定此節點可繼承的欄位定義 ID 清單，並保留 ConfigJson 中既有的其他設定。
    /// </summary>
    public void SetInheritableFields(IReadOnlyList<Guid> fieldDefinitionIds)
    {
        var config = JsonSerializer.Deserialize<NodeConfigData>(ConfigJson, _jsonOptions) ?? new NodeConfigData();
        config = config with { InheritableFieldIds = fieldDefinitionIds.ToList() };
        ConfigJson = JsonSerializer.Serialize(config, _jsonOptions);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private sealed record NodeConfigData
    {
        [JsonPropertyName("inheritableFieldIds")]
        public List<Guid>? InheritableFieldIds { get; init; }
    }
}

public enum WorkflowNodeType
{
    /// <summary>申請/發起。</summary>
    Apply = 1,
    /// <summary>處理/承辦。</summary>
    Process = 2,
    /// <summary>核准/簽核。</summary>
    Approve = 3,
    /// <summary>結案。</summary>
    Close = 4,
    /// <summary>通知（單純通知不阻塞流程）。</summary>
    Notify = 5
}
