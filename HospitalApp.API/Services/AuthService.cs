using HospitalApp.API.Data;
using HospitalApp.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HospitalApp.API.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterPatientAsync(RegisterPatientDto dto);
    Task<string> GenerateTokenAsync(User user);
    Task<AuthResult> RefreshTokenAsync(string rawRefreshToken);
    Task RevokeRefreshTokenAsync(string rawRefreshToken);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return new AuthResult { Success = false, Message = "Invalid credentials" };

        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        string? fullName = null;
        Guid? profileId = null;

        if (user.Role == "Doctor")
        {
            var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            fullName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}" : null;
            profileId = doctor?.Id;
        }
        else if (user.Role == "Patient")
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            fullName = patient != null ? $"{patient.FirstName} {patient.LastName}" : null;
            profileId = patient?.Id;
        }
        else if (user.Role == "Admin")
        {
            fullName = "Hospital Admin";
        }

        var accessToken = await GenerateTokenAsync(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            Role = user.Role,
            UserId = user.Id,
            ProfileId = profileId,
            FullName = fullName,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }

    public async Task<AuthResult> RegisterPatientAsync(RegisterPatientDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return new AuthResult { Success = false, Message = "Email already registered" };

        var now = DateTime.UtcNow;

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Patient",
            IsActive = true,
            CreatedAt = now,
            NotificationsEnabled = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var patient = new Patient
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc),
            Gender = dto.Gender,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            City = dto.City,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            BloodType = dto.BloodType,
            Height = dto.Height,
            Weight = dto.Weight,
            Allergies = dto.Allergies,
            ChronicDiseases = dto.ChronicDiseases,
            PreviousIllnesses = dto.PreviousIllnesses,
            CurrentMedications = dto.CurrentMedications,
            InsuranceNumber = dto.InsuranceNumber,
            InsuranceProvider = dto.InsuranceProvider,
            CreatedAt = now
        };
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var accessToken = await GenerateTokenAsync(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            Role = "Patient",
            UserId = user.Id,
            ProfileId = patient.Id,
            FullName = $"{patient.FirstName} {patient.LastName}"
        };
    }

    public Task<string> GenerateTokenAsync(User user)
    {
        var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);
        var tokenHandler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(descriptor);
        return Task.FromResult(tokenHandler.WriteToken(token));
    }

    public async Task<AuthResult> RefreshTokenAsync(string rawRefreshToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == tokenHash);

        if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return new AuthResult { Success = false, Message = "Invalid or expired refresh token" };

        var newAccessToken = await GenerateTokenAsync(stored.User!);
        return new AuthResult { Success = true, Token = newAccessToken };
    }

    public async Task RevokeRefreshTokenAsync(string rawRefreshToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash);
        if (stored != null)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = HashToken(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return raw;
    }

    private static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }
}

public class LoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = "";
}

public class RegisterPatientDto
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
