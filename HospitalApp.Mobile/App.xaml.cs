using HospitalApp.Mobile.Pages;
using HospitalApp.Mobile.Services;

namespace HospitalApp.Mobile;

public partial class App : Application
{
    public App(ApiService api)
    {
        InitializeComponent();

        var token = Preferences.Get("Token", "");
        var role = Preferences.Get("Role", "");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role))
        {
            MainPage = role switch
            {
                "Doctor" => new NavigationPage(new DoctorDashboardPage(api)),
                "Admin" => new NavigationPage(new AdminDashboardPage(api)),
                _ => new NavigationPage(new PatientDashboardPage(api))
            };
        }
        else
        {
            MainPage = new NavigationPage(new LoginPage(api));
        }
    }
}