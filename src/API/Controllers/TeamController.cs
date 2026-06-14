using Application.Commands.Team;
using Application.DTOs;
using Application.Queries.Team;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/team")]
public class TeamController : ControllerBase
{
    private readonly IMediator _mediator;
    public TeamController(IMediator mediator) => _mediator = mediator;

    [HttpGet("agents")]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAgents()
    {
        return Ok(await _mediator.Send(new GetAgentsQuery()));
    }

    [HttpPost("agents")]
    public async Task<ActionResult<AgentDto>> CreateAgent([FromBody] CreateAgentCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("assign")]
    public async Task<ActionResult> AssignConversation([FromBody] AssignConversationCommand command)
    {
        var result = await _mediator.Send(command);
        return result ? Ok() : NotFound();
    }
}
