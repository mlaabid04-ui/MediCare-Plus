using HospitalApp.Mobile.Pages;
using HospitalApp.Mobile.Services;
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
            });

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddTransient<LoginPage>();

        var app = builder.Build();
        ServiceHelper.Services = app.Services;
        return app;
    }
}