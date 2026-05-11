using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.DocumentTypes.Commands;

public sealed record CreateDocumentTypeCommand(
    string CompanyCode,
    string Code,
    string Name) : ICommand<DocumentTypeDto>;

public sealed class CreateDocumentTypeCommandValidator : AbstractValidator<CreateDocumentTypeCommand>
{
    public CreateDocumentTypeCommandValidator()
    {
        RuleFor(x => x.CompanyCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class CreateDocumentTypeCommandHandler
    : ICommandHandler<CreateDocumentTypeCommand, DocumentTypeDto>
{
    private readonly IDocumentTypeRepository _repo;

    public CreateDocumentTypeCommandHandler(IDocumentTypeRepository repo)
    {
        _repo = repo;
    }

    public async Task<DocumentTypeDto> Handle(
        CreateDocumentTypeCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _repo.FindByCompanyAndCodeAsync(
            request.CompanyCode, request.Code, cancellationToken);
        if (duplicate is not null)
        {
            throw new DomainException(DocumentTypeErrorCodes.CodeDuplicate,
                $"CompanyCode '{request.CompanyCode}' + Code '{request.Code}' 已存在。");
        }

        var dt = new DocumentType(
            Guid.NewGuid(),
            request.CompanyCode,
            request.Code,
            request.Name,
            DateTimeOffset.UtcNow.Year);

        await _repo.AddAsync(dt, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        return DocumentTypeDtoMapper.ToDto(dt);
    }
}
