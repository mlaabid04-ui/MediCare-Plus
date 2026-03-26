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

        NotificationHub.On<NotificationDto>("ReceiveNotification", notif =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                NotificationReceived?.Invoke(notif));
        });

        VideoHub.On<IncomingCallDto>("IncomingCall", call =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                IncomingCallReceived?.Invoke(call));
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

    public async Task DisconnectAsync()
    {
        if (ChatHub != null) await ChatHub.StopAsync();
        if (NotificationHub != null) await NotificationHub.StopAsync();
        if (VideoHub != null) await VideoHub.StopAsync();
    }

    public bool IsConnected =>
        ChatHub?.State == HubConnectionState.Connected;
}

public static class ServiceHelper
{
    public static IServiceProvider? Services { get; set; }

    public static T GetService<T>() where T : notnull
        => Services!.GetRequiredService<T>();
}
