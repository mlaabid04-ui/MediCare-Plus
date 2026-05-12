// ============================================
// Hubs/ChatHub.cs
// ============================================
using HospitalApp.API.Data;
using HospitalApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HospitalApp.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _notifHub;

    public ChatHub(AppDbContext db, IHubContext<NotificationHub> notifHub)
    {
        _db = db;
        _notifHub = notifHub;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(string receiverId, string message, string? appointmentId = null)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(senderId)) return;
        if (string.IsNullOrEmpty(message)) return;

        if (!Guid.TryParse(senderId, out var senderGuid) ||
            !Guid.TryParse(receiverId, out var receiverGuid))
            throw new HubException("Invalid user ID format.");

        var chatMsg = new ChatMessage
        {
            SenderId = senderGuid,
            ReceiverId = receiverGuid,
            Message = message,
            AppointmentId = appointmentId != null && Guid.TryParse(appointmentId, out var apptId) ? apptId : null
        };
        _db.ChatMessages.Add(chatMsg);

        // Save notification for receiver
        var preview = message.StartsWith("[IMG:") ? "📷 Sent a photo"
                    : message.StartsWith("[FILE:") ? "📄 Sent a file"
                    : message.Length > 60 ? message[..60] + "…"
                    : message;

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == senderGuid);
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == senderGuid);
        var nurse = await _db.Nurses.FirstOrDefaultAsync(n => n.UserId == senderGuid);
        var senderName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}"
            : patient != null ? $"{patient.FirstName} {patient.LastName}"
            : nurse != null ? $"{nurse.FirstName} {nurse.LastName}"
            : (await _db.Users.FindAsync(senderGuid))?.Email ?? senderId;

        var notification = new Notification
        {
            UserId = receiverGuid,
            Title = $"New message from {senderName}",
            Message = preview,
            Type = "Chat"
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        var msgDto = new
        {
            id = chatMsg.Id,
            senderId = senderId,
            senderName,
            message = message,
            sentAt = chatMsg.SentAt,
            isRead = false
        };

        // Deliver message in real-time
        await Clients.Group(receiverId).SendAsync("ReceiveMessage", msgDto);
        await Clients.Caller.SendAsync("MessageSent", msgDto);

        // Push notification to receiver in real-time
        await _notifHub.Clients.Group(receiverId).SendAsync("ReceiveNotification", new
        {
            id = notification.Id,
            title = notification.Title,
            message = preview,
            type = "Chat",
            isRead = false,
            senderId = senderId,
            createdAt = notification.CreatedAt
        });
    }

    public async Task MarkMessagesRead(string senderId)
    {
        var receiverId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (receiverId == null) return;

        await _db.ChatMessages
            .Where(m => m.SenderId == Guid.Parse(senderId)
                     && m.ReceiverId == Guid.Parse(receiverId) && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

        await Clients.Group(senderId).SendAsync("MessagesRead", receiverId);
    }
}

// ============================================
// Hubs/NotificationHub.cs
// ============================================
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }
}

// ============================================
// Hubs/VideoCallHub.cs
// ============================================
[Authorize]
public class VideoCallHub : Hub
{
    private static readonly Dictionary<string, string> _userConnections = new();
    private readonly AppDbContext _db;

    public VideoCallHub(AppDbContext db) { _db = db; }

    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            _userConnections[userId] = Context.ConnectionId;
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            _userConnections.Remove(userId);
        return base.OnDisconnectedAsync(ex);
    }

    public async Task InitiateCall(string targetUserId, bool isVideo, string? appointmentId = null)
    {
        var callerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (callerId == null) return;

        var callerGuid = Guid.Parse(callerId);
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == callerGuid);
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == callerGuid);
        var nurse = await _db.Nurses.FirstOrDefaultAsync(n => n.UserId == callerGuid);
        var callerName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}"
            : patient != null ? $"{patient.FirstName} {patient.LastName}"
            : nurse != null ? $"{nurse.FirstName} {nurse.LastName}"
            : (await _db.Users.FindAsync(callerGuid))?.Email ?? callerId;

        var session = new VideoCallSession
        {
            AppointmentId = appointmentId != null ? Guid.Parse(appointmentId) : null,
            CallerId = callerGuid,
            ReceiverId = Guid.Parse(targetUserId),
            Status = "Pending"
        };
        _db.VideoCallSessions.Add(session);
        await _db.SaveChangesAsync();

        if (_userConnections.TryGetValue(targetUserId, out var connId))
        {
            await Clients.Client(connId).SendAsync("IncomingCall", new
            {
                sessionId = session.Id,
                callerId,
                callerName,
                roomId = session.RoomId,
                isVideo
            });
        }
    }

    public async Task AcceptCall(string sessionId)
    {
        var session = await _db.VideoCallSessions.FindAsync(Guid.Parse(sessionId));
        if (session == null) return;

        session.Status = "Active";
        session.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var callerConnId = _userConnections.GetValueOrDefault(session.CallerId.ToString());
        if (callerConnId != null)
            await Clients.Client(callerConnId).SendAsync("CallAccepted", new { roomId = session.RoomId });
    }

    public async Task EndCall(string sessionId)
    {
        var session = await _db.VideoCallSessions.FindAsync(Guid.Parse(sessionId));
        if (session == null) return;

        session.Status = "Ended";
        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var callerConnId = _userConnections.GetValueOrDefault(session.CallerId.ToString());
        var receiverConnId = _userConnections.GetValueOrDefault(session.ReceiverId.ToString());

        if (callerConnId != null) await Clients.Client(callerConnId).SendAsync("CallEnded");
        if (receiverConnId != null) await Clients.Client(receiverConnId).SendAsync("CallEnded");
    }
}
