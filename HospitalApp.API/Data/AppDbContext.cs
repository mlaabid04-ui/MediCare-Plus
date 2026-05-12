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
    public DbSet<DoctorVacation> DoctorVacations => Set<DoctorVacation>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<VideoCallSession> VideoCallSessions => Set<VideoCallSession>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();

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

        // PatientDocument
        modelBuilder.Entity<PatientDocument>(e => {
            e.HasOne(d => d.Patient).WithMany(p => p.Documents)
                .HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Cascade);
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
            // ── Original 18 (renamed to French) ──────────────────────
            S("11111111-0000-0000-0000-000000000001", "Cardiologue",                              "heart",        "#E74C3C"),
            S("11111111-0000-0000-0000-000000000002", "Dermatologue",                             "skin",         "#F39C12"),
            S("11111111-0000-0000-0000-000000000003", "Neurologue",                               "brain",        "#9B59B6"),
            S("11111111-0000-0000-0000-000000000004", "Chirurgien orthopédique",                  "bone",         "#3498DB"),
            S("11111111-0000-0000-0000-000000000005", "Pédiatre",                                 "child",        "#2ECC71"),
            S("11111111-0000-0000-0000-000000000006", "Médecin généraliste",                      "stethoscope",  "#1ABC9C"),
            S("11111111-0000-0000-0000-000000000007", "Ophtalmologue",                            "eye",          "#E67E22"),
            S("11111111-0000-0000-0000-000000000008", "Gynécologue obstétricien",                 "female",       "#E91E63"),
            S("11111111-0000-0000-0000-000000000009", "Psychiatre",                               "mind",         "#607D8B"),
            S("11111111-0000-0000-0000-000000000010", "Radiologue",                               "xray",         "#795548"),
            S("11111111-0000-0000-0000-000000000011", "Chirurgien dentiste",                      "tooth",        "#F9A825"),
            S("11111111-0000-0000-0000-000000000012", "Gastrologue entérologue",                  "stomach",      "#43A047"),
            S("11111111-0000-0000-0000-000000000013", "Kinésithérapeute",                         "physio",       "#EF6C00"),
            S("11111111-0000-0000-0000-000000000014", "Médecin légal et de travail",               "work",         "#1565C0"),
            S("11111111-0000-0000-0000-000000000015", "Endocrinologue – maladies métaboliques",    "hormone",      "#7B1FA2"),
            S("11111111-0000-0000-0000-000000000016", "Oto-rhino-laryngologue",                   "ear",          "#E65100"),
            S("11111111-0000-0000-0000-000000000017", "Pneumologue",                              "lungs",        "#00838F"),
            S("11111111-0000-0000-0000-000000000018", "Rhumatologue",                             "joint",        "#546E7A"),
            // ── New French specialties ────────────────────────────────
            S("22222222-0000-0000-0000-000000000001", "Acupuncteur",                              "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000002", "Algologue",                                "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000003", "Allergologue",                             "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000004", "Anatomo-pathologiste",                     "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000005", "Anesthésiste-réanimateur",                 "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000006", "Angiologue",                               "heart",        "#E74C3C"),
            S("22222222-0000-0000-0000-000000000007", "Autre",                                    "stethoscope",  "#94A3B8"),
            S("22222222-0000-0000-0000-000000000008", "Biologiste vétérinaire",                   "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000009", "Cancérologue",                             "stethoscope",  "#EF4444"),
            S("22222222-0000-0000-0000-000000000010", "Chirurgien cancérologue",                  "stethoscope",  "#EF4444"),
            S("22222222-0000-0000-0000-000000000011", "Chirurgien cardiovasculaire",               "heart",        "#E74C3C"),
            S("22222222-0000-0000-0000-000000000012", "Chirurgien général",                       "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000013", "Chirurgien maxillo-facial",                "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000014", "Chirurgien plasticien",                    "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000015", "Chirurgien thoracique",                    "lungs",        "#00838F"),
            S("22222222-0000-0000-0000-000000000016", "Diabétologue",                             "hormone",      "#7B1FA2"),
            S("22222222-0000-0000-0000-000000000017", "Génétique médicale",                       "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000018", "Gériatre",                                 "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000019", "Gérontologue",                             "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000020", "Gynécologue sexologue",                    "female",       "#E91E63"),
            S("22222222-0000-0000-0000-000000000021", "Homéopathe",                               "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000022", "Infectiologue",                            "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000023", "Infirmier",                                "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000024", "Médecin biologiste",                       "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000025", "Médecin interne",                          "stethoscope",  "#1ABC9C"),
            S("22222222-0000-0000-0000-000000000026", "Médecin morphologique et anti-âge",        "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000027", "Médecin Ostéopathe",                       "bone",         "#3498DB"),
            S("22222222-0000-0000-0000-000000000028", "Médecin physique et réadaptation fonctionnelle", "physio", "#EF6C00"),
            S("22222222-0000-0000-0000-000000000029", "Médecin sportif",                          "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000030", "Médecin urgentiste",                       "stethoscope",  "#EF4444"),
            S("22222222-0000-0000-0000-000000000031", "Médecine nucléaire",                       "xray",         "#795548"),
            S("22222222-0000-0000-0000-000000000032", "Néphrologue",                              "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000033", "Neurochirurgien",                          "brain",        "#9B59B6"),
            S("22222222-0000-0000-0000-000000000034", "Neuropsychiatre",                          "mind",         "#607D8B"),
            S("22222222-0000-0000-0000-000000000035", "Nutritionniste",                           "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000036", "Odontologue chirurgicale",                 "tooth",        "#F9A825"),
            S("22222222-0000-0000-0000-000000000037", "Oncologue médicale",                       "stethoscope",  "#EF4444"),
            S("22222222-0000-0000-0000-000000000038", "Opticien",                                 "eye",          "#E67E22"),
            S("22222222-0000-0000-0000-000000000039", "Orthodontiste",                            "tooth",        "#F9A825"),
            S("22222222-0000-0000-0000-000000000040", "Orthopédiste dento-faciale",               "tooth",        "#F9A825"),
            S("22222222-0000-0000-0000-000000000041", "Orthophoniste",                            "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000042", "Orthoptiste",                              "eye",          "#E67E22"),
            S("22222222-0000-0000-0000-000000000043", "Parodontologue",                           "tooth",        "#F9A825"),
            S("22222222-0000-0000-0000-000000000044", "Pédodontiste",                             "tooth",        "#F9A825"),
            S("22222222-0000-0000-0000-000000000045", "Pharmacologue",                            "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000046", "Podologue",                                "stethoscope",  "#4F46E5"),
            S("22222222-0000-0000-0000-000000000047", "Psychologue",                              "mind",         "#607D8B"),
            S("22222222-0000-0000-0000-000000000048", "Psychomotricité",                          "physio",       "#EF6C00"),
            S("22222222-0000-0000-0000-000000000049", "Radiothérapeute",                          "xray",         "#795548"),
            S("22222222-0000-0000-0000-000000000050", "Réanimateur",                              "stethoscope",  "#EF4444"),
            S("22222222-0000-0000-0000-000000000051", "Stomatologue",                             "tooth",        "#F9A825"),
            S("22222222-0000-0000-0000-000000000052", "Urologue",                                 "stethoscope",  "#4F46E5")
        );
    }

    private static Specialty S(string id, string name, string icon, string color) => new Specialty
    {
        Id = Guid.Parse(id), Name = name, Description = name, IconName = icon, Color = color
    };
}