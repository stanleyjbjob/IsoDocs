using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.FieldDefinitions.Commands;

/// <summary>
/// 更新欄位定義，自動將 Version + 1。進行中案件的 CaseField.FieldVersion 不受影響（版本隔離）。
/// </summary>
public sealed record UpdateFieldDefinitionCommand(
    Guid Id,
    string Name,
    FieldType Type,
    bool IsRequired,
    string? ValidationJson,
    string? OptionsJson) : ICommand<FieldDefinitionDto>;

public sealed class UpdateFieldDefinitionCommandValidator
    : AbstractValidator<UpdateFieldDefinitionCommand>
{
    public UpdateFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("欄位名稱必填")
            .MaximumLength(128).WithMessage("欄位名稱不可超過 128 字");
    }
}

public sealed class UpdateFieldDefinitionCommandHandler
    : ICommandHandler<UpdateFieldDefinitionCommand, FieldDefinitionDto>
{
    private readonly IFieldDefinitionRepository _repo;

    public UpdateFieldDefinitionCommandHandler(IFieldDefinitionRepository repo) => _repo = repo;

    public async Task<FieldDefinitionDto> Handle(
        UpdateFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var fd = await _repo.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException(FieldDefinitionErrorCodes.NotFound,
                $"欄位定義 '{request.Id}' 不存在。");

        fd.Update(request.Name, request.Type, request.IsRequired,
            request.ValidationJson, request.OptionsJson);

        await _repo.SaveChangesAsync(cancellationToken);

        return FieldDefinitionDtoMapper.ToDto(fd);
    }
}
