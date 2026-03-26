// ============================================
// Data/AppDbContext.cs
// ============================================
using HospitalApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Data;

public class AppDbContext : DbContext
{
    static AppDbContext()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Nurse> Nurses => Set<Nurse>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<VideoCallSession> VideoCallSessions => Set<VideoCallSession>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(e => {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Id).ValueGeneratedOnAdd();
        });

        // Doctor
        modelBuilder.Entity<Doctor>(e => {
            e.HasOne(d => d.User).WithOne()
                .HasForeignKey<Doctor>(d => d.UserId);
            e.HasOne(d => d.Specialty).WithMany(s => s.Doctors)
                .HasForeignKey(d => d.SpecialtyId);
            e.Property(d => d.ConsultationFee)
                .HasColumnType("decimal(10,2)");
            e.Property(d => d.Rating)
                .HasColumnType("decimal(3,2)");
        });

        // Patient
        modelBuilder.Entity<Patient>(e => {
            e.HasOne(p => p.User).WithOne()
                .HasForeignKey<Patient>(p => p.UserId);
            e.Property(p => p.Height)
                .HasColumnType("decimal(5,2)");
            e.Property(p => p.Weight)
                .HasColumnType("decimal(5,2)");
        });

        // Appointment
        modelBuilder.Entity<Appointment>(e => {
            e.HasOne(a => a.Doctor).WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId);
            e.HasOne(a => a.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId);
            e.HasIndex(a => new
            {
                a.DoctorId,
                a.AppointmentDate,
                a.StartTime
            }).IsUnique();
        });

        // Seed Specialties
        modelBuilder.Entity<Specialty>().HasData(
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                Name = "Cardiology",
                Description = "Heart and cardiovascular system",
                IconName = "heart",
                Color = "#E74C3C"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000002"),
                Name = "Dermatology",
                Description = "Skin, hair and nails",
                IconName = "skin",
                Color = "#F39C12"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000003"),
                Name = "Neurology",
                Description = "Brain and nervous system",
                IconName = "brain",
                Color = "#9B59B6"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000004"),
                Name = "Orthopedics",
                Description = "Bones, joints and muscles",
                IconName = "bone",
                Color = "#3498DB"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000005"),
                Name = "Pediatrics",
                Description = "Children health",
                IconName = "child",
                Color = "#2ECC71"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000006"),
                Name = "General Medicine",
                Description = "General health care",
                IconName = "stethoscope",
                Color = "#1ABC9C"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000007"),
                Name = "Ophthalmology",
                Description = "Eyes and vision",
                IconName = "eye",
                Color = "#E67E22"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000008"),
                Name = "Gynecology",
                Description = "Women reproductive health",
                IconName = "female",
                Color = "#E91E63"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000009"),
                Name = "Psychiatry",
                Description = "Mental health",
                IconName = "mind",
                Color = "#607D8B"
            },
            new Specialty
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000010"),
                Name = "Radiology",
                Description = "Medical imaging",
                IconName = "xray",
                Color = "#795548"
            }
        );
    }
}