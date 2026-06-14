using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/status")]
public class StatusController : ControllerBase
{
    private readonly IWhatsAppService _whatsApp;

    public StatusController(IWhatsAppService whatsApp)
    {
        _whatsApp = whatsApp;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendStatusRequest request)
    {
        await _whatsApp.SendStatusAsync(request);
        return Ok(new { status = "sent" });
    }
}
