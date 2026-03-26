using HospitalApp.API.Data;
using HospitalApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface IDoctorService
{
    Task<List<DoctorListDto>> GetDoctorsAsync(Guid? specialtyId);
    Task<DoctorDetailDto?> GetDoctorByIdAsync(Guid id);
    Task<List<DoctorSchedule>> GetDoctorScheduleAsync(Guid doctorId);
    Task UpdateScheduleAsync(Guid doctorId, List<ScheduleUpdateDto> schedules);
}

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _db;
    public DoctorService(AppDbContext db) { _db = db; }

    public async Task<List<DoctorListDto>> GetDoctorsAsync(Guid? specialtyId)
    {
        var q = _db.Doctors.Include(d => d.Specialty).Include(d => d.User).AsQueryable();
        if (specialtyId.HasValue) q = q.Where(d => d.SpecialtyId == specialtyId);
        return await q.Where(d => d.User!.IsActive).Select(d => new DoctorListDto
        {
            Id = d.Id,
            FullName = $"Dr. {d.FirstName} {d.LastName}",
            SpecialtyName = d.Specialty!.Name,
            SpecialtyColor = d.Specialty.Color,
            SpecialtyIcon = d.Specialty.IconName,
            ProfileImageUrl = d.User!.ProfileImageUrl,
            Rating = d.Rating,
            TotalReviews = d.TotalReviews,
            YearsOfExperience = d.YearsOfExperience,
            ConsultationFee = d.ConsultationFee,
            HospitalSection = d.HospitalSection
        }).ToListAsync();
    }

    public async Task<DoctorDetailDto?> GetDoctorByIdAsync(Guid id)
    {
        var d = await _db.Doctors.Include(d => d.Specialty).Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (d == null) return null;
        return new DoctorDetailDto
        {
            Id = d.Id,
            UserId = d.UserId,
            FullName = $"Dr. {d.FirstName} {d.LastName}",
            FirstName = d.FirstName,
            LastName = d.LastName,
            SpecialtyName = d.Specialty?.Name ?? "",
            SpecialtyColor = d.Specialty?.Color ?? "",
            ProfileImageUrl = d.User?.ProfileImageUrl,
            Rating = d.Rating,
            TotalReviews = d.TotalReviews,
            YearsOfExperience = d.YearsOfExperience,
            ConsultationFee = d.ConsultationFee,
            Biography = d.Biography,
            PhoneNumber = d.PhoneNumber,
            HospitalSection = d.HospitalSection,
            LicenseNumber = d.LicenseNumber
        };
    }

    public async Task<List<DoctorSchedule>> GetDoctorScheduleAsync(Guid doctorId)
        => await _db.DoctorSchedules.Where(s => s.DoctorId == doctorId).ToListAsync();

    public async Task UpdateScheduleAsync(Guid doctorId, List<ScheduleUpdateDto> schedules)
    {
        var existing = await _db.DoctorSchedules.Where(s => s.DoctorId == doctorId).ToListAsync();
        _db.DoctorSchedules.RemoveRange(existing);
        foreach (var s in schedules)
        {
            _db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsAvailable = s.IsAvailable
            });
        }
        await _db.SaveChangesAsync();
    }
}

public class DoctorListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string SpecialtyName { get; set; } = "";
    public string? SpecialtyColor { get; set; }
    public string? SpecialtyIcon { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal Rating { get; set; }
    public int TotalReviews { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? HospitalSection { get; set; }
}

public class DoctorDetailDto : DoctorListDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Biography { get; set; }
    public string? PhoneNumber { get; set; }
    public string LicenseNumber { get; set; } = "";
}

public class ScheduleUpdateDto
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
}
