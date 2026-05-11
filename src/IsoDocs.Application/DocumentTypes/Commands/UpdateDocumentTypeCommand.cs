using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.DocumentTypes.Commands;

public sealed record UpdateDocumentTypeCommand(
    Guid Id,
    string Name,
    bool IsActive) : ICommand<DocumentTypeDto>;

public sealed class UpdateDocumentTypeCommandValidator : AbstractValidator<UpdateDocumentTypeCommand>
{
    public UpdateDocumentTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class UpdateDocumentTypeCommandHandler
    : ICommandHandler<UpdateDocumentTypeCommand, DocumentTypeDto>
{
    private readonly IDocumentTypeRepository _repo;

    public UpdateDocumentTypeCommandHandler(IDocumentTypeRepository repo)
    {
        _repo = repo;
    }

    public async Task<DocumentTypeDto> Handle(
        UpdateDocumentTypeCommand request, CancellationToken cancellationToken)
    {
        var dt = await _repo.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException(DocumentTypeErrorCodes.NotFound,
                $"DocumentType '{request.Id}' 不存在。");

        dt.Update(request.Name, request.IsActive);

        await _repo.SaveChangesAsync(cancellationToken);

        return DocumentTypeDtoMapper.ToDto(dt);
    }
}
