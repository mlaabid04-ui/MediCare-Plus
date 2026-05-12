using HospitalApp.Mobile.Pages;
using HospitalApp.Mobile.Services;
using HospitalApp.Mobile.Platforms.Android;
using CommunityToolkit.Maui;

namespace HospitalApp.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
                // Custom WebView handler grants camera/mic access for Jitsi video calls
                handlers.AddHandler<WebView, CustomWebViewHandler>();
            });

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RoleSelectorPage>();
        builder.Services.AddTransient<PatientLoginPage>();
        builder.Services.AddTransient<DoctorLoginPage>();
        builder.Services.AddTransient<AdminLoginPage>();

        var app = builder.Build();
        ServiceHelper.Services = app.Services;
        return app;
    }
}