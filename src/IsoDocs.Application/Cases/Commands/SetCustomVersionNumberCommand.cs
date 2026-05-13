using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using MediatR;
using System.Text.RegularExpressions;

namespace IsoDocs.Application.Cases.Commands;

public sealed record SetCustomVersionNumberCommand(Guid CaseId, string VersionNumber) : ICommand<string>;

public sealed class SetCustomVersionNumberCommandHandler : ICommandHandler<SetCustomVersionNumberCommand, string>
{
    // 英數字、點、連字號、底線；首字必為英數；最多 20 字元
    private static readonly Regex VersionPattern =
        new(@"^[A-Za-z0-9]([A-Za-z0-9.\-_]{0,19})?$", RegexOptions.Compiled);

    private readonly ICaseRepository _cases;

    public SetCustomVersionNumberCommandHandler(ICaseRepository cases)
    {
        _cases = cases;
    }

    public async Task<string> Handle(SetCustomVersionNumberCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VersionNumber) || !VersionPattern.IsMatch(request.VersionNumber))
            throw new DomainException("case.invalid_version_number",
                "版號格式不正確，僅允許英數字、點、連字號、底線，最多 20 字元。");

        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken)
            ?? throw new DomainException("case.not_found", $"找不到案件 {request.CaseId}。");

        @case.SetCustomVersionNumber(request.VersionNumber);
        await _cases.SaveChangesAsync(cancellationToken);
        return @case.CustomVersionNumber!;
    }
}
