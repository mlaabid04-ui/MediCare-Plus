using HospitalApp.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface IChatService
{
    Task<List<ChatMessageDto>> GetMessagesAsync(Guid userId, Guid otherUserId);
    Task<List<ChatContactDto>> GetContactsAsync(Guid userId);
}

public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    public ChatService(AppDbContext db) { _db = db; }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(Guid userId, Guid otherUserId)
    {
        return await _db.ChatMessages
            .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                        (m.SenderId == otherUserId && m.ReceiverId == userId))
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Message = m.Message,
                IsRead = m.IsRead,
                SentAt = m.SentAt,
                IsMine = m.SenderId == userId
            }).ToListAsync();
    }

    public async Task<List<ChatContactDto>> GetContactsAsync(Guid userId)
    {
        var messages = await _db.ChatMessages
            .Include(m => m.Sender).Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .ToListAsync();

        var contacts = messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => {
                var other = g.First().SenderId == userId ? g.First().Receiver! : g.First().Sender!;
                return new ChatContactDto
                {
                    UserId = other.Id,
                    Email = other.Email,
                    ProfileImageUrl = other.ProfileImageUrl,
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead),
                    LastMessage = g.OrderByDescending(m => m.SentAt).First().Message,
                    LastMessageTime = g.OrderByDescending(m => m.SentAt).First().SentAt
                };
            })
            .OrderByDescending(c => c.LastMessageTime)
            .ToList();

        // Resolve display names from profiles
        var contactUserIds = contacts.Select(c => c.UserId).ToList();
        var doctors = await _db.Doctors.Where(d => contactUserIds.Contains(d.UserId))
            .ToDictionaryAsync(d => d.UserId, d => $"Dr. {d.FirstName} {d.LastName}");
        var patients = await _db.Patients.Where(p => contactUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => $"{p.FirstName} {p.LastName}");
        var nurses = await _db.Nurses.Where(n => contactUserIds.Contains(n.UserId))
            .ToDictionaryAsync(n => n.UserId, n => $"{n.FirstName} {n.LastName}");

        foreach (var c in contacts)
        {
            c.DisplayName = doctors.TryGetValue(c.UserId, out var dn) ? dn
                : patients.TryGetValue(c.UserId, out var pn) ? pn
                : nurses.TryGetValue(c.UserId, out var nn) ? nn
                : null;
        }

        return contacts;
    }
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
}
