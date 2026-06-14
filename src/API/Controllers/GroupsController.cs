using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly IWhatsAppService _whatsApp;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IWhatsAppService whatsApp, ILogger<GroupsController> logger)
    {
        _whatsApp = whatsApp;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GroupCreateRequest request)
    {
        await _whatsApp.CreateGroupAsync(request);
        return Ok(new { status = "created" });
    }

    [HttpGet("{groupJid}")]
    public async Task<IActionResult> Metadata(string groupJid)
    {
        var meta = await _whatsApp.GetGroupMetadataAsync(Uri.UnescapeDataString(groupJid));
        if (meta == null) return NotFound();
        return Ok(meta);
    }

    [HttpPost("{groupJid}/participants")]
    public async Task<IActionResult> Participants(string groupJid, [FromBody] GroupParticipantsRequest request)
    {
        request.GroupJid = Uri.UnescapeDataString(groupJid);
        await _whatsApp.GroupParticipantsUpdateAsync(request);
        return Ok(new { status = "ok", action = request.Action });
    }

    [HttpPut("{groupJid}")]
    public async Task<IActionResult> Update(string groupJid, [FromBody] GroupUpdateRequest request)
    {
        request.GroupJid = Uri.UnescapeDataString(groupJid);
        await _whatsApp.GroupUpdateAsync(request);
        return Ok(new { status = "updated" });
    }

    [HttpPost("{groupJid}/leave")]
    public async Task<IActionResult> Leave(string groupJid)
    {
        await _whatsApp.GroupLeaveAsync(Uri.UnescapeDataString(groupJid));
        return Ok(new { status = "left" });
    }
}
