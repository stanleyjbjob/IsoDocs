using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Attachments;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Attachments.Queries;

/// <summary>
/// 取得附件下載 URL（SAS 短效連結，30 分鐘）。
/// 對應 GET /api/cases/{caseId}/attachments/{attachmentId}。
/// </summary>
public sealed record GetAttachmentDownloadUrlQuery(Guid CaseId, Guid AttachmentId) : IQuery<AttachmentDownloadDto>;

public sealed class GetAttachmentDownloadUrlQueryHandler
    : IQueryHandler<GetAttachmentDownloadUrlQuery, AttachmentDownloadDto>
{
    private readonly IAttachmentRepository _attachments;
    private readonly IBlobStorageService _blob;

    public GetAttachmentDownloadUrlQueryHandler(IAttachmentRepository attachments, IBlobStorageService blob)
    {
        _attachments = attachments;
        _blob = blob;
    }

    public async Task<AttachmentDownloadDto> Handle(GetAttachmentDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _attachments.FindByIdAsync(request.AttachmentId, cancellationToken);

        if (attachment is null || attachment.CaseId != request.CaseId || attachment.IsDeleted)
            throw new DomainException(AttachmentErrorCodes.NotFound, $"附件 {request.AttachmentId} 不存在或已刪除。");

        var blobPath = $"cases/{attachment.CaseId}/{attachment.Id}/{attachment.FileName}";
        var token = await _blob.GenerateDownloadTokenAsync(blobPath, cancellationToken);

        return new AttachmentDownloadDto(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            token.DownloadUrl,
            token.ExpiresAt,
            attachment.UploadedAt);
    }
}
