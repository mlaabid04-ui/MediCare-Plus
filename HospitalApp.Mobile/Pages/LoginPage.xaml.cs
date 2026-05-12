using HospitalApp.Mobile.Services;
using HospitalApp.Mobile.Pages;
using Java.Util.Logging;
using System.Net;

namespace HospitalApp.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;

    public LoginPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        ErrorFrame.IsVisible = false;

        if (string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError("Please enter email and password");
            return;
        }

        SetLoading(true);
        try
        {
            var result = await _api.LoginAsync(
                EmailEntry.Text.Trim(), PasswordEntry.Text);

            if (result.Success)
            {
                // Flavor guard: reject login if role doesn't match this APK
                var allowed = AppConfig.AllowedRole;
                if (!string.IsNullOrEmpty(allowed) && result.Role != allowed)
                {
                    ShowError($"Cette application est réservée aux {(allowed == "Doctor" ? "médecins" : "patients")}.");
                    return;
                }

                Preferences.Set("Token", result.Token ?? "");
                Preferences.Set("UserId",
                    result.UserId?.ToString() ?? "");
                Preferences.Set("ProfileId",
                    result.ProfileId?.ToString() ?? "");
                Preferences.Set("Role", result.Role ?? "");
                Preferences.Set("FullName", result.FullName ?? "");
                Preferences.Set("ProfileImageUrl",
                    result.ProfileImageUrl ?? "");

                var signalR = ServiceHelper.GetService<SignalRService>();
                await signalR.ConnectAsync();

                Page nextPage;
                if (result.Role == "Doctor")
                {
                    var doctorId = result.ProfileId ?? Guid.Empty;
                    var doctorDetail = await ServiceHelper.GetService<ApiService>().GetDoctorByIdAsync(doctorId);
                    nextPage = doctorDetail?.IsProfileComplete == false
                        ? new DoctorProfileSetupPage(ServiceHelper.GetService<ApiService>())
                        : new DoctorDashboardPage(ServiceHelper.GetService<ApiService>(), signalR);
                }
                else if (result.Role == "Admin")
                    nextPage = new AdminDashboardPage(ServiceHelper.GetService<ApiService>());
                else
                    nextPage = new PatientDashboardPage(ServiceHelper.GetService<ApiService>(), signalR);

                Application.Current!.MainPage = new NavigationPage(nextPage);
            }
            else
                ShowError(result.Message ?? "Login failed");
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally { SetLoading(false); }
    }

    private async void CreateAccount_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_api));
    }

    private void TogglePassword_Tapped(object sender, TappedEventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorFrame.IsVisible = true;
    }

    private void SetLoading(bool loading)
    {
        LoginButton.IsEnabled = !loading;
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        LoginButton.Text = loading ? "Signing in..." : "Sign In";
    }
}