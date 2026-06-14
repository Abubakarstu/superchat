using Application.Commands;
using Application.DTOs;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ai")]
    public async Task<ActionResult<IEnumerable<AiConfigDto>>> GetAiConfigs()
    {
        var configs = await _mediator.Send(new GetAiConfigsQuery());
        return Ok(configs);
    }

    [HttpPut("ai/{id:guid}")]
    public async Task<ActionResult<AiConfigDto>> UpdateAiConfig(Guid id, [FromBody] UpdateAiConfigCommand command)
    {
        command.Id = id;
        var config = await _mediator.Send(command);
        return Ok(config);
    }

    [HttpGet("qr")]
    public async Task<ActionResult> GetQrCode()
    {
        var qr = await _mediator.Send(new GetQrCodeQuery());
        return Ok(new { qr });
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        var connected = await _mediator.Send(new GetConnectionStatusQuery());
        return Ok(new { connected });
    }
}
