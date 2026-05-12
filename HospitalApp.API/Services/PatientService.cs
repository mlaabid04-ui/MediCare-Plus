using HospitalApp.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface IPatientService
{
    Task<PatientDetailDto?> GetPatientByIdAsync(Guid id);
    Task UpdatePatientAsync(Guid id, UpdatePatientDto dto);
}

public class PatientService : IPatientService
{
    private readonly AppDbContext _db;
    public PatientService(AppDbContext db) { _db = db; }

    public async Task<PatientDetailDto?> GetPatientByIdAsync(Guid id)
    {
        var p = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return null;
        return new PatientDetailDto
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            FullName = $"{p.FirstName} {p.LastName}",
            DateOfBirth = p.DateOfBirth,
            Gender = p.Gender,
            PhoneNumber = p.PhoneNumber,
            Address = p.Address,
            City = p.City,
            BloodType = p.BloodType,
            Height = p.Height,
            Weight = p.Weight,
            Allergies = p.Allergies,
            ChronicDiseases = p.ChronicDiseases,
            PreviousIllnesses = p.PreviousIllnesses,
            CurrentMedications = p.CurrentMedications,
            InsuranceNumber = p.InsuranceNumber,
            InsuranceProvider = p.InsuranceProvider,
            ProfileImageUrl = p.User?.ProfileImageUrl,
            Email = p.User?.Email
        };
    }

    public async Task UpdatePatientAsync(Guid id, UpdatePatientDto dto)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p == null) return;
        if (dto.FirstName != null)          p.FirstName = dto.FirstName;
        if (dto.LastName != null)           p.LastName = dto.LastName;
        if (dto.Gender != null)             p.Gender = dto.Gender;
        if (dto.PhoneNumber != null)        p.PhoneNumber = dto.PhoneNumber;
        if (dto.Address != null)            p.Address = dto.Address;
        if (dto.City != null)               p.City = dto.City;
        if (dto.BloodType != null)          p.BloodType = dto.BloodType;
        if (dto.Height.HasValue)            p.Height = dto.Height;
        if (dto.Weight.HasValue)            p.Weight = dto.Weight;
        if (dto.Allergies != null)          p.Allergies = dto.Allergies;
        if (dto.ChronicDiseases != null)    p.ChronicDiseases = dto.ChronicDiseases;
        if (dto.PreviousIllnesses != null)  p.PreviousIllnesses = dto.PreviousIllnesses;
        if (dto.CurrentMedications != null) p.CurrentMedications = dto.CurrentMedications;
        if (dto.InsuranceProvider != null)  p.InsuranceProvider = dto.InsuranceProvider;
        if (dto.InsuranceNumber != null)    p.InsuranceNumber = dto.InsuranceNumber;
        await _db.SaveChangesAsync();
    }
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
}

public class UpdatePatientDto
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
