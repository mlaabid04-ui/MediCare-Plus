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

public class RegisterDoctorRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? Languages { get; set; }
    public Guid SpecialtyId { get; set; }
}

// Shared wizard data — passed between the 3 registration steps
public class DoctorRegistrationData
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string City { get; set; } = "";
    public Guid SpecialtyId { get; set; }
    public string SpecialtyName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Languages { get; set; } = "";
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
    public string? HospitalName { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    // Computed display
    public string RatingDisplay => $"⭐ {Rating:F1}";
    public string FeeDisplay => $"{ConsultationFee} MAD/séance";
    public string ExperienceDisplay => $"{YearsOfExperience} ans exp.";
    public string LocationDisplay => string.IsNullOrEmpty(City) ? (HospitalName ?? "") : (HospitalName != null ? $"{HospitalName}, {City}" : City);
    // Distance from user (set by DoctorListPage after geolocation)
    public double? DistanceKm { get; set; }
    public string DistanceDisplay => DistanceKm.HasValue ? $"{DistanceKm.Value:F1} km" : "";
}

public class DoctorDetailDto : DoctorListDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Biography { get; set; }
    public string? PhoneNumber { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string? Languages { get; set; }   // "Arabe;Français;Anglais"
    public string? Diplomas { get; set; }    // newline-separated
    public string? Address { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public string? CabinetImages { get; set; }
    public bool IsProfileComplete { get; set; }
    public List<string> LanguageList => Languages?.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
    public List<string> DiplomaList  => Diplomas?.Split('\n',  StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
    public List<string> CabinetImageList => CabinetImages?.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
}

public class DoctorScheduleItemDto
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

public class UpdateDoctorProfileRequest
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
    public Guid? DoctorUserId { get; set; }
    public Guid? PatientUserId { get; set; }
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
    public string? SenderId { get; set; }   // set for Chat notifications
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

public class PatientDocumentDto
{
    public Guid Id { get; set; }
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public string Category { get; set; } = "Autre";
    public DateTime UploadedAt { get; set; }
    public string FileUrl { get; set; } = "";
    // Computed
    public string SizeDisplay => FileSize switch
    {
        < 1024 => $"{FileSize} o",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} Ko",
        _ => $"{FileSize / (1024.0 * 1024):F1} Mo"
    };
    public string CategoryIcon => Category switch
    {
        "Analyse"  => "🔬",
        "Scanner"  => "📡",
        "Radio"    => "☢️",
        "Rapport"  => "📄",
        _          => "📎"
    };
    public string TypeIcon => ContentType switch
    {
        var t when t.StartsWith("image/") => "🖼️",
        "application/pdf"                 => "📕",
        _                                 => "📁"
    };
    public string DateDisplay => UploadedAt.ToString("dd MMM yyyy");
}

public class PrescriptionDto
{
    public Guid AppointmentId { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorSpecialty { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Prescription { get; set; } = "";
    public string ConsultationType { get; set; } = "";
    public string TypeIcon => ConsultationType == "Video" ? "📹" : "🏥";
    public string DateDisplay => AppointmentDate.ToString("dd MMM yyyy");
}

public class UpdatePatientRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
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
    public string? InsuranceProvider { get; set; }
    public string? InsuranceNumber { get; set; }
}

public class IncomingCallDto
{
    public string SessionId { get; set; } = "";
    public string CallerId { get; set; } = "";
    public string CallerName { get; set; } = "";
    public string RoomId { get; set; } = "";
    public bool IsVideo { get; set; } = true;
}

public class IncomingChatMessageDto
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = "";
    public string? SenderName { get; set; }
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
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
