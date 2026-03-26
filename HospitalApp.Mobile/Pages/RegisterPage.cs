using HospitalApp.Mobile.Models;
using HospitalApp.Mobile.Services;

namespace HospitalApp.Mobile.Pages;

public class RegisterPage : ContentPage
{
    private readonly ApiService _api;
    private string _selectedGender = "";

    // Fields
    private Entry _firstNameEntry, _lastNameEntry, _emailEntry,
                  _passwordEntry, _confirmPasswordEntry, _phoneEntry,
                  _cityEntry, _heightEntry, _weightEntry,
                  _insuranceProviderEntry, _insuranceNumberEntry,
                  _emergencyNameEntry, _emergencyPhoneEntry;
    private DatePicker _birthDatePicker;
    private Picker _bloodTypePicker;
    private Editor _allergiesEditor, _chronicEditor,
                   _previousIllnessesEditor, _medicationsEditor;
    private Frame _maleBtn, _femaleBtn, _otherBtn, _errorFrame;
    private Label _errorLabel;

    public RegisterPage(ApiService api)
    {
        _api = api;
        Title = "Create Account";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildUI();
    }

    private void BuildUI()
    {
        var scroll = new ScrollView();
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(20, 16),
            Spacing = 12
        };

        // Header
        stack.Children.Add(CreateSectionHeader("👤", "Patient Registration",
            "Fill in your complete medical profile"));

        // Personal Info
        stack.Children.Add(CreateSectionCard("👤 Personal Information",
            BuildPersonalSection()));

        // Credentials
        stack.Children.Add(CreateSectionCard("🔐 Account Credentials",
            BuildCredentialsSection()));

        // Medical Info
        stack.Children.Add(CreateSectionCard("🩺 Medical Information",
            BuildMedicalSection()));

        // Insurance
        stack.Children.Add(CreateSectionCard("🛡️ Insurance & Emergency",
            BuildInsuranceSection()));

        // Error frame
        _errorLabel = new Label
        {
            TextColor = Color.FromArgb("#DC2626"),
            FontSize = 13
        };
        _errorFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#FEE2E2"),
            BorderColor = Color.FromArgb("#FECACA"),
            CornerRadius = 12,
            Padding = new Thickness(14),
            IsVisible = false,
            HasShadow = false,
            Content = _errorLabel
        };
        stack.Children.Add(_errorFrame);

        // Register Button
        var registerBtn = new Button
        {
            Text = "Create My Account 🚀",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 52,
            Margin = new Thickness(0, 8, 0, 0)
        };
        registerBtn.Clicked += RegisterButton_Clicked;
        stack.Children.Add(registerBtn);
        stack.Children.Add(new BoxView
        {
            HeightRequest = 40,
            Color = Colors.Transparent
        });

        scroll.Content = stack;
        Content = scroll;
    }

    private Frame CreateSectionHeader(string emoji, string title, string subtitle)
    {
        return new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = new Thickness(16),
            HasShadow = true,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new HorizontalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Color.FromArgb("#DBEAFE"),
                        CornerRadius = 20, Padding = new Thickness(12),
                        WidthRequest = 52, HeightRequest = 52,
                        HasShadow = false, BorderColor = Colors.Transparent,
                        Content = new Label
                        {
                            Text = emoji, FontSize = 24,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    },
                    new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                            new Label { Text = subtitle, FontSize = 12, TextColor = Color.FromArgb("#64748B") }
                        }
                    }
                }
            }
        };
    }

    private Frame CreateSectionCard(string title, View content)
    {
        var inner = new VerticalStackLayout { Spacing = 14 };
        inner.Children.Add(new Label
        {
            Text = title,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E293B")
        });
        inner.Children.Add(content);
        return new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = new Thickness(16),
            HasShadow = true,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = inner
        };
    }

    private View BuildPersonalSection()
    {
        _firstNameEntry = CreateEntry("First Name");
        _lastNameEntry = CreateEntry("Last Name");
        _phoneEntry = CreateEntry("Phone Number", Keyboard.Telephone);
        _cityEntry = CreateEntry("City");
        _birthDatePicker = new DatePicker
        {
            Format = "dd/MM/yyyy",
            TextColor = Color.FromArgb("#1E293B"),
            BackgroundColor = Color.FromArgb("#F9FAFB")
        };

        // Gender buttons
        _maleBtn = CreateGenderBtn("♂ Male", "Male");
        _femaleBtn = CreateGenderBtn("♀ Female", "Female");
        _otherBtn = CreateGenderBtn("Other", "Other");
        var genderRow = new HorizontalStackLayout
        {
            Spacing = 10,
            Children = { _maleBtn, _femaleBtn, _otherBtn }
        };

        var nameGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12,
            Children = { _firstNameEntry }
        };
        Grid.SetColumn(_lastNameEntry, 1);
        nameGrid.Children.Add(_lastNameEntry);

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(nameGrid);
        stack.Children.Add(CreateFieldGroup("Date of Birth *", _birthDatePicker));
        stack.Children.Add(CreateFieldGroup("Gender *", genderRow));
        stack.Children.Add(CreateFieldGroup("Phone Number *", _phoneEntry));
        stack.Children.Add(CreateFieldGroup("City", _cityEntry));
        return stack;
    }

    private View BuildCredentialsSection()
    {
        _emailEntry = CreateEntry("Email Address", Keyboard.Email);
        _passwordEntry = CreateEntry("Password");
        _passwordEntry.IsPassword = true;
        _confirmPasswordEntry = CreateEntry("Confirm Password");
        _confirmPasswordEntry.IsPassword = true;

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(CreateFieldGroup("Email *", _emailEntry));
        stack.Children.Add(CreateFieldGroup("Password *", _passwordEntry));
        stack.Children.Add(CreateFieldGroup("Confirm Password *",
            _confirmPasswordEntry));
        return stack;
    }

    private View BuildMedicalSection()
    {
        _heightEntry = CreateEntry("170", Keyboard.Numeric);
        _weightEntry = CreateEntry("70", Keyboard.Numeric);
        _bloodTypePicker = new Picker
        {
            TextColor = Color.FromArgb("#1E293B"),
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            Title = "Select blood type"
        };
        foreach (var bt in new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" })
            _bloodTypePicker.Items.Add(bt);

        _allergiesEditor = CreateEditor("e.g. Penicillin, Peanuts...");
        _chronicEditor = CreateEditor("e.g. Diabetes, Hypertension...");
        _previousIllnessesEditor = CreateEditor("List previous conditions...");
        _medicationsEditor = CreateEditor("List current medications...");

        var bwGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        var btStack = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "Blood Type", FontSize = 12, TextColor = Color.FromArgb("#374151"), FontAttributes = FontAttributes.Bold },
                _bloodTypePicker
            }
        };
        var htStack = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "Height (cm)", FontSize = 12, TextColor = Color.FromArgb("#374151"), FontAttributes = FontAttributes.Bold },
                _heightEntry
            }
        };
        bwGrid.Children.Add(btStack);
        Grid.SetColumn(htStack, 1);
        bwGrid.Children.Add(htStack);

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(bwGrid);
        stack.Children.Add(CreateFieldGroup("Weight (kg)", _weightEntry));
        stack.Children.Add(CreateFieldGroup("Known Allergies", _allergiesEditor));
        stack.Children.Add(CreateFieldGroup("Chronic Diseases", _chronicEditor));
        stack.Children.Add(CreateFieldGroup("Previous Illnesses",
            _previousIllnessesEditor));
        stack.Children.Add(CreateFieldGroup("Current Medications",
            _medicationsEditor));
        return stack;
    }

    private View BuildInsuranceSection()
    {
        _insuranceProviderEntry = CreateEntry("e.g. Blue Cross");
        _insuranceNumberEntry = CreateEntry("INS-XXXXXXXX");
        _emergencyNameEntry = CreateEntry("Contact person name");
        _emergencyPhoneEntry = CreateEntry("+1 234 567 890",
            Keyboard.Telephone);

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(CreateFieldGroup("Insurance Provider",
            _insuranceProviderEntry));
        stack.Children.Add(CreateFieldGroup("Insurance Number",
            _insuranceNumberEntry));
        stack.Children.Add(CreateFieldGroup("Emergency Contact Name",
            _emergencyNameEntry));
        stack.Children.Add(CreateFieldGroup("Emergency Contact Phone",
            _emergencyPhoneEntry));
        return stack;
    }

    private View CreateFieldGroup(string label, View field)
    {
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = label, FontSize = 12,
                    TextColor = Color.FromArgb("#374151"),
                    FontAttributes = FontAttributes.Bold
                },
                field
            }
        };
    }

    private Entry CreateEntry(string placeholder,
        Keyboard? keyboard = null)
    {
        return new Entry
        {
            Placeholder = placeholder,
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            TextColor = Color.FromArgb("#1E293B"),
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            FontSize = 14,
            HeightRequest = 48,
            Keyboard = keyboard ?? Keyboard.Default
        };
    }

    private Editor CreateEditor(string placeholder)
    {
        return new Editor
        {
            Placeholder = placeholder,
            HeightRequest = 80,
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            TextColor = Color.FromArgb("#1E293B"),
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            FontSize = 14,
            AutoSize = EditorAutoSizeOption.TextChanges
        };
    }

    private Frame CreateGenderBtn(string text, string gender)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#F3F4F6"),
            CornerRadius = 10,
            Padding = new Thickness(14, 10),
            BorderColor = Color.FromArgb("#E2E8F0"),
            HasShadow = false,
            Content = new Label
            {
                Text = text,
                TextColor = Color.FromArgb("#1E293B"),
                FontSize = 14
            }
        };
        frame.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => SelectGender(gender))
        });
        return frame;
    }

    private void SelectGender(string gender)
    {
        _selectedGender = gender;
        _maleBtn.BackgroundColor = gender == "Male"
            ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#F3F4F6");
        _femaleBtn.BackgroundColor = gender == "Female"
            ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#F3F4F6");
        _otherBtn.BackgroundColor = gender == "Other"
            ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#F3F4F6");
    }

    private void ShowError(string msg)
    {
        _errorLabel.Text = msg;
        _errorFrame.IsVisible = true;
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        _errorFrame.IsVisible = false;

        if (string.IsNullOrWhiteSpace(_firstNameEntry.Text) ||
            string.IsNullOrWhiteSpace(_lastNameEntry.Text) ||
            string.IsNullOrWhiteSpace(_emailEntry.Text) ||
            string.IsNullOrWhiteSpace(_passwordEntry.Text))
        {
            ShowError("Please fill all required fields");
            return;
        }

        if (_passwordEntry.Text != _confirmPasswordEntry.Text)
        {
            ShowError("Passwords do not match");
            return;
        }

        try
        {
            var result = await _api.RegisterPatientAsync(
                new RegisterPatientRequest
                {
                    FirstName = _firstNameEntry.Text,
                    LastName = _lastNameEntry.Text,
                    Email = _emailEntry.Text,
                    Password = _passwordEntry.Text,
                    DateOfBirth = _birthDatePicker.Date,
                    Gender = _selectedGender,
                    PhoneNumber = _phoneEntry.Text ?? "",
                    City = _cityEntry.Text,
                    BloodType = _bloodTypePicker.SelectedItem?.ToString(),
                    Height = decimal.TryParse(_heightEntry.Text, out var h)
                        ? h : null,
                    Weight = decimal.TryParse(_weightEntry.Text, out var w)
                        ? w : null,
                    Allergies = _allergiesEditor.Text,
                    ChronicDiseases = _chronicEditor.Text,
                    PreviousIllnesses = _previousIllnessesEditor.Text,
                    CurrentMedications = _medicationsEditor.Text,
                    InsuranceProvider = _insuranceProviderEntry.Text,
                    InsuranceNumber = _insuranceNumberEntry.Text,
                    EmergencyContactName = _emergencyNameEntry.Text,
                    EmergencyContactPhone = _emergencyPhoneEntry.Text
                });

            if (result.Success)
            {
                Preferences.Set("Token", result.Token ?? "");
                Preferences.Set("UserId",
                    result.UserId?.ToString() ?? "");
                Preferences.Set("ProfileId",
                    result.ProfileId?.ToString() ?? "");
                Preferences.Set("Role", "Patient");
                Preferences.Set("FullName", result.FullName ?? "");

                Application.Current!.MainPage = new NavigationPage(
                    new PatientDashboardPage(
                        ServiceHelper.GetService<ApiService>()));
            }
            else
                ShowError(result.Message ?? "Registration failed");
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }
}