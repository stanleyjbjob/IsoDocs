using System.Text.Json;
using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.Workflows.Commands;

/// <summary>
/// 建立新流程範本（草稿，Version=1，未發行）。對應 POST /api/workflow-templates。
/// </summary>
public sealed record CreateWorkflowTemplateCommand(
    string Code,
    string Name,
    string? Description,
    Guid CreatedByUserId,
    IReadOnlyList<NodeInput> Nodes) : ICommand<WorkflowTemplateDto>;

public sealed class CreateWorkflowTemplateCommandValidator : AbstractValidator<CreateWorkflowTemplateCommand>
{
    public CreateWorkflowTemplateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).WithMessage("代碼必填且不超過 32 字");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128).WithMessage("名稱必填且不超過 128 字");
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.Nodes).NotNull();
        RuleForEach(x => x.Nodes).ChildRules(node =>
        {
            node.RuleFor(n => n.Name).NotEmpty().MaximumLength(128);
            node.RuleFor(n => n.NodeOrder).GreaterThan(0);
        });
    }
}

public sealed class CreateWorkflowTemplateCommandHandler : ICommandHandler<CreateWorkflowTemplateCommand, WorkflowTemplateDto>
{
    private readonly IWorkflowTemplateRepository _templates;

    public CreateWorkflowTemplateCommandHandler(IWorkflowTemplateRepository templates)
    {
        _templates = templates;
    }

    public async Task<WorkflowTemplateDto> Handle(CreateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _templates.FindByCodeAsync(request.Code, cancellationToken);
        if (duplicate is not null)
            throw new DomainException(WorkflowErrorCodes.CodeDuplicate, $"流程範本代碼 '{request.Code}' 已存在。");

        var template = new WorkflowTemplate(Guid.NewGuid(), request.Code, request.Name, request.CreatedByUserId);
        template.Update(request.Name, request.Description);

        var nodes = request.Nodes
            .Select(n => new WorkflowNode(
                Guid.NewGuid(), template.Id, template.Version,
                n.NodeOrder, n.Name, n.NodeType, n.RequiredRoleId))
            .ToList();

        var definitionJson = JsonSerializer.Serialize(
            nodes.Select(n => new { n.NodeOrder, n.Name, NodeType = n.NodeType.ToString(), n.RequiredRoleId }));
        template.UpdateDefinition(definitionJson);

        await _templates.AddAsync(template, cancellationToken);
        await _templates.AddNodesAsync(nodes, cancellationToken);
        await _templates.SaveChangesAsync(cancellationToken);

        return WorkflowTemplateDtoMapper.ToDto(template, nodes);
    }
}
