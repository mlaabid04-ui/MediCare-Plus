// ============================================
// Models/AppModels.cs  (Frontend)
// ============================================
namespace HospitalApp.Mobile.Models;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? Role { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ProfileId { get; set; }
    public string? FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class RegisterPatientRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BloodType { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? PreviousIllnesses { get; set; }
    public string? CurrentMedications { get; set; }
    public string? InsuranceNumber { get; set; }
    public string? InsuranceProvider { get; set; }
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
    // Computed display
    public string RatingDisplay => $"⭐ {Rating:F1}";
    public string FeeDisplay => $"${ConsultationFee}/session";
    public string ExperienceDisplay => $"{YearsOfExperience} yrs exp.";
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

public class TimeSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayTime { get; set; } = "";
    // Computed for UI
    public Color SlotBackground => IsAvailable ? Color.FromArgb("#F0FDF4") : Color.FromArgb("#F9FAFB");
    public Color SlotBorder => IsAvailable ? Color.FromArgb("#86EFAC") : Color.FromArgb("#E2E8F0");
    public Color SlotTextColor => IsAvailable ? Color.FromArgb("#166534") : Color.FromArgb("#9CA3AF");
    public string AvailabilityText => IsAvailable ? "Available" : "Booked";
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
    // Computed
    public string DateDisplay => AppointmentDate.ToString("dddd, MMM dd yyyy");
    public string TimeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public string TypeIcon => ConsultationType == "Video" ? "📹" : "🏥";
    public Color StatusColor => Status switch
    {
        "Scheduled" => Color.FromArgb("#3B82F6"),
        "Confirmed" => Color.FromArgb("#10B981"),
        "Completed" => Color.FromArgb("#8B5CF6"),
        "Cancelled" => Color.FromArgb("#EF4444"),
        "NoShow" => Color.FromArgb("#F59E0B"),
        _ => Color.FromArgb("#6B7280")
    };
    public Color StatusBg => Status switch
    {
        "Scheduled" => Color.FromArgb("#DBEAFE"),
        "Confirmed" => Color.FromArgb("#D1FAE5"),
        "Completed" => Color.FromArgb("#EDE9FE"),
        "Cancelled" => Color.FromArgb("#FEE2E2"),
        _ => Color.FromArgb("#F3F4F6")
    };
    public bool CanCancel => Status is "Scheduled" or "Confirmed" && AppointmentDate > DateTime.Today;
    public bool IsUpcoming => AppointmentDate >= DateTime.Today && Status is "Scheduled" or "Confirmed";
}

public class AppointmentResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? AppointmentId { get; set; }
}

public class PatientDetailDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? BloodType { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? PreviousIllnesses { get; set; }
    public string? CurrentMedications { get; set; }
    public string? InsuranceNumber { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Email { get; set; }
    // Computed
    public int Age => (int)((DateTime.Today - DateOfBirth).TotalDays / 365.25);
    public string AgeDisplay => $"{Age} years old";
    public string GenderIcon => Gender switch { "Male" => "♂", "Female" => "♀", _ => "⚧" };
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsMine { get; set; }
    public string TimeDisplay => SentAt.ToString("HH:mm");
}

public class ChatContactDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int UnreadCount { get; set; }
    public string LastMessage { get; set; } = "";
    public DateTime LastMessageTime { get; set; }
    public string TimeDisplay => LastMessageTime.Date == DateTime.Today
        ? LastMessageTime.ToString("HH:mm")
        : LastMessageTime.ToString("MMM dd");
    public bool HasUnread => UnreadCount > 0;
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsRead { get; set; }
    public Guid? AppointmentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TypeIcon => Type switch
    {
        "Appointment" => "📅",
        "Reminder" => "⏰",
        "Chat" => "💬",
        "System" => "⚙️",
        _ => "🔔"
    };
    public string TimeDisplay => (DateTime.UtcNow - CreatedAt) switch
    {
        var t when t.TotalMinutes < 1 => "Just now",
        var t when t.TotalMinutes < 60 => $"{(int)t.TotalMinutes}m ago",
        var t when t.TotalHours < 24 => $"{(int)t.TotalHours}h ago",
        _ => CreatedAt.ToString("MMM dd")
    };
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

public class CreateDoctorRequest
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

public class CreateNurseRequest
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
    public string RoleIcon => Role switch { "Doctor" => "👨‍⚕️", "Nurse" => "👩‍⚕️", "Admin" => "🔑", _ => "🧑" };
    public Color RoleColor => Role switch
    {
        "Doctor" => Color.FromArgb("#DBEAFE"),
        "Nurse" => Color.FromArgb("#D1FAE5"),
        "Admin" => Color.FromArgb("#FEF3C7"),
        _ => Color.FromArgb("#F3F4F6")
    };
    public string StatusDisplay => IsActive ? "Active" : "Inactive";
    public Color StatusColor => IsActive ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
}

public class IncomingCallDto
{
    public string SessionId { get; set; } = "";
    public string CallerId { get; set; } = "";
    public string CallerName { get; set; } = "";
    public string RoomId { get; set; } = "";
}

public class SpecialtyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string? Color { get; set; }
    public string Emoji => IconName switch
    {
        "heart" => "❤️", "skin" => "🧴", "brain" => "🧠", "bone" => "🦴",
        "child" => "👶", "stethoscope" => "🩺", "eye" => "👁️",
        "female" => "🌸", "mind" => "🧘", "xray" => "🔬", _ => "🏥"
    };
}
