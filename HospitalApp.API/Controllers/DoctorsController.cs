using HospitalApp.API.Data;
using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _service;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public DoctorsController(IDoctorService svc, AppDbContext db, IWebHostEnvironment env)
    {
        _service = svc; _db = db; _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? specialtyId, [FromQuery] string? search, [FromQuery] string? city)
        => Ok(await _service.GetDoctorsAsync(specialtyId, search, city));

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

    [HttpPut("{id}/profile")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateDoctorProfileDto dto)
    {
        await _service.UpdateDoctorProfileAsync(id, dto);
        return Ok();
    }

    [HttpPost("{id}/profile-image")]
    [Authorize(Roles = "Doctor,Admin")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfileImage(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file.");
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null) return NotFound();

        var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "doctors");
        Directory.CreateDirectory(folder);
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"profile_{id}{ext}";
        using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            await file.CopyToAsync(stream);

        var url = $"/uploads/doctors/{fileName}";
        doctor.User!.ProfileImageUrl = url;
        await _db.SaveChangesAsync();
        return Ok(new { url });
    }

    [HttpGet("{id}/vacations")]
    public async Task<IActionResult> GetVacations(Guid id)
        => Ok(await _service.GetVacationsAsync(id));

    [HttpPost("{id}/vacations")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> SaveVacations(Guid id, [FromBody] List<DoctorVacationDto> vacations)
    {
        await _service.SaveVacationsAsync(id, vacations);
        return Ok();
    }

    [HttpPost("{id}/cabinet-images")]
    [Authorize(Roles = "Doctor,Admin")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadCabinetImage(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file.");
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null) return NotFound();

        var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "doctors");
        Directory.CreateDirectory(folder);
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"cabinet_{id}_{Guid.NewGuid()}{ext}";
        using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            await file.CopyToAsync(stream);

        var url = $"/uploads/doctors/{fileName}";
        var list = (doctor.CabinetImages ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        list.Add(url);
        doctor.CabinetImages = string.Join(";", list);
        await _db.SaveChangesAsync();
        return Ok(new { url });
    }

    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> GetReviews(Guid id)
    {
        var reviews = await _db.Reviews
            .Include(r => r.Patient).ThenInclude(p => p!.User)
            .Where(r => r.DoctorId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                PatientName = r.Patient != null ? $"{r.Patient.FirstName} {r.Patient.LastName}" : "Anonyme",
                PatientImageUrl = r.Patient != null && r.Patient.User != null ? r.Patient.User.ProfileImageUrl : null
            })
            .ToListAsync();
        return Ok(reviews);
    }
}
