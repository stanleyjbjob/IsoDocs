using IsoDocs.Application.Authorization;
using IsoDocs.Application.FieldDefinitions;
using IsoDocs.Application.FieldDefinitions.Commands;
using IsoDocs.Application.FieldDefinitions.Queries;
using IsoDocs.Domain.Workflows;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/field-definitions — 自訂欄位定義管理。issue #7 [3.1.1]。
/// 讀端點需 field_definitions.read，寫端點需 field_definitions.write。
/// 欄位異動時 Version 自動 +1，既有 CaseField.FieldVersion 不受影響（版本隔離）。
/// </summary>
[ApiController]
[Route("api/field-definitions")]
public sealed class FieldDefinitionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FieldDefinitionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.FieldDefinitionsRead)]
    public async Task<ActionResult<IReadOnlyList<FieldDefinitionDto>>> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListFieldDefinitionsQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.FieldDefinitionsRead)]
    public async Task<ActionResult<FieldDefinitionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFieldDefinitionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.FieldDefinitionsWrite)]
    public async Task<ActionResult<FieldDefinitionDto>> Create(
        [FromBody] CreateFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateFieldDefinitionCommand(
            Code: request.Code,
            Name: request.Name,
            Type: request.Type,
            IsRequired: request.IsRequired,
            ValidationJson: request.ValidationJson,
            OptionsJson: request.OptionsJson);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.FieldDefinitionsWrite)]
    public async Task<ActionResult<FieldDefinitionDto>> Update(
        Guid id,
        [FromBody] UpdateFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateFieldDefinitionCommand(
            Id: id,
            Name: request.Name,
            Type: request.Type,
            IsRequired: request.IsRequired,
            ValidationJson: request.ValidationJson,
            OptionsJson: request.OptionsJson);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    public sealed record CreateFieldDefinitionRequest(
        string Code,
        string Name,
        FieldType Type,
        bool IsRequired,
        string? ValidationJson,
        string? OptionsJson);

    public sealed record UpdateFieldDefinitionRequest(
        string Name,
        FieldType Type,
        bool IsRequired,
        string? ValidationJson,
        string? OptionsJson);
}
