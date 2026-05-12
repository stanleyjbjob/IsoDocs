using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// 取得案件完整簽核軌跡（全部 SignOff 動作，依 ActionAt 升冪排序）。
/// 對應 GET /api/cases/{caseId}/sign-off-trail。
/// </summary>
public sealed record GetSignOffTrailQuery(Guid CaseId) : IQuery<IReadOnlyList<SignOffEntryDto>>;

public sealed class GetSignOffTrailQueryHandler : IQueryHandler<GetSignOffTrailQuery, IReadOnlyList<SignOffEntryDto>>
{
    private readonly ICaseRepository _cases;

    public GetSignOffTrailQueryHandler(ICaseRepository cases)
    {
        _cases = cases;
    }

    public Task<IReadOnlyList<SignOffEntryDto>> Handle(GetSignOffTrailQuery request, CancellationToken cancellationToken)
    {
        return _cases.GetSignOffTrailAsync(request.CaseId, cancellationToken);
    }
}
