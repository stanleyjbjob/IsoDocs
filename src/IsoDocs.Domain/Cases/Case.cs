using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件主檔（如工作需求單、文件變更申請等）。
/// 以 TemplateVersion / FieldVersion 凍結建立當下的範本與欄位快照，避免日後設定異動影響已使用者。
/// </summary>
public class Case : Entity<Guid>, IAggregateRoot
{
    public string CaseNumber { get; protected set; } = string.Empty;
    public string Title { get; protected set; } = string.Empty;
    public Guid DocumentTypeId { get; protected set; }
    public Guid WorkflowTemplateId { get; protected set; }
    public int TemplateVersion { get; protected set; }
    public int FieldVersion { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public CaseStatus Status { get; protected set; } = CaseStatus.InProgress;
    public Guid InitiatedByUserId { get; protected set; }
    public DateTimeOffset InitiatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpectedCompletionAt { get; protected set; }
    /// <summary>發起時設定的原始預計完成時間，不隨節點修改變動（issue [5.4.1]）。</summary>
    public DateTimeOffset? OriginalExpectedAt { get; protected set; }
    public DateTimeOffset? ClosedAt { get; protected set; }
    public DateTimeOffset? VoidedAt { get; protected set; }
    /// <summary>使用者自訂版號（issue [5.4.2]）。</summary>
    public string? CustomVersionNumber { get; protected set; }

    private Case() { }

    public Case(Guid id, string caseNumber, string title, Guid documentTypeId,
        Guid workflowTemplateId, int templateVersion, int fieldVersion,
        Guid initiatedByUserId, DateTimeOffset? expectedCompletionAt, Guid? customerId)
    {
        Id = id;
        CaseNumber = caseNumber;
        Title = title;
        DocumentTypeId = documentTypeId;
        WorkflowTemplateId = workflowTemplateId;
        TemplateVersion = templateVersion;
        FieldVersion = fieldVersion;
        InitiatedByUserId = initiatedByUserId;
        ExpectedCompletionAt = expectedCompletionAt;
        OriginalExpectedAt = expectedCompletionAt;
        CustomerId = customerId;
    }

    public void Close()
    {
        if (Status != CaseStatus.InProgress)
            throw new DomainException("case.invalid_status", "只有進行中的案件可結案");
        Status = CaseStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = ClosedAt;
    }

    public void Void()
    {
        if (Status == CaseStatus.Voided)
            throw new DomainException("case.already_voided", "案件已作廢");
        Status = CaseStatus.Voided;
        VoidedAt = DateTimeOffset.UtcNow;
        UpdatedAt = VoidedAt;
    }

    public void UpdateExpectedCompletion(DateTimeOffset expectedAt)
    {
        ExpectedCompletionAt = expectedAt;
        OriginalExpectedAt ??= expectedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCustomVersionNumber(string version)
    {
        CustomVersionNumber = version;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
