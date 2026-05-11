using System.Text.Json;
using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.Workflows.Commands;

/// <summary>
/// 更新流程範本。若範本已發行則自動 BumpVersion，保持版本隔離。
/// 對應 PUT /api/workflow-templates/{id}。
/// </summary>
public sealed record UpdateWorkflowTemplateCommand(
    Guid TemplateId,
    string Name,
    string? Description,
    IReadOnlyList<NodeInput> Nodes) : ICommand<WorkflowTemplateDto>;

public sealed class UpdateWorkflowTemplateCommandValidator : AbstractValidator<UpdateWorkflowTemplateCommand>
{
    public UpdateWorkflowTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.Nodes).NotNull();
        RuleForEach(x => x.Nodes).ChildRules(node =>
        {
            node.RuleFor(n => n.Name).NotEmpty().MaximumLength(128);
            node.RuleFor(n => n.NodeOrder).GreaterThan(0);
        });
    }
}

public sealed class UpdateWorkflowTemplateCommandHandler : ICommandHandler<UpdateWorkflowTemplateCommand, WorkflowTemplateDto>
{
    private readonly IWorkflowTemplateRepository _templates;

    public UpdateWorkflowTemplateCommandHandler(IWorkflowTemplateRepository templates)
    {
        _templates = templates;
    }

    public async Task<WorkflowTemplateDto> Handle(UpdateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _templates.FindByIdAsync(request.TemplateId, cancellationToken)
            ?? throw new DomainException(WorkflowErrorCodes.TemplateNotFound, $"找不到流程範本 {request.TemplateId}。");

        if (template.PublishedAt is not null)
        {
            // 已發行版本 → BumpVersion 建立新草稿版本，舊版節點保留供歷史查閱
            template.BumpVersion();
        }
        else
        {
            // 草稿狀態 → 清除同版節點後重建
            await _templates.RemoveNodesAsync(template.Id, template.Version, cancellationToken);
        }

        template.Update(request.Name, request.Description);

        var nodes = request.Nodes
            .Select(n => new WorkflowNode(
                Guid.NewGuid(), template.Id, template.Version,
                n.NodeOrder, n.Name, n.NodeType, n.RequiredRoleId))
            .ToList();

        var definitionJson = JsonSerializer.Serialize(
            nodes.Select(n => new { n.NodeOrder, n.Name, NodeType = n.NodeType.ToString(), n.RequiredRoleId }));
        template.UpdateDefinition(definitionJson);

        await _templates.AddNodesAsync(nodes, cancellationToken);
        await _templates.SaveChangesAsync(cancellationToken);

        return WorkflowTemplateDtoMapper.ToDto(template, nodes);
    }
}
