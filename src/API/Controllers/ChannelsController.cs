using Application.Commands.Channels;
using Application.DTOs;
using Application.Queries.Channels;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/channels")]
public class ChannelsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ChannelsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChannelAccountDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetChannelsQuery()));
    }

    [HttpPost]
    public async Task<ActionResult<ChannelAccountDto>> Connect([FromBody] ConnectChannelCommand command)
    {
        return Ok(await _mediator.Send(command));
    }
}
