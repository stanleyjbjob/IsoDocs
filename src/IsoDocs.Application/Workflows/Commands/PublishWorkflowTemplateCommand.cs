using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Workflows.Commands;

/// <summary>
/// 發行流程範本目前版本。對應 PUT /api/workflow-templates/{id}/publish。
/// </summary>
public sealed record PublishWorkflowTemplateCommand(Guid TemplateId) : ICommand<WorkflowTemplateDto>;

public sealed class PublishWorkflowTemplateCommandHandler : ICommandHandler<PublishWorkflowTemplateCommand, WorkflowTemplateDto>
{
    private readonly IWorkflowTemplateRepository _templates;

    public PublishWorkflowTemplateCommandHandler(IWorkflowTemplateRepository templates)
    {
        _templates = templates;
    }

    public async Task<WorkflowTemplateDto> Handle(PublishWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _templates.FindByIdAsync(request.TemplateId, cancellationToken)
            ?? throw new DomainException(WorkflowErrorCodes.TemplateNotFound, $"找不到流程範本 {request.TemplateId}。");

        if (template.PublishedAt is not null)
            throw new DomainException(
                WorkflowErrorCodes.NotPublishable,
                $"流程範本 {request.TemplateId} 版本 {template.Version} 已發行，請先更新範本以建立新版本。");

        template.Publish();
        await _templates.SaveChangesAsync(cancellationToken);

        var nodes = await _templates.ListNodesAsync(template.Id, template.Version, cancellationToken);
        return WorkflowTemplateDtoMapper.ToDto(template, nodes);
    }
}
