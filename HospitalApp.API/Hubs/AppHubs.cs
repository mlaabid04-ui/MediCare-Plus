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

    public ChatHub(AppDbContext db) { _db = db; }

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
        if (senderId == null) return;

        var chatMsg = new ChatMessage
        {
            SenderId = Guid.Parse(senderId),
            ReceiverId = Guid.Parse(receiverId),
            Message = message,
            AppointmentId = appointmentId != null ? Guid.Parse(appointmentId) : null
        };
        _db.ChatMessages.Add(chatMsg);
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(Guid.Parse(senderId));
        var msgDto = new
        {
            id = chatMsg.Id,
            senderId = senderId,
            senderName = sender?.Email,
            message = message,
            sentAt = chatMsg.SentAt,
            isRead = false
        };

        // Send to receiver
        await Clients.Group(receiverId).SendAsync("ReceiveMessage", msgDto);
        // Send back to sender
        await Clients.Caller.SendAsync("MessageSent", msgDto);
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

    public async Task InitiateCall(string targetUserId, string appointmentId)
    {
        var callerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (callerId == null) return;

        var session = new VideoCallSession
        {
            AppointmentId = Guid.Parse(appointmentId),
            CallerId = Guid.Parse(callerId),
            ReceiverId = Guid.Parse(targetUserId),
            Status = "Pending"
        };
        _db.VideoCallSessions.Add(session);
        await _db.SaveChangesAsync();

        var caller = await _db.Users.FindAsync(Guid.Parse(callerId));
        if (_userConnections.TryGetValue(targetUserId, out var connId))
        {
            await Clients.Client(connId).SendAsync("IncomingCall", new
            {
                sessionId = session.Id,
                callerId,
                callerName = caller?.Email,
                roomId = session.RoomId
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
