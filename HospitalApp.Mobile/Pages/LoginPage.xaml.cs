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
                Preferences.Set("Token", result.Token ?? "");
                Preferences.Set("UserId",
                    result.UserId?.ToString() ?? "");
                Preferences.Set("ProfileId",
                    result.ProfileId?.ToString() ?? "");
                Preferences.Set("Role", result.Role ?? "");
                Preferences.Set("FullName", result.FullName ?? "");
                Preferences.Set("ProfileImageUrl",
                    result.ProfileImageUrl ?? "");

                Application.Current!.MainPage = result.Role switch
                {
                    "Doctor" => new NavigationPage(
                        new DoctorDashboardPage(
                            ServiceHelper.GetService<ApiService>())),
                    "Admin" => new NavigationPage(
                        new AdminDashboardPage(
                            ServiceHelper.GetService<ApiService>())),
                    _ => new NavigationPage(
                        new PatientDashboardPage(
                            ServiceHelper.GetService<ApiService>()))
                };
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