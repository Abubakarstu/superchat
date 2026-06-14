using Application.Commands.Campaigns;
using Application.DTOs;
using Application.Queries.Campaigns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/campaigns")]
public class CampaignsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CampaignsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetCampaignsQuery()));
    }

    [HttpPost]
    public async Task<ActionResult<CampaignDto>> Create([FromBody] CreateCampaignCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult> Send(Guid id)
    {
        var result = await _mediator.Send(new SendCampaignCommand { CampaignId = id });
        return result ? Ok() : NotFound();
    }
}
