using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;
    public AppointmentsController(IAppointmentService svc) { _service = svc; }

    [HttpGet("slots/{doctorId}")]
    public async Task<IActionResult> GetSlots(Guid doctorId, [FromQuery] DateTime date)
        => Ok(await _service.GetAvailableSlotsAsync(doctorId, date));

    [HttpPost("book")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Book([FromBody] BookAppointmentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _service.BookAppointmentAsync(dto, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("doctor/{doctorId}/week")]
    public async Task<IActionResult> GetDoctorWeek(Guid doctorId, [FromQuery] DateTime weekStart)
        => Ok(await _service.GetDoctorAppointmentsWeekAsync(doctorId, weekStart));

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientAppointments(Guid patientId)
        => Ok(await _service.GetPatientAppointmentsAsync(patientId));

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _service.CancelAppointmentAsync(id, userId);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
    {
        var result = await _service.UpdateAppointmentStatusAsync(id, dto.Status);
        return result ? Ok() : NotFound();
    }
}

public class UpdateStatusDto { public string Status { get; set; } = ""; }
