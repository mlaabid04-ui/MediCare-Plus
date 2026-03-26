// ============================================
// Services/AppointmentService.cs
// ============================================
using HospitalApp.API.Data;
using HospitalApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface IAppointmentService
{
    Task<List<TimeSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);
    Task<AppointmentResultDto> BookAppointmentAsync(BookAppointmentDto dto, Guid patientUserId);
    Task<List<AppointmentDto>> GetDoctorAppointmentsWeekAsync(Guid doctorId, DateTime weekStart);
    Task<List<AppointmentDto>> GetPatientAppointmentsAsync(Guid patientId);
    Task<bool> CancelAppointmentAsync(Guid appointmentId, Guid userId);
    Task<bool> UpdateAppointmentStatusAsync(Guid appointmentId, string status);
}

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifService;

    public AppointmentService(AppDbContext db, INotificationService notifService)
    {
        _db = db;
        _notifService = notifService;
    }

    public async Task<List<TimeSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek && s.IsAvailable);

        if (schedule == null)
            return new List<TimeSlotDto>();

        var bookedSlots = await _db.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == date.Date
                    && a.Status != "Cancelled")
            .Select(a => a.StartTime)
            .ToListAsync();

        var slots = new List<TimeSlotDto>();
        var current = schedule.StartTime;
        var slotDuration = TimeSpan.FromMinutes(30);

        while (current + slotDuration <= schedule.EndTime)
        {
            slots.Add(new TimeSlotDto
            {
                StartTime = current,
                EndTime = current + slotDuration,
                IsAvailable = !bookedSlots.Contains(current),
                DisplayTime = $"{current:hh\\:mm} - {(current + slotDuration):hh\\:mm}"
            });
            current += slotDuration;
        }

        return slots;
    }

    public async Task<AppointmentResultDto> BookAppointmentAsync(BookAppointmentDto dto, Guid patientUserId)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == patientUserId);
        if (patient == null)
            return new AppointmentResultDto { Success = false, Message = "Patient not found" };

        // Check slot still available
        var conflict = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == dto.DoctorId &&
            a.AppointmentDate.Date == dto.AppointmentDate.Date &&
            a.StartTime == dto.StartTime &&
            a.Status != "Cancelled");

        if (conflict)
            return new AppointmentResultDto { Success = false, Message = "This time slot is no longer available" };

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = dto.DoctorId,
            AppointmentDate = dto.AppointmentDate.Date,
            StartTime = dto.StartTime,
            EndTime = dto.StartTime + TimeSpan.FromMinutes(30),
            Reason = dto.Reason,
            ConsultationType = dto.ConsultationType,
            Status = "Scheduled"
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        // Get doctor info for notification
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == dto.DoctorId);

        // Notify patient
        await _notifService.CreateNotificationAsync(
            patientUserId,
            "Appointment Booked",
            $"Your appointment with Dr. {doctor?.FirstName} {doctor?.LastName} on {dto.AppointmentDate:MMM dd} at {dto.StartTime:hh\\:mm} has been scheduled.",
            "Appointment",
            appointment.Id
        );

        // Notify doctor
        if (doctor?.User != null)
        {
            await _notifService.CreateNotificationAsync(
                doctor.User.Id,
                "New Appointment",
                $"New appointment booked by {patient.FirstName} {patient.LastName} on {dto.AppointmentDate:MMM dd} at {dto.StartTime:hh\\:mm}.",
                "Appointment",
                appointment.Id
            );
        }

        return new AppointmentResultDto { Success = true, AppointmentId = appointment.Id };
    }

    public async Task<List<AppointmentDto>> GetDoctorAppointmentsWeekAsync(Guid doctorId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var appointments = await _db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p!.User)
            .Where(a => a.DoctorId == doctorId &&
                   a.AppointmentDate >= weekStart &&
                   a.AppointmentDate < weekEnd &&
                   a.Status != "Cancelled")
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientName = $"{a.Patient?.FirstName} {a.Patient?.LastName}",
            PatientImageUrl = a.Patient?.User?.ProfileImageUrl,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status,
            Reason = a.Reason,
            ConsultationType = a.ConsultationType
        }).ToList();
    }

    public async Task<List<AppointmentDto>> GetPatientAppointmentsAsync(Guid patientId)
    {
        var appointments = await _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            DoctorName = $"Dr. {a.Doctor?.FirstName} {a.Doctor?.LastName}",
            DoctorSpecialty = a.Doctor?.Specialty?.Name,
            DoctorImageUrl = a.Doctor?.User?.ProfileImageUrl,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status,
            Reason = a.Reason,
            ConsultationType = a.ConsultationType
        }).ToList();
    }

    public async Task<bool> CancelAppointmentAsync(Guid appointmentId, Guid userId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p!.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null) return false;

        appointment.Status = "Cancelled";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notify both parties
        if (appointment.Patient?.User != null)
            await _notifService.CreateNotificationAsync(
                appointment.Patient.User.Id,
                "Appointment Cancelled",
                $"Your appointment on {appointment.AppointmentDate:MMM dd} at {appointment.StartTime:hh\\:mm} has been cancelled.",
                "Appointment", appointmentId);

        if (appointment.Doctor?.User != null)
            await _notifService.CreateNotificationAsync(
                appointment.Doctor.User.Id,
                "Appointment Cancelled",
                $"Appointment with patient on {appointment.AppointmentDate:MMM dd} at {appointment.StartTime:hh\\:mm} has been cancelled.",
                "Appointment", appointmentId);

        return true;
    }

    public async Task<bool> UpdateAppointmentStatusAsync(Guid appointmentId, string status)
    {
        var appointment = await _db.Appointments.FindAsync(appointmentId);
        if (appointment == null) return false;
        appointment.Status = status;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}

// DTOs
public class TimeSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayTime { get; set; } = "";
}

public class BookAppointmentDto
{
    public Guid DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Reason { get; set; } = "";
    public string ConsultationType { get; set; } = "InPerson";
}

public class AppointmentResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? AppointmentId { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public string? PatientName { get; set; }
    public string? PatientImageUrl { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorSpecialty { get; set; }
    public string? DoctorImageUrl { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public string ConsultationType { get; set; } = "";
}
