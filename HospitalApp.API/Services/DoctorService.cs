using HospitalApp.API.Data;
using HospitalApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface IDoctorService
{
    Task<List<DoctorListDto>> GetDoctorsAsync(Guid? specialtyId, string? search = null, string? city = null);
    Task<DoctorDetailDto?> GetDoctorByIdAsync(Guid id);
    Task<List<DoctorSchedule>> GetDoctorScheduleAsync(Guid doctorId);
    Task UpdateScheduleAsync(Guid doctorId, List<ScheduleUpdateDto> schedules);
    Task UpdateDoctorProfileAsync(Guid doctorId, UpdateDoctorProfileDto dto);
    Task<List<DoctorVacationDto>> GetVacationsAsync(Guid doctorId);
    Task SaveVacationsAsync(Guid doctorId, List<DoctorVacationDto> vacations);
}

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _db;
    public DoctorService(AppDbContext db) { _db = db; }

    public async Task<List<DoctorListDto>> GetDoctorsAsync(Guid? specialtyId, string? search = null, string? city = null)
    {
        var q = _db.Doctors.Include(d => d.Specialty).Include(d => d.User).AsQueryable();
        if (specialtyId.HasValue)
            q = q.Where(d => d.SpecialtyId == specialtyId);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(d => (d.FirstName + " " + d.LastName).ToLower().Contains(search.ToLower())
                           || d.FirstName.ToLower().Contains(search.ToLower())
                           || d.LastName.ToLower().Contains(search.ToLower()));
        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(d => d.City != null && d.City.ToLower().Contains(city.ToLower()));
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
            HospitalSection = d.HospitalSection,
            HospitalName = d.HospitalName,
            Address = d.Address,
            City = d.City,
            Latitude = d.Latitude,
            Longitude = d.Longitude
        }).ToListAsync();
    }

    public async Task<DoctorDetailDto?> GetDoctorByIdAsync(Guid id)
    {
        var d = await _db.Doctors
            .Include(d => d.Specialty).Include(d => d.User).Include(d => d.Schedules)
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
            HospitalName = d.HospitalName,
            Address = d.Address,
            City = d.City,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            Languages = d.Languages,
            Diplomas = d.Diplomas,
            LicenseNumber = d.LicenseNumber,
            SlotDurationMinutes = d.SlotDurationMinutes,
            CabinetImages = d.CabinetImages,
            IsProfileComplete = !string.IsNullOrEmpty(d.PhoneNumber) && d.Schedules.Any()
        };
    }

    public async Task UpdateDoctorProfileAsync(Guid doctorId, UpdateDoctorProfileDto dto)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
        if (doctor == null) return;
        if (dto.PhoneNumber != null) doctor.PhoneNumber = dto.PhoneNumber;
        if (dto.Biography != null) doctor.Biography = dto.Biography;
        if (dto.Address != null) doctor.Address = dto.Address;
        if (dto.City != null) doctor.City = dto.City;
        if (dto.HospitalName != null) doctor.HospitalName = dto.HospitalName;
        if (dto.HospitalSection != null) doctor.HospitalSection = dto.HospitalSection;
        if (dto.ConsultationFee.HasValue) doctor.ConsultationFee = dto.ConsultationFee.Value;
        if (dto.YearsOfExperience.HasValue) doctor.YearsOfExperience = dto.YearsOfExperience.Value;
        if (dto.Languages != null) doctor.Languages = dto.Languages;
        if (dto.Diplomas != null) doctor.Diplomas = dto.Diplomas;
        if (dto.SlotDurationMinutes.HasValue) doctor.SlotDurationMinutes = dto.SlotDurationMinutes.Value;
        if (dto.FirstNameAr != null) doctor.FirstNameAr = dto.FirstNameAr;
        if (dto.LastNameAr != null) doctor.LastNameAr = dto.LastNameAr;
        if (dto.AddressAr != null) doctor.AddressAr = dto.AddressAr;
        if (dto.Latitude.HasValue) doctor.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue) doctor.Longitude = dto.Longitude;
        await _db.SaveChangesAsync();
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
                IsAvailable = s.IsAvailable,
                ConsultationType = s.ConsultationType
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<DoctorVacationDto>> GetVacationsAsync(Guid doctorId)
        => await _db.DoctorVacations
            .Where(v => v.DoctorId == doctorId)
            .Select(v => new DoctorVacationDto
            {
                Id = v.Id,
                Label = v.Label,
                StartDate = v.StartDate,
                EndDate = v.EndDate
            }).ToListAsync();

    public async Task SaveVacationsAsync(Guid doctorId, List<DoctorVacationDto> vacations)
    {
        var existing = await _db.DoctorVacations.Where(v => v.DoctorId == doctorId).ToListAsync();
        _db.DoctorVacations.RemoveRange(existing);
        foreach (var v in vacations)
        {
            _db.DoctorVacations.Add(new DoctorVacation
            {
                DoctorId = doctorId,
                Label = v.Label,
                StartDate = v.StartDate,
                EndDate = v.EndDate
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
    public string? HospitalName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class DoctorDetailDto : DoctorListDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Biography { get; set; }
    public string? PhoneNumber { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string? Languages { get; set; }
    public string? Diplomas { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public string? CabinetImages { get; set; }
    public bool IsProfileComplete { get; set; }
}

public class ScheduleUpdateDto
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string ConsultationType { get; set; } = "Présentiel";
}

public class DoctorVacationDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateDoctorProfileDto
{
    public string? PhoneNumber { get; set; }
    public string? Biography { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? HospitalName { get; set; }
    public string? HospitalSection { get; set; }
    public decimal? ConsultationFee { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Languages { get; set; }
    public string? Diplomas { get; set; }
    public int? SlotDurationMinutes { get; set; }
    public string? FirstNameAr { get; set; }
    public string? LastNameAr { get; set; }
    public string? AddressAr { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
