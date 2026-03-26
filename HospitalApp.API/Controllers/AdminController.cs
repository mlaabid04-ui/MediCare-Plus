using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    public AdminController(IAdminService svc) { _service = svc; }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
        => Ok(await _service.GetDashboardStatsAsync());

    [HttpPost("doctors")]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        var result = await _service.CreateDoctorAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("nurses")]
    public async Task<IActionResult> CreateNurse([FromBody] CreateNurseDto dto)
    {
        var result = await _service.CreateNurseAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role)
        => Ok(await _service.GetUsersAsync(role));

    [HttpPut("users/{id}/toggle")]
    public async Task<IActionResult> ToggleUser(Guid id)
    {
        await _service.ToggleUserActiveAsync(id);
        return Ok();
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _service.GetAllAppointmentsAsync(from, to));
}
