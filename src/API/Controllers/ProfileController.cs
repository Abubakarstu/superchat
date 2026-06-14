using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IWhatsAppService _whatsApp;

    public ProfileController(IWhatsAppService whatsApp)
    {
        _whatsApp = whatsApp;
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        await _whatsApp.UpdateProfileAsync(request);
        return Ok(new { status = "updated" });
    }
}
