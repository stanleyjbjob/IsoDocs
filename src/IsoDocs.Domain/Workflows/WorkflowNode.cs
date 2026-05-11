using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Workflows;

/// <summary>
/// 流程範本下的節點定義。一個範本可包含多個節點，依 NodeOrder 線性流轉，
/// 並可由 RequiredRoleId 限制可承辦的角色。
/// </summary>
public class WorkflowNode : Entity<Guid>
{
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
