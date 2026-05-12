using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 修改案件預計完成時間。
/// 首次設定（OriginalExpectedAt 尚未寫入）時同步寫入 Case.OriginalExpectedAt；
/// 後續修改時額外更新當前節點的 CaseNode.ModifiedExpectedAt。
/// 對應 PUT /api/cases/{id}/expected-completion 端點。issue [5.4.1]。
/// </summary>
public sealed record UpdateExpectedCompletionCommand(
    Guid CaseId,
    DateTimeOffset ExpectedAt) : ICommand<CaseDto>;

public sealed class UpdateExpectedCompletionCommandValidator : AbstractValidator<UpdateExpectedCompletionCommand>
{
    public UpdateExpectedCompletionCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.ExpectedAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("預計完成時間必須晚於目前時間");
    }
}

public sealed class UpdateExpectedCompletionCommandHandler : ICommandHandler<UpdateExpectedCompletionCommand, CaseDto>
{
    private readonly ICaseRepository _cases;

    public UpdateExpectedCompletionCommandHandler(ICaseRepository cases)
    {
        _cases = cases;
    }

    public async Task<CaseDto> Handle(UpdateExpectedCompletionCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken)
            ?? throw new DomainException("case.not_found", $"找不到案件 {request.CaseId}");

        // 判斷是否為後續修改（首次設定時 OriginalExpectedAt 尚未存在）
        var isSubsequentModification = @case.OriginalExpectedAt.HasValue;

        @case.UpdateExpectedCompletion(request.ExpectedAt);

        if (isSubsequentModification)
        {
            var activeNode = await _cases.FindActiveNodeAsync(request.CaseId, cancellationToken);
            activeNode?.ModifyExpected(request.ExpectedAt);
        }

        await _cases.SaveChangesAsync(cancellationToken);

        return new CaseDto(
            @case.Id,
            @case.CaseNumber,
            @case.Title,
            @case.Status.ToString(),
            @case.ExpectedCompletionAt,
            @case.OriginalExpectedAt);
    }
}
