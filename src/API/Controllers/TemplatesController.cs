using Application.Commands.Templates;
using Application.DTOs;
using Application.Queries.Templates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public TemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TemplateDto>>> GetAll([FromQuery] Guid? accountId)
    {
        return Ok(await _mediator.Send(new GetTemplatesQuery { AccountId = accountId }));
    }

    [HttpPost]
    public async Task<ActionResult<TemplateDto>> Create([FromBody] CreateTemplateCommand command)
    {
        return Ok(await _mediator.Send(command));
    }
}
