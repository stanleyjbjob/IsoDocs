using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.FieldDefinitions.Commands;

public sealed record CreateFieldDefinitionCommand(
    string Code,
    string Name,
    FieldType Type,
    bool IsRequired,
    string? ValidationJson,
    string? OptionsJson) : ICommand<FieldDefinitionDto>;

public sealed class CreateFieldDefinitionCommandValidator
    : AbstractValidator<CreateFieldDefinitionCommand>
{
    public CreateFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("欄位代碼必填")
            .MaximumLength(64).WithMessage("欄位代碼不可超過 64 字");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("欄位名稱必填")
            .MaximumLength(128).WithMessage("欄位名稱不可超過 128 字");
    }
}

public sealed class CreateFieldDefinitionCommandHandler
    : ICommandHandler<CreateFieldDefinitionCommand, FieldDefinitionDto>
{
    private readonly IFieldDefinitionRepository _repo;

    public CreateFieldDefinitionCommandHandler(IFieldDefinitionRepository repo) => _repo = repo;

    public async Task<FieldDefinitionDto> Handle(
        CreateFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _repo.FindByCodeAsync(request.Code, cancellationToken);
        if (duplicate is not null)
            throw new DomainException(FieldDefinitionErrorCodes.CodeDuplicate,
                $"欄位代碼 '{request.Code}' 已存在。");

        var fd = new FieldDefinition(
            Guid.NewGuid(),
            request.Code,
            request.Name,
            request.Type,
            request.IsRequired,
            request.ValidationJson,
            request.OptionsJson);

        await _repo.AddAsync(fd, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        return FieldDefinitionDtoMapper.ToDto(fd);
    }
}
