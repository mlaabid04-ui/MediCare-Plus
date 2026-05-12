using HospitalApp.Mobile.Models;
using HospitalApp.Mobile.Services;

namespace HospitalApp.Mobile.Pages;

// =============================================
// REGISTER PAGE  –  Doctori.ma style
// =============================================
public class RegisterPage : ContentPage
{
    private readonly ApiService _api;
    private string _selectedGender = "";

    private Entry _firstNameEntry = new(), _lastNameEntry = new();
    private Entry _emailEntry = new(), _passwordEntry = new();
    private Entry _phoneEntry = new();
    private DatePicker _birthDatePicker = new();
    private Picker _genderPicker = new();
    private Frame _maleBtn = new(), _femaleBtn = new();
    private Frame _errorFrame = new();
    private Label _errorLabel = new();
    private bool _acceptCgu, _acceptPrivacy;
    private Frame _cguCheck = new(), _privacyCheck = new();
    private Button _registerBtn = new();
    private ActivityIndicator _spinner = new();

    private static readonly Color Teal   = Color.FromArgb("#4A8B9E");
    private static readonly Color TealDk = Color.FromArgb("#3A7A8C");
    private static readonly Color Navy   = Color.FromArgb("#1E2D4A");
    private static readonly Color Border = Color.FromArgb("#D1D5DB");
    private static readonly Color Sub    = Color.FromArgb("#6B7280");

    public RegisterPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Teal;
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new(GridLength.Auto),  // teal header with logo
                new(GridLength.Star)   // white card
            },
            BackgroundColor = Teal
        };

        // ── TEAL HEADER ───────────────────────────────────────────────
        var header = new VerticalStackLayout
        {
            Padding = new Thickness(24, 56, 24, 32),
            Spacing = 14,
            HorizontalOptions = LayoutOptions.Center
        };

        var logoRow = new HorizontalStackLayout { Spacing = 10, HorizontalOptions = LayoutOptions.Center };
        logoRow.Children.Add(new Frame
        {
            WidthRequest = 38, HeightRequest = 38, CornerRadius = 19,
            BackgroundColor = Colors.White.WithAlpha(0.25f),
            BorderColor = Colors.Transparent, HasShadow = false, Padding = 0,
            Content = new Label { Text = "🩺", FontSize = 20,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        });
        logoRow.Children.Add(new Label
        {
            Text = "MediCare+", FontSize = 24, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White, VerticalOptions = LayoutOptions.Center
        });
        header.Children.Add(logoRow);

        header.Children.Add(new Label
        {
            Text = "Créer votre dossier médical",
            FontSize = 22, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        });

        root.Children.Add(header);

        // ── WHITE CARD ────────────────────────────────────────────────
        var card = new Frame
        {
            CornerRadius = 28,
            BackgroundColor = Colors.White,
            HasShadow = false, BorderColor = Colors.Transparent,
            Padding = new Thickness(24, 28, 24, 40),
            Margin = new Thickness(0, -10, 0, 0)
        };

        var cardStack = new VerticalStackLayout { Spacing = 16 };

        cardStack.Children.Add(new Label
        {
            Text = "C'est gratuit et le sera toujours.",
            FontSize = 14, TextColor = Sub,
            Margin = new Thickness(0, 0, 0, 4)
        });

        // ── Prénom + Nom ──────────────────────────────────────────────
        _firstNameEntry = DEntry("Prénom *");
        _lastNameEntry  = DEntry("Nom *");
        var nameGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
                { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 12
        };
        nameGrid.Children.Add(InputBox("👤", _firstNameEntry));
        var lastBox = InputBox("👤", _lastNameEntry);
        Grid.SetColumn(lastBox, 1);
        nameGrid.Children.Add(lastBox);
        cardStack.Children.Add(nameGrid);

        // ── Genre ─────────────────────────────────────────────────────
        _maleBtn   = GenderBtn("♂  Homme", "Homme");
        _femaleBtn = GenderBtn("♀  Femme", "Femme");
        var genderRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
                { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 12
        };
        genderRow.Children.Add(_maleBtn);
        Grid.SetColumn(_femaleBtn, 1); genderRow.Children.Add(_femaleBtn);
        cardStack.Children.Add(LabeledField("Genre *", genderRow));

        // ── Téléphone ─────────────────────────────────────────────────
        _phoneEntry = DEntry("Téléphone mobile *", Keyboard.Telephone);
        var phoneBox = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
                { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 0
        };
        var flagBadge = new Frame
        {
            BackgroundColor = Colors.White, BorderColor = Border,
            CornerRadius = 8, Padding = new Thickness(10, 0),
            HasShadow = false,
            Content = new HorizontalStackLayout
            {
                Spacing = 4, VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "🇲🇦", FontSize = 18, VerticalOptions = LayoutOptions.Center },
                    new Label { Text = "+212", FontSize = 13, TextColor = Navy, VerticalOptions = LayoutOptions.Center }
                }
            }
        };
        var phoneField = new Frame
        {
            BorderColor = Border, CornerRadius = 8, Padding = new Thickness(12, 0),
            HasShadow = false, BackgroundColor = Colors.White,
            Content = _phoneEntry
        };
        phoneBox.Children.Add(flagBadge);
        Grid.SetColumn(phoneField, 1); phoneBox.Children.Add(phoneField);
        cardStack.Children.Add(LabeledField("Téléphone mobile *", phoneBox));

        // ── Date de naissance ─────────────────────────────────────────
        _birthDatePicker = new DatePicker
        {
            Format = "dd/MM/yyyy", MaximumDate = DateTime.Today,
            TextColor = Navy, BackgroundColor = Colors.Transparent
        };
        var dobBox = new Frame
        {
            BorderColor = Border, CornerRadius = 8, Padding = new Thickness(12, 0),
            HasShadow = false, BackgroundColor = Colors.White
        };
        var dobGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            Padding = 0, ColumnSpacing = 8
        };
        dobGrid.Children.Add(new Label { Text = "📅", FontSize = 18, TextColor = Teal, VerticalOptions = LayoutOptions.Center });
        Grid.SetColumn(_birthDatePicker, 1); dobGrid.Children.Add(_birthDatePicker);
        dobBox.Content = dobGrid;
        cardStack.Children.Add(LabeledField("Date de naissance *", dobBox));

        // ── Email ─────────────────────────────────────────────────────
        _emailEntry = DEntry("Email *", Keyboard.Email);
        cardStack.Children.Add(InputBox("@", _emailEntry, isTealText: true));

        // ── Mot de passe ──────────────────────────────────────────────
        _passwordEntry = DEntry("Mot de passe *");
        _passwordEntry.IsPassword = true;
        var eyeLbl = new Label { Text = "👁", FontSize = 18, TextColor = Sub, VerticalOptions = LayoutOptions.Center };
        eyeLbl.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(() => _passwordEntry.IsPassword = !_passwordEntry.IsPassword) });
        cardStack.Children.Add(InputBox("🔒", _passwordEntry, trailing: eyeLbl));

        // ── Checkboxes ────────────────────────────────────────────────
        cardStack.Children.Add(CheckRow(ref _cguCheck, ref _acceptCgu,
            "J'accepte les ", "Conditions Générales", " d'utilisation du service."));
        cardStack.Children.Add(CheckRow(ref _privacyCheck, ref _acceptPrivacy,
            "J'accepte ", "la politique de confidentialité", ""));

        // ── CNDP notice ───────────────────────────────────────────────
        cardStack.Children.Add(new Frame
        {
            BackgroundColor = Color.FromArgb("#E8F4F7"),
            BorderColor = Color.FromArgb("#B0D8E5"),
            CornerRadius = 8, Padding = new Thickness(14, 12), HasShadow = false,
            Content = new Label
            {
                Text = "Conformément à la loi 09-08, vous disposez d'un droit d'accès, de rectification et d'opposition au traitement de vos données personnelles. Ce traitement a été autorisé par la CNDP sous le N° A-S-908/2023",
                FontSize = 11, TextColor = Color.FromArgb("#2E6E82"),
                LineBreakMode = LineBreakMode.WordWrap
            }
        });

        cardStack.Children.Add(new Label
            { Text = "Champs obligatoires *", FontSize = 11, TextColor = Color.FromArgb("#EF4444") });

        // ── Error ─────────────────────────────────────────────────────
        _errorLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
        _errorFrame = new Frame { BackgroundColor = Color.FromArgb("#FEF2F2"), BorderColor = Color.FromArgb("#FECACA"),
            CornerRadius = 8, Padding = 12, IsVisible = false, HasShadow = false, Content = _errorLabel };
        cardStack.Children.Add(_errorFrame);

        // ── S'inscrire button (right-aligned) ─────────────────────────
        _registerBtn = new Button
        {
            Text = "S'inscrire",
            BackgroundColor = Teal, TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold, FontSize = 15,
            CornerRadius = 8, HeightRequest = 50,
            WidthRequest = 160, HorizontalOptions = LayoutOptions.End
        };
        _registerBtn.Clicked += RegisterButton_Clicked;
        cardStack.Children.Add(_registerBtn);

        _spinner = new ActivityIndicator { Color = Teal, IsRunning = false, IsVisible = false,
            HorizontalOptions = LayoutOptions.Center };
        cardStack.Children.Add(_spinner);

        // ── Login link ────────────────────────────────────────────────
        var loginRow = new HorizontalStackLayout
            { HorizontalOptions = LayoutOptions.Center, Spacing = 4, Margin = new Thickness(0, 6, 0, 0) };
        loginRow.Children.Add(new Label
            { Text = "Vous avez déjà un compte?", FontSize = 13, TextColor = Sub });
        var loginLink = new Label
            { Text = "Connectez-vous à votre compte", FontSize = 13, TextColor = Teal, FontAttributes = FontAttributes.Bold };
        loginLink.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await Navigation.PopAsync()) });
        loginRow.Children.Add(loginLink);
        cardStack.Children.Add(loginRow);

        // ── Footer ────────────────────────────────────────────────────
        cardStack.Children.Add(new Label
        {
            Text = "©Copyrights MediCare+ 2026 – Tous droits réservés.",
            FontSize = 11, TextColor = Color.FromArgb("#9CA3AF"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });

        card.Content = new ScrollView { Content = cardStack };
        Grid.SetRow(card, 1);
        root.Children.Add(card);

        Content = new ScrollView
        {
            Content = root,
            BackgroundColor = Teal
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static Entry DEntry(string placeholder, Keyboard? kb = null) => new()
    {
        Placeholder = placeholder, Keyboard = kb ?? Keyboard.Default,
        BackgroundColor = Colors.Transparent, HeightRequest = 50,
        TextColor = Navy, PlaceholderColor = Sub, FontSize = 14
    };

    private static View InputBox(string icon, Entry entry, View? trailing = null, bool isTealText = false)
    {
        var cols = trailing != null
            ? new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
            : new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) };
        var grid = new Grid { ColumnDefinitions = cols, Padding = new Thickness(12, 0), ColumnSpacing = 8 };
        grid.Children.Add(new Label { Text = icon, FontSize = 17,
            TextColor = isTealText ? Teal : Teal, VerticalOptions = LayoutOptions.Center });
        Grid.SetColumn(entry, 1); grid.Children.Add(entry);
        if (trailing != null) { Grid.SetColumn(trailing, 2); grid.Children.Add(trailing); }
        return new Frame { BorderColor = Border, CornerRadius = 8, Padding = 0, HasShadow = false, BackgroundColor = Colors.White, Content = grid };
    }

    private static View LabeledField(string label, View field) =>
        new VerticalStackLayout { Spacing = 6, Children =
        {
            new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Navy },
            field
        }};

    private Frame GenderBtn(string text, string gender)
    {
        var f = new Frame
        {
            BackgroundColor = Color.FromArgb("#F3F4F6"), CornerRadius = 8,
            Padding = new Thickness(10, 10), BorderColor = Border, HasShadow = false,
            Content = new Label { Text = text, FontSize = 12, TextColor = Navy,
                HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }
        };
        f.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectGender(gender)) });
        return f;
    }

    private void SelectGender(string gender)
    {
        _selectedGender = gender;
        _maleBtn.BackgroundColor   = gender == "Homme" ? Teal : Color.FromArgb("#F3F4F6");
        _femaleBtn.BackgroundColor = gender == "Femme" ? Teal : Color.FromArgb("#F3F4F6");
        ((Label)_maleBtn.Content).TextColor   = gender == "Homme" ? Colors.White : Navy;
        ((Label)_femaleBtn.Content).TextColor = gender == "Femme" ? Colors.White : Navy;
    }

    private View CheckRow(ref Frame checkFrame, ref bool value, string pre, string link, string post)
    {
        var f = checkFrame = new Frame
        {
            WidthRequest = 20, HeightRequest = 20, CornerRadius = 4,
            BackgroundColor = Colors.White, BorderColor = Border,
            HasShadow = false, Padding = 0
        };
        var localRef = false;
        var tap = new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                localRef = !localRef;
                f.BackgroundColor = localRef ? Teal : Colors.White;
                f.Content = localRef
                    ? new Label { Text = "✓", FontSize = 11, TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    : null;
            })
        };
        f.GestureRecognizers.Add(tap);

        var row = new HorizontalStackLayout { Spacing = 8 };
        row.Children.Add(f);
        var fullText = pre + link + post;
        var textLbl = new Label { FontSize = 13, VerticalOptions = LayoutOptions.Center };
        var span1 = new Span { Text = pre, TextColor = Navy };
        var span2 = new Span { Text = link, TextColor = Teal, FontAttributes = FontAttributes.Bold };
        var span3 = new Span { Text = post, TextColor = Navy };
        textLbl.FormattedText = new FormattedString();
        textLbl.FormattedText.Spans.Add(span1);
        textLbl.FormattedText.Spans.Add(span2);
        textLbl.FormattedText.Spans.Add(span3);
        row.Children.Add(textLbl);
        return row;
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        _errorFrame.IsVisible = false;

        if (string.IsNullOrWhiteSpace(_firstNameEntry.Text) || string.IsNullOrWhiteSpace(_lastNameEntry.Text)
            || string.IsNullOrWhiteSpace(_emailEntry.Text) || string.IsNullOrWhiteSpace(_passwordEntry.Text)
            || string.IsNullOrWhiteSpace(_phoneEntry.Text))
        { ShowError("Veuillez remplir tous les champs obligatoires"); return; }

        _registerBtn.IsEnabled = false; _registerBtn.Text = "Inscription...";
        _spinner.IsRunning = true; _spinner.IsVisible = true;
        try
        {
            var result = await _api.RegisterPatientAsync(new RegisterPatientRequest
            {
                FirstName = _firstNameEntry.Text.Trim(),
                LastName  = _lastNameEntry.Text.Trim(),
                Email     = _emailEntry.Text.Trim(),
                Password  = _passwordEntry.Text,
                DateOfBirth    = _birthDatePicker.Date,
                Gender         = _selectedGender,
                PhoneNumber    = _phoneEntry.Text.Trim(),
                City = null, BloodType = null, Height = null, Weight = null,
                Allergies = null, ChronicDiseases = null, PreviousIllnesses = null,
                CurrentMedications = null, InsuranceProvider = null, InsuranceNumber = null,
                EmergencyContactName = null, EmergencyContactPhone = null
            });

            if (result.Success)
            {
                Preferences.Set("Token", result.Token ?? "");
                Preferences.Set("UserId", result.UserId?.ToString() ?? "");
                Preferences.Set("ProfileId", result.ProfileId?.ToString() ?? "");
                Preferences.Set("Role", "Patient");
                Preferences.Set("FullName", result.FullName ?? "");
                var signalR = ServiceHelper.GetService<SignalRService>();
                await signalR.ConnectAsync();
                Application.Current!.MainPage = new NavigationPage(
                    new PatientDashboardPage(ServiceHelper.GetService<ApiService>(), signalR));
            }
            else ShowError(result.Message ?? "Échec de l'inscription");
        }
        catch (Exception ex) { ShowError($"Erreur: {ex.Message}"); }
        finally { _registerBtn.IsEnabled = true; _registerBtn.Text = "S'inscrire"; _spinner.IsRunning = false; _spinner.IsVisible = false; }
    }

    private void ShowError(string msg) { _errorLabel.Text = msg; _errorFrame.IsVisible = true; }
}
