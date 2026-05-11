using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 發起工作需求單。對應 POST /api/cases 端點。
/// 自動取號、建立初始 CaseNode、發布 CaseInitiatedNotification。
/// </summary>
public sealed record CreateCaseCommand(
    string Title,
    Guid DocumentTypeId,
    Guid WorkflowTemplateId,
    DateTimeOffset? ExpectedCompletionAt,
    Guid? CustomerId,
    Guid InitiatedByUserId) : ICommand<CaseDto>;

public sealed class CreateCaseCommandValidator : AbstractValidator<CreateCaseCommand>
{
    public CreateCaseCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("標題必填").MaximumLength(200);
        RuleFor(x => x.DocumentTypeId).NotEmpty().WithMessage("文件類型必填");
        RuleFor(x => x.WorkflowTemplateId).NotEmpty().WithMessage("流程範本必填");
        RuleFor(x => x.InitiatedByUserId).NotEmpty().WithMessage("發起人必填");
    }
}

public sealed class CreateCaseCommandHandler : ICommandHandler<CreateCaseCommand, CaseDto>
{
    private readonly ICaseRepository _cases;
    private readonly IDocumentTypeRepository _documentTypes;
    private readonly IWorkflowRepository _workflows;
    private readonly IPublisher _publisher;

    public CreateCaseCommandHandler(
        ICaseRepository cases,
        IDocumentTypeRepository documentTypes,
        IWorkflowRepository workflows,
        IPublisher publisher)
    {
        _cases = cases;
        _documentTypes = documentTypes;
        _workflows = workflows;
        _publisher = publisher;
    }

    public async Task<CaseDto> Handle(CreateCaseCommand request, CancellationToken cancellationToken)
    {
        var documentType = await _documentTypes.FindByIdAsync(request.DocumentTypeId, cancellationToken);
        if (documentType is null || !documentType.IsActive)
            throw new DomainException("case.document_type_not_found", "文件類型不存在或已停用");

        var template = await _workflows.FindByIdAsync(request.WorkflowTemplateId, cancellationToken);
        if (template is null || !template.IsActive)
            throw new DomainException("case.workflow_not_found", "流程範本不存在或已停用");

        var nodes = await _workflows.ListNodesByTemplateAsync(
            request.WorkflowTemplateId, template.Version, cancellationToken);
        if (nodes.Count == 0)
            throw new DomainException("case.no_workflow_nodes", "流程範本未定義任何節點，無法發起案件");

        var caseNumber = documentType.AcquireNext(DateTimeOffset.UtcNow.Year);

        var @case = new Case(
            id: Guid.NewGuid(),
            caseNumber: caseNumber,
            title: request.Title,
            documentTypeId: request.DocumentTypeId,
            workflowTemplateId: request.WorkflowTemplateId,
            templateVersion: template.Version,
            fieldVersion: 1,
            initiatedByUserId: request.InitiatedByUserId,
            expectedCompletionAt: request.ExpectedCompletionAt,
            customerId: request.CustomerId);

        await _cases.AddAsync(@case, cancellationToken);

        var firstNode = nodes.OrderBy(n => n.NodeOrder).First();
        var caseNode = new CaseNode(
            id: Guid.NewGuid(),
            caseId: @case.Id,
            workflowNodeId: firstNode.Id,
            nodeOrder: firstNode.NodeOrder,
            nodeName: firstNode.Name,
            assigneeUserId: null,
            originalExpectedAt: request.ExpectedCompletionAt);

        await _cases.AddNodeAsync(caseNode, cancellationToken);
        await _cases.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new CaseInitiatedNotification(@case.Id, @case.CaseNumber, caseNode.Id),
            cancellationToken);

        return CaseDtoMapper.ToDto(@case);
    }
}

/// <summary>
/// 案件發起通知。由 issue #24 [6.2] 實作的通知訂閱者處理。
/// </summary>
public sealed record CaseInitiatedNotification(
    Guid CaseId,
    string CaseNumber,
    Guid InitialNodeId) : INotification;
