using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Attachments.Queries;

/// <summary>
/// 列出案件所有附件（不含已刪除），依上傳時間排序。
/// 對應 GET /api/cases/{caseId}/attachments。
/// </summary>
public sealed record ListAttachmentsQuery(Guid CaseId) : IQuery<IReadOnlyList<AttachmentDto>>;

public sealed class ListAttachmentsQueryHandler
    : IQueryHandler<ListAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    private readonly IAttachmentRepository _attachments;

    public ListAttachmentsQueryHandler(IAttachmentRepository attachments)
    {
        _attachments = attachments;
    }

    public async Task<IReadOnlyList<AttachmentDto>> Handle(ListAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var attachments = await _attachments.ListByCaseIdAsync(request.CaseId, cancellationToken);
        return attachments
            .Select(a => new AttachmentDto(a.Id, a.CaseId, a.FileName, a.ContentType, a.SizeBytes, a.UploadedByUserId, a.UploadedAt))
            .ToList();
    }
}
