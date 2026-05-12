using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly IAppointmentService _apptService;
    public PatientsController(IPatientService svc, IAppointmentService apptService)
    {
        _service = svc;
        _apptService = apptService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var patient = await _service.GetPatientByIdAsync(id);
        return patient == null ? NotFound() : Ok(patient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientDto dto)
    {
        await _service.UpdatePatientAsync(id, dto);
        return Ok();
    }

    [HttpGet("{id}/prescriptions")]
    public async Task<IActionResult> GetPrescriptions(Guid id)
        => Ok(await _apptService.GetPatientPrescriptionsAsync(id));
}
