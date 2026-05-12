using HospitalApp.Mobile.Pages;
using HospitalApp.Mobile.Services;

namespace HospitalApp.Mobile;

public partial class App : Application
{
    public App(ApiService api, SignalRService signalR)
    {
        InitializeComponent();

        var token = Preferences.Get("Token", "");
        var role  = Preferences.Get("Role", "");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role))
        {
            MainPage = role switch
            {
                "Doctor" => new NavigationPage(new DoctorDashboardPage(api, signalR)),
                "Admin"  => new NavigationPage(new AdminDashboardPage(api)),
                _        => new NavigationPage(new PatientDashboardPage(api, signalR))
            };
        }
        else
        {
            // Show role selector as the new landing screen
            MainPage = new NavigationPage(new RoleSelectorPage(api));
        }
    }
}
