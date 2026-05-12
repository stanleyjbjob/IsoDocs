namespace IsoDocs.Application.Cases;

/// <summary>
/// 單筆文件發行簽核軌跡 DTO。
/// 用於 GET /api/cases/{id}/sign-off-trail 端點回傳與前端顯示。
/// </summary>
public sealed record SignOffEntryDto(
    Guid Id,
    Guid CaseId,
    Guid? CaseNodeId,
    string? NodeName,
    Guid ActorUserId,
    string? Comment,
    DateTimeOffset ActionAt);
