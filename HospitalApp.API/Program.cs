// ============================================
// HospitalApp.API - Program.cs
// ============================================

using HospitalApp.API.Data;
using HospitalApp.API.Hubs;
using HospitalApp.API.Models;
using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

var builder = WebApplication.CreateBuilder(args);

// ── Port: Railway injects PORT, fallback to 5001 for local ──
var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── Database: Railway injects DATABASE_URL (postgresql://...) ──
var connectionString = ParseConnectionString();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

static string ParseConnectionString()
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        // Railway format: postgresql://user:pass@host:port/dbname
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    }
    // Local fallback
    return "Host=localhost;Port=5432;Database=HospitalDB;Username=postgres;Password=123456789";
}

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "HospitalSecretKey2024!@#$VeryLong";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents {
            OnMessageReceived = ctx => {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSignalR(opts =>
{
    opts.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB (for base64 images/files)
    opts.EnableDetailedErrors = true;
});
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddHostedService<AppointmentReminderService>();
builder.Services.Configure<HostOptions>(opts =>
    opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

var app = builder.Build();

// Ensure upload folder exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads", "documents");
Directory.CreateDirectory(uploadsPath);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<VideoCallHub>("/hubs/video");

// Auto migrate
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

app.Run();
