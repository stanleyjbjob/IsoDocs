using IsoDocs.Application.Authorization;
using IsoDocs.Application.Customers;
using IsoDocs.Application.Customers.Commands;
using IsoDocs.Application.Customers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.CustomersRead)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListCustomersQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CustomersRead)]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CustomersWrite)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateCustomerCommand(
            Code: request.Code,
            Name: request.Name,
            ContactPerson: request.ContactPerson,
            ContactEmail: request.ContactEmail,
            ContactPhone: request.ContactPhone,
            Note: request.Note);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CustomersWrite)]
    public async Task<ActionResult<CustomerDto>> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateCustomerCommand(
            CustomerId: id,
            Name: request.Name,
            ContactPerson: request.ContactPerson,
            ContactEmail: request.ContactEmail,
            ContactPhone: request.ContactPhone,
            Note: request.Note);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.CustomersWrite)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetCustomerActiveCommand(id, IsActive: false), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.CustomersWrite)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetCustomerActiveCommand(id, IsActive: true), cancellationToken);
        return NoContent();
    }

    public sealed record CreateCustomerRequest(
        string Code,
        string Name,
        string? ContactPerson,
        string? ContactEmail,
        string? ContactPhone,
        string? Note);

    public sealed record UpdateCustomerRequest(
        string Name,
        string? ContactPerson,
        string? ContactEmail,
        string? ContactPhone,
        string? Note);
}
