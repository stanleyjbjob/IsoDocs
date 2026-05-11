using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.DocumentTypes.Commands;

/// <summary>
/// 向指定文件類型取得下一個自動編碼（含年度重置與樂觀鎖重試）。
/// </summary>
public sealed record AcquireNextCodeCommand(Guid DocumentTypeId) : ICommand<string>;

public sealed class AcquireNextCodeCommandHandler : ICommandHandler<AcquireNextCodeCommand, string>
{
    private readonly IDocumentTypeRepository _repo;

    public AcquireNextCodeCommandHandler(IDocumentTypeRepository repo)
    {
        _repo = repo;
    }

    public Task<string> Handle(
        AcquireNextCodeCommand request, CancellationToken cancellationToken)
        => _repo.AcquireNextCodeAsync(request.DocumentTypeId, cancellationToken);
}
