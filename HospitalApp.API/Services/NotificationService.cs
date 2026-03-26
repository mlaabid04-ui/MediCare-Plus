// ============================================
// Services/NotificationService.cs
// ============================================
using HospitalApp.API.Data;
using HospitalApp.API.Hubs;
using HospitalApp.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, string title, string message, string type = "Info", Guid? appointmentId = null);
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hubContext = hub;
    }

    public async Task CreateNotificationAsync(Guid userId, string title, string message, string type = "Info", Guid? appointmentId = null)
    {
        var notif = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            AppointmentId = appointmentId
        };
        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync();

        // Push real-time notification
        await _hubContext.Clients.Group(userId.ToString())
            .SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = notif.Id,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = notif.CreatedAt
            });
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                AppointmentId = n.AppointmentId,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notif = await _db.Notifications.FindAsync(notificationId);
        if (notif != null) { notif.IsRead = true; await _db.SaveChangesAsync(); }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}

// ============================================
// Services/AppointmentReminderService.cs
// Background service for 1-hour reminders
// ============================================
public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(IServiceScopeFactory sf, ILogger<AppointmentReminderService> logger)
    {
        _scopeFactory = sf;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckUpcomingAppointments();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckUpcomingAppointments()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var targetTime = now.AddHours(1);

        var upcoming = await db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p!.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Where(a => a.Status == "Scheduled" || a.Status == "Confirmed")
            .ToListAsync();

        foreach (var appt in upcoming)
        {
            var apptDateTime = appt.AppointmentDate.Date + appt.StartTime;
            var diff = apptDateTime - now;

            if (diff >= TimeSpan.FromMinutes(59) && diff <= TimeSpan.FromMinutes(61))
            {
                _logger.LogInformation($"Sending reminders for appointment {appt.Id}");

                if (appt.Patient?.User != null)
                    await notifService.CreateNotificationAsync(
                        appt.Patient.User.Id,
                        "⏰ Appointment Reminder",
                        $"Your appointment with Dr. {appt.Doctor?.FirstName} {appt.Doctor?.LastName} starts in 1 hour at {appt.StartTime:hh\\:mm}.",
                        "Reminder", appt.Id);

                if (appt.Doctor?.User != null)
                    await notifService.CreateNotificationAsync(
                        appt.Doctor.User.Id,
                        "⏰ Appointment Reminder",
                        $"You have an appointment with {appt.Patient?.FirstName} {appt.Patient?.LastName} in 1 hour at {appt.StartTime:hh\\:mm}.",
                        "Reminder", appt.Id);
            }
        }
    }
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
}
