using IsoDocs.Application.Attachments;
using IsoDocs.Application.Attachments.Commands;
using IsoDocs.Application.Attachments.Queries;
using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases/{caseId}/attachments — 附件管理。issue #26 [7.2]。
/// 附件本體儲存於 Azure Blob，端點回傳 SAS URL 供前端直接上傳/下載。
/// 附件不因案件結案或作廢而刪除（軟刪除設計）。
/// </summary>
[ApiController]
[Route("api/cases/{caseId:guid}/attachments")]
[Authorize]
public sealed class AttachmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserSyncService _userSync;

    public AttachmentsController(IMediator mediator, IUserSyncService userSync)
    {
        _mediator = mediator;
        _userSync = userSync;
    }

    /// <summary>列出案件附件（不含已刪除）。</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.AttachmentsRead)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> List(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListAttachmentsQuery(caseId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 發起附件上傳：建立附件記錄並回傳 Azure Blob SAS 上傳 URL。
    /// 前端取得 uploadUrl 後直接對 Azure Blob 進行 PUT（BlockBlob）上傳。
    /// 支援複製貼上圖片：前端將剪貼板圖片 Blob 以此 URL 直接上傳。
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.AttachmentsWrite)]
    public async Task<ActionResult<UploadInitiationDto>> InitiateUpload(
        Guid caseId,
        [FromBody] InitiateUploadRequest request,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        if (!principal.IsAuthenticated)
            return Unauthorized();

        var user = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);

        var cmd = new InitiateUploadCommand(
            CaseId: caseId,
            FileName: request.FileName,
            ContentType: request.ContentType,
            SizeBytes: request.SizeBytes,
            UploadedByUserId: user.Id);

        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    /// <summary>取得附件 SAS 下載 URL（30 分鐘效期）。</summary>
    [HttpGet("{attachmentId:guid}")]
    [Authorize(Policy = Permissions.AttachmentsRead)]
    public async Task<ActionResult<AttachmentDownloadDto>> GetDownloadUrl(
        Guid caseId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetAttachmentDownloadUrlQuery(caseId, attachmentId), cancellationToken);
        return Ok(dto);
    }

    public sealed record InitiateUploadRequest(
        string FileName,
        string ContentType,
        long SizeBytes);
}
