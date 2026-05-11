using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Workflows;

/// <summary>
/// 流程範本。範本異動以 Version 累加並透過 PublishedAt 標記發行時間，
/// 既有進行中案件沿用建立當時的 TemplateVersion，避免影響既有紀錄。
/// </summary>
public class WorkflowTemplate : Entity<Guid>, IAggregateRoot
{
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int Version { get; protected set; } = 1;
    public string DefinitionJson { get; protected set; } = "{}";
    public DateTimeOffset? PublishedAt { get; protected set; }
    public bool IsActive { get; protected set; } = true;
    public Guid CreatedByUserId { get; protected set; }

    private WorkflowTemplate() { }

    public WorkflowTemplate(Guid id, string code, string name, Guid createdByUserId)
    {
        Id = id;
        Code = code;
        Name = name;
        CreatedByUserId = createdByUserId;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDefinition(string definitionJson)
    {
        DefinitionJson = definitionJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        PublishedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void BumpVersion()
    {
        Version += 1;
        PublishedAt = null;
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
