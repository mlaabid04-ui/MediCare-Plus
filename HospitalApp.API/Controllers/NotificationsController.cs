using HospitalApp.API.Data;
using HospitalApp.API.Models;
using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly AppDbContext _db;
    public NotificationsController(INotificationService svc, AppDbContext db) { _service = svc; _db = db; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return Ok(await _service.GetUserNotificationsAsync(userId));
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _service.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _service.MarkAllAsReadAsync(userId);
        return Ok();
    }

    [HttpPut("fcm-token")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] FcmTokenDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        user.FcmToken = dto.Token;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
