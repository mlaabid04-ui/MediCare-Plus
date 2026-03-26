using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _service;
    public ChatController(IChatService svc) { _service = svc; }

    [HttpGet("{otherUserId}")]
    public async Task<IActionResult> GetMessages(Guid otherUserId)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return Ok(await _service.GetMessagesAsync(userId, otherUserId));
    }

    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return Ok(await _service.GetContactsAsync(userId));
    }
}
