using Application.DTOs;
using Application.Queries.Analytics;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AnalyticsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        return Ok(await _mediator.Send(new GetDashboardQuery { From = from, To = to }));
    }
}
