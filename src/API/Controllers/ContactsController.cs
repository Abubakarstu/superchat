using Application.Commands.Contacts;
using Application.DTOs;
using Application.Queries.Contacts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetAll([FromQuery] string? tag, [FromQuery] string? search)
    {
        return Ok(await _mediator.Send(new GetContactsQuery { Tag = tag, Search = search }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetContactByIdQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create([FromBody] CreateContactCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContactDto>> Update(Guid id, [FromBody] UpdateContactCommand command)
    {
        command.Id = id;
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("{id:guid}/tags")]
    public async Task<ActionResult> AddTag(Guid id, [FromBody] AddContactTagCommand command)
    {
        command.ContactId = id;
        await _mediator.Send(command);
        return Ok();
    }
}
