using Application.Commands.Workflows;
using Application.DTOs;
using Application.Queries.Workflows;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkflowsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetWorkflowsQuery()));
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDto>> Create([FromBody] CreateWorkflowCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpPatch("{id:guid}/toggle")]
    public async Task<ActionResult> Toggle(Guid id, [FromBody] ToggleWorkflowCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return result ? Ok() : NotFound();
    }
}
