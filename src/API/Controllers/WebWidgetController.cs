using Application.DTOs;
using Application.Queries.WebWidget;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/widget")]
public class WebWidgetController : ControllerBase
{
    private readonly IMediator _mediator;
    public WebWidgetController(IMediator mediator) => _mediator = mediator;

    [HttpGet("config")]
    public async Task<ActionResult<WebWidgetDto>> GetConfig()
    {
        var config = await _mediator.Send(new GetWidgetConfigQuery());
        if (config == null) return NotFound();
        return Ok(config);
    }
}
