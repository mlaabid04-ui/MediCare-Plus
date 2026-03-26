using HospitalApp.API.Data;
using HospitalApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface IAdminService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<AdminResult> CreateDoctorAsync(CreateDoctorDto dto);
    Task<AdminResult> CreateNurseAsync(CreateNurseDto dto);
    Task<List<UserListDto>> GetUsersAsync(string? role);
    Task ToggleUserActiveAsync(Guid userId);
    Task<List<AppointmentDto>> GetAllAppointmentsAsync(DateTime? from, DateTime? to);
}

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    public AdminService(AppDbContext db) { _db = db; }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return new DashboardStatsDto
        {
            TotalDoctors = await _db.Doctors.CountAsync(),
            TotalPatients = await _db.Patients.CountAsync(),
            TotalNurses = await _db.Nurses.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            TodayAppointments = await _db.Appointments.CountAsync(a => a.AppointmentDate.Date == today),
            PendingAppointments = await _db.Appointments.CountAsync(a => a.Status == "Scheduled"),
            CompletedToday = await _db.Appointments.CountAsync(a => a.AppointmentDate.Date == today && a.Status == "Completed"),
            CancelledThisMonth = await _db.Appointments.CountAsync(a =>
                a.AppointmentDate.Month == today.Month && a.Status == "Cancelled"),
            ActiveDoctors = await _db.Doctors.Include(d => d.User).CountAsync(d => d.User!.IsActive)
        };
    }

    public async Task<AdminResult> CreateDoctorAsync(CreateDoctorDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return new AdminResult { Success = false, Message = "Email already exists" };

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Doctor"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var doctor = new Doctor
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            SpecialtyId = dto.SpecialtyId,
            LicenseNumber = dto.LicenseNumber,
            PhoneNumber = dto.PhoneNumber,
            Biography = dto.Biography,
            YearsOfExperience = dto.YearsOfExperience,
            ConsultationFee = dto.ConsultationFee,
            HospitalSection = dto.HospitalSection
        };
        _db.Doctors.Add(doctor);

        // Default schedule Mon-Fri 9:00-16:00
        for (int day = 1; day <= 5; day++)
        {
            _db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id,
                DayOfWeek = day,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(16, 0, 0),
                IsAvailable = true
            });
        }

        await _db.SaveChangesAsync();
        return new AdminResult { Success = true, Id = doctor.Id };
    }

    public async Task<AdminResult> CreateNurseAsync(CreateNurseDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return new AdminResult { Success = false, Message = "Email already exists" };

        var user = new User { Email = dto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), Role = "Nurse" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var nurse = new Nurse { UserId = user.Id, FirstName = dto.FirstName, LastName = dto.LastName, LicenseNumber = dto.LicenseNumber, PhoneNumber = dto.PhoneNumber, Department = dto.Department };
        _db.Nurses.Add(nurse);
        await _db.SaveChangesAsync();
        return new AdminResult { Success = true, Id = nurse.Id };
    }

    public async Task<List<UserListDto>> GetUsersAsync(string? role)
    {
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(role)) q = q.Where(u => u.Role == role);
        return await q.Select(u => new UserListDto { Id = u.Id, Email = u.Email, Role = u.Role, IsActive = u.IsActive, CreatedAt = u.CreatedAt, LastLogin = u.LastLogin }).ToListAsync();
    }

    public async Task ToggleUserActiveAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null) { user.IsActive = !user.IsActive; await _db.SaveChangesAsync(); }
    }

    public async Task<List<AppointmentDto>> GetAllAppointmentsAsync(DateTime? from, DateTime? to)
    {
        var q = _db.Appointments.Include(a => a.Patient).Include(a => a.Doctor).ThenInclude(d => d!.Specialty).AsQueryable();
        if (from.HasValue) q = q.Where(a => a.AppointmentDate >= from);
        if (to.HasValue) q = q.Where(a => a.AppointmentDate <= to);
        return await q.OrderByDescending(a => a.AppointmentDate).Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientName = $"{a.Patient!.FirstName} {a.Patient.LastName}",
            DoctorName = $"Dr. {a.Doctor!.FirstName} {a.Doctor.LastName}",
            DoctorSpecialty = a.Doctor.Specialty!.Name,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status,
            Reason = a.Reason,
            ConsultationType = a.ConsultationType
        }).ToListAsync();
    }
}

public class DashboardStatsDto
{
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalNurses { get; set; }
    public int TotalAppointments { get; set; }
    public int TodayAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int CompletedToday { get; set; }
    public int CancelledThisMonth { get; set; }
    public int ActiveDoctors { get; set; }
}

public class AdminResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? Id { get; set; }
}

public class CreateDoctorDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid SpecialtyId { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Biography { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? HospitalSection { get; set; }
}

public class CreateNurseDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string LicenseNumber { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
}

public class UserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}
