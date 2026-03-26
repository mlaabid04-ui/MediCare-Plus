using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _service;
    public DoctorsController(IDoctorService svc) { _service = svc; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? specialtyId)
        => Ok(await _service.GetDoctorsAsync(specialtyId));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var doctor = await _service.GetDoctorByIdAsync(id);
        return doctor == null ? NotFound() : Ok(doctor);
    }

    [HttpGet("{id}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid id)
        => Ok(await _service.GetDoctorScheduleAsync(id));

    [HttpPut("{id}/schedule")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] List<ScheduleUpdateDto> schedules)
    {
        await _service.UpdateScheduleAsync(id, schedules);
        return Ok();
    }
}
