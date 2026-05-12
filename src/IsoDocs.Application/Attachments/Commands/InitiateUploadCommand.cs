using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Attachments;

namespace IsoDocs.Application.Attachments.Commands;

/// <summary>
/// 發起附件上傳：建立 Attachment 記錄並回傳 Azure Blob SAS 上傳 URL。
/// 對應 POST /api/cases/{caseId}/attachments。
/// </summary>
public sealed record InitiateUploadCommand(
    Guid CaseId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId) : ICommand<UploadInitiationDto>;

public sealed class InitiateUploadCommandValidator : AbstractValidator<InitiateUploadCommand>
{
    public InitiateUploadCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(100L * 1024 * 1024);
        RuleFor(x => x.UploadedByUserId).NotEmpty();
    }
}

public sealed class InitiateUploadCommandHandler : ICommandHandler<InitiateUploadCommand, UploadInitiationDto>
{
    private readonly IAttachmentRepository _attachments;
    private readonly IBlobStorageService _blob;

    public InitiateUploadCommandHandler(IAttachmentRepository attachments, IBlobStorageService blob)
    {
        _attachments = attachments;
        _blob = blob;
    }

    public async Task<UploadInitiationDto> Handle(InitiateUploadCommand request, CancellationToken cancellationToken)
    {
        var attachmentId = Guid.NewGuid();
        var blobPath = $"cases/{request.CaseId}/{attachmentId}/{request.FileName}";
        var blobUrl = _blob.BuildBlobUrl(blobPath);

        var attachment = new Attachment(
            attachmentId,
            request.CaseId,
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            blobUrl,
            request.UploadedByUserId);

        await _attachments.AddAsync(attachment, cancellationToken);
        await _attachments.SaveChangesAsync(cancellationToken);

        var token = await _blob.GenerateUploadTokenAsync(blobPath, request.ContentType, cancellationToken);
        return new UploadInitiationDto(attachmentId, token.UploadUrl, token.ExpiresAt);
    }
}
