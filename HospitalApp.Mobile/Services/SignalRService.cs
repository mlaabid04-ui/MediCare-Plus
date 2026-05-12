using HospitalApp.Mobile.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace HospitalApp.Mobile.Services;

public class SignalRService
{
    public HubConnection? ChatHub { get; private set; }
    public HubConnection? NotificationHub { get; private set; }
    public HubConnection? VideoHub { get; private set; }

    public event Action<NotificationDto>? NotificationReceived;
    public event Action<IncomingCallDto>? IncomingCallReceived;
    public event Action<IncomingChatMessageDto>? MessageReceived;
    public event Action<IncomingChatMessageDto>? MessageSent;

    public async Task ConnectAsync()
    {
        var token = Preferences.Get("Token", "");
        if (string.IsNullOrEmpty(token)) return;

        ChatHub = new HubConnectionBuilder()
            .WithUrl($"{AppConfig.HubBaseUrl}/hubs/chat",
                opts => opts.AccessTokenProvider = () => Task.FromResult(token)!)
            .WithAutomaticReconnect()
            .Build();

        NotificationHub = new HubConnectionBuilder()
            .WithUrl($"{AppConfig.HubBaseUrl}/hubs/notifications",
                opts => opts.AccessTokenProvider = () => Task.FromResult(token)!)
            .WithAutomaticReconnect()
            .Build();

        VideoHub = new HubConnectionBuilder()
            .WithUrl($"{AppConfig.HubBaseUrl}/hubs/video",
                opts => opts.AccessTokenProvider = () => Task.FromResult(token)!)
            .WithAutomaticReconnect()
            .Build();

        ChatHub.On<IncomingChatMessageDto>("ReceiveMessage", msg =>
        {
            MainThread.BeginInvokeOnMainThread(() => MessageReceived?.Invoke(msg));
        });

        ChatHub.On<IncomingChatMessageDto>("MessageSent", msg =>
        {
            MainThread.BeginInvokeOnMainThread(() => MessageSent?.Invoke(msg));
        });

        NotificationHub.On<NotificationDto>("ReceiveNotification", notif =>
        {
            MainThread.BeginInvokeOnMainThread(() => NotificationReceived?.Invoke(notif));
        });

        VideoHub.On<IncomingCallDto>("IncomingCall", call =>
        {
            MainThread.BeginInvokeOnMainThread(() => IncomingCallReceived?.Invoke(call));
        });

        try
        {
            await ChatHub.StartAsync();
            await NotificationHub.StartAsync();
            await VideoHub.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR error: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(string receiverId, string message, string? appointmentId = null)
    {
        if (ChatHub?.State == HubConnectionState.Connected)
            await ChatHub.InvokeAsync("SendMessage", receiverId, message, appointmentId);
    }

    public async Task MarkMessagesReadAsync(string senderId)
    {
        if (ChatHub?.State == HubConnectionState.Connected)
            await ChatHub.InvokeAsync("MarkMessagesRead", senderId);
    }

    public async Task InitiateCallAsync(string targetUserId, bool isVideo)
    {
        if (VideoHub?.State == HubConnectionState.Connected)
            await VideoHub.InvokeAsync("InitiateCall", targetUserId, isVideo);
    }

    public async Task DisconnectAsync()
    {
        if (ChatHub != null) await ChatHub.StopAsync();
        if (NotificationHub != null) await NotificationHub.StopAsync();
        if (VideoHub != null) await VideoHub.StopAsync();
    }

    public bool IsConnected => ChatHub?.State == HubConnectionState.Connected;
}

public static class ServiceHelper
{
    public static IServiceProvider? Services { get; set; }

    public static T GetService<T>() where T : notnull
        => Services!.GetRequiredService<T>();
}
