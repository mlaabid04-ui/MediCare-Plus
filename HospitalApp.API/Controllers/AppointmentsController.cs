using HospitalApp.API.Data;
using HospitalApp.API.Models;
using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;
    private readonly AppDbContext _db;
    public AppointmentsController(IAppointmentService svc, AppDbContext db) { _service = svc; _db = db; }

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

    [HttpPut("{id}/prescription")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> WritePrescription(Guid id, [FromBody] WritePrescriptionDto dto)
    {
        var result = await _service.WritePrescriptionAsync(id, dto.Prescription);
        return result ? Ok() : NotFound();
    }

    [HttpPost("{id}/review")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> SubmitReview(Guid id, [FromBody] CreateReviewDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return BadRequest(new { message = "Patient introuvable." });

        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);
        if (appointment == null) return NotFound(new { message = "Rendez-vous introuvable." });
        if (appointment.Status != "Completed")
            return BadRequest(new { message = "Vous pouvez évaluer uniquement après un rendez-vous terminé." });

        var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.PatientId == patient.Id && r.DoctorId == appointment.DoctorId);
        if (alreadyReviewed)
            return BadRequest(new { message = "Vous avez déjà évalué ce médecin." });

        var review = new Review
        {
            AppointmentId = id,
            PatientId = patient.Id,
            DoctorId = appointment.DoctorId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Comment = dto.Comment
        };
        _db.Reviews.Add(review);

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == appointment.DoctorId);
        if (doctor != null)
        {
            var existingRatings = await _db.Reviews
                .Where(r => r.DoctorId == appointment.DoctorId)
                .Select(r => r.Rating).ToListAsync();
            doctor.TotalReviews = existingRatings.Count + 1;
            doctor.Rating = Math.Round((decimal)(existingRatings.Sum() + review.Rating) / doctor.TotalReviews, 1);
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class UpdateStatusDto { public string Status { get; set; } = ""; }
