using HospitalApp.Mobile.Models;
using HospitalApp.Mobile.Services;

namespace HospitalApp.Mobile.Pages;

// =============================================
// ROLE SELECTOR — Landing Screen
// =============================================
public class RoleSelectorPage : ContentPage
{
    private readonly ApiService _api;

    public RoleSelectorPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#1E1B4B");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new VerticalStackLayout { Spacing = 0 };

        // ── Hero ──
        var hero = new VerticalStackLayout
        {
            Padding = new Thickness(32, 80, 32, 50),
            Spacing = 16,
            HorizontalOptions = LayoutOptions.Center
        };
        hero.Children.Add(new Frame
        {
            WidthRequest = 94, HeightRequest = 94, CornerRadius = 28,
            BackgroundColor = Color.FromArgb("#4F46E5"),
            BorderColor = Color.FromArgb("#818CF8"),
            HasShadow = true, Padding = 0, HorizontalOptions = LayoutOptions.Center,
            Content = new Label { Text = "🏥", FontSize = 48, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        });
        hero.Children.Add(new Label
        {
            Text = "MediCare+", FontSize = 36, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center
        });
        hero.Children.Add(new Label
        {
            Text = "Votre santé, notre priorité",
            FontSize = 15, TextColor = Color.FromArgb("#A5B4FC"),
            HorizontalOptions = LayoutOptions.Center
        });
        root.Children.Add(hero);

        // ── Bottom Card ──
        var card = new Frame
        {
            CornerRadius = 40, BackgroundColor = Color.FromArgb("#F0F4FF"),
            Padding = new Thickness(24, 40, 24, 60),
            HasShadow = false, BorderColor = Colors.Transparent
        };
        var cardStack = new VerticalStackLayout { Spacing = 14 };

        cardStack.Children.Add(new Label
        {
            Text = "Choisissez votre espace",
            FontSize = 22, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E1B4B"),
            HorizontalOptions = LayoutOptions.Center
        });
        cardStack.Children.Add(new Label
        {
            Text = "Sélectionnez votre profil pour continuer",
            FontSize = 13, TextColor = Color.FromArgb("#64748B"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, -6, 0, 10)
        });

        cardStack.Children.Add(RoleCard(
            "👤", "Espace Patient",
            "Prenez rendez-vous et suivez\nvos dossiers médicaux",
            "#0EA5E9", "#E0F2FE",
            async () => await Navigation.PushAsync(new PatientLoginPage(_api))
        ));
        cardStack.Children.Add(RoleCard(
            "🩺", "Espace Médecin",
            "Gérez vos consultations\net suivez vos patients",
            "#4F46E5", "#EEF2FF",
            async () => await Navigation.PushAsync(new DoctorLoginPage(_api))
        ));
        cardStack.Children.Add(RoleCard(
            "🔑", "Administration",
            "Tableau de bord et gestion\nde l'établissement",
            "#7C3AED", "#F5F3FF",
            async () => await Navigation.PushAsync(new AdminLoginPage(_api))
        ));

        card.Content = cardStack;
        root.Children.Add(card);
        Content = new ScrollView { Content = root };
    }

    private static View RoleCard(string icon, string title, string subtitle, string accentHex, string bgHex, Func<Task> onTap)
    {
        var accent = Color.FromArgb(accentHex);

        var iconFr = new Frame
        {
            WidthRequest = 58, HeightRequest = 58, CornerRadius = 18,
            BackgroundColor = accent, BorderColor = Colors.Transparent,
            HasShadow = false, Padding = 0,
            Content = new Label { Text = icon, FontSize = 28, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        var textStack = new VerticalStackLayout
        {
            Spacing = 4, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") },
                new Label { Text = subtitle, FontSize = 12, TextColor = Color.FromArgb("#64748B"), LineBreakMode = LineBreakMode.WordWrap }
            }
        };

        var arrow = new Label
        {
            Text = "›", FontSize = 34, TextColor = accent,
            VerticalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 14
        };
        grid.Children.Add(iconFr);
        Grid.SetColumn(textStack, 1); grid.Children.Add(textStack);
        Grid.SetColumn(arrow, 2);    grid.Children.Add(arrow);

        var card = new Frame
        {
            BackgroundColor = Color.FromArgb(bgHex), CornerRadius = 22,
            Padding = new Thickness(18), HasShadow = false,
            BorderColor = accent.WithAlpha(0.3f),
            Content = grid
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await onTap()) });
        return card;
    }
}

// =============================================
// PATIENT LOGIN  –  same pattern as DoctorLoginPage
// =============================================
public class PatientLoginPage : ContentPage
{
    private readonly ApiService _api;
    private Entry _email = new(), _password = new();
    private Label _errLabel = new();
    private Frame _errFrame = new(), _checkFrame = new();
    private Button _btn = new();
    private ActivityIndicator _spinner = new();
    private bool _rememberMe;

    public PatientLoginPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#126B82");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new VerticalStackLayout { Spacing = 0 };

        // ── HEADER (same structure as DoctorLoginPage) ────────────────
        root.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#126B82"),
            Padding = new Thickness(28, 64, 28, 60),
            Children =
            {
                new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 14,
                    Children =
                    {
                        new Frame
                        {
                            WidthRequest = 80, HeightRequest = 80, CornerRadius = 24,
                            BackgroundColor = Color.FromArgb("#0D4D5E"),
                            BorderColor = Colors.White.WithAlpha(0.3f),
                            HasShadow = false, Padding = 0, HorizontalOptions = LayoutOptions.Center,
                            Content = new Label { Text = "👤", FontSize = 40,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions   = LayoutOptions.Center }
                        },
                        new Label
                        {
                            Text = "Espace Patient",
                            FontSize = 28, FontAttributes = FontAttributes.Bold,
                            TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "Prenez rendez-vous en ligne\net suivez votre santé",
                            FontSize = 13, TextColor = Colors.White.WithAlpha(0.75f),
                            HorizontalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                }
            }
        });

        root.Children.Add(BuildForm());
        Content = new ScrollView { Content = root };
    }

    private View BuildForm()
    {
        var accent = Color.FromArgb("#126B82");
        var formStack = new VerticalStackLayout { Spacing = 18 };

        formStack.Children.Add(new Label
        {
            Text = "Connexion Patient",
            FontSize = 22, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F2540")
        });
        formStack.Children.Add(new Label
        {
            Text = "Accès à votre espace santé personnel",
            FontSize = 13, TextColor = Color.FromArgb("#64748B"),
            Margin = new Thickness(0, -10, 0, 4)
        });

        _email    = new Entry { Placeholder = "votre@email.com", Keyboard = Keyboard.Email,
            BackgroundColor = Colors.Transparent, HeightRequest = 52,
            TextColor = Color.FromArgb("#0F2540"), PlaceholderColor = Color.FromArgb("#9CA3AF"), FontSize = 14 };
        _password = new Entry { Placeholder = "Mot de passe", IsPassword = true,
            BackgroundColor = Colors.Transparent, HeightRequest = 52,
            TextColor = Color.FromArgb("#0F2540"), PlaceholderColor = Color.FromArgb("#9CA3AF"), FontSize = 14 };

        formStack.Children.Add(FieldBox("✉️", "Adresse e-mail", _email));
        var eye = new Label { Text = "👁", FontSize = 18, VerticalOptions = LayoutOptions.Center };
        eye.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(() => _password.IsPassword = !_password.IsPassword) });
        formStack.Children.Add(FieldBox("🔒", "Mot de passe", _password, eye));

        // Remember me + Forgot password
        var optRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        var remRow = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        _checkFrame = new Frame
        {
            WidthRequest = 20, HeightRequest = 20, CornerRadius = 5,
            BackgroundColor = Colors.White, BorderColor = Color.FromArgb("#E2E8F0"),
            HasShadow = false, Padding = 0
        };
        var checkTap = new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                _rememberMe = !_rememberMe;
                _checkFrame.BackgroundColor = _rememberMe ? accent : Colors.White;
                _checkFrame.BorderColor     = _rememberMe ? accent : Color.FromArgb("#E2E8F0");
                _checkFrame.Content = _rememberMe
                    ? new Label { Text = "✓", FontSize = 11, TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions   = LayoutOptions.Center }
                    : null;
            })
        };
        _checkFrame.GestureRecognizers.Add(checkTap);
        var remLbl = new Label { Text = "Se souvenir de moi", FontSize = 13,
            TextColor = Color.FromArgb("#64748B"), VerticalOptions = LayoutOptions.Center };
        remLbl.GestureRecognizers.Add(checkTap);
        remRow.Children.Add(_checkFrame);
        remRow.Children.Add(remLbl);
        optRow.Children.Add(remRow);
        var forgotLbl = new Label { Text = "Mot de passe oublié ?", FontSize = 13,
            TextColor = accent, FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(forgotLbl, 1); optRow.Children.Add(forgotLbl);
        formStack.Children.Add(optRow);

        _errLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
        _errFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#FEE2E2"), BorderColor = Color.FromArgb("#FECACA"),
            CornerRadius = 10, Padding = 12, IsVisible = false, HasShadow = false, Content = _errLabel
        };
        formStack.Children.Add(_errFrame);

        _btn = new Button
        {
            Text = "Se connecter",
            BackgroundColor = accent, TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold, FontSize = 16,
            CornerRadius = 14, HeightRequest = 54,
            Margin = new Thickness(0, 4)
        };
        _btn.Clicked += OnLogin;
        formStack.Children.Add(_btn);

        _spinner = new ActivityIndicator
        {
            Color = accent, IsRunning = false, IsVisible = false,
            HorizontalOptions = LayoutOptions.Center
        };
        formStack.Children.Add(_spinner);

        // Register link
        var regRow = new HorizontalStackLayout
            { HorizontalOptions = LayoutOptions.Center, Spacing = 4 };
        regRow.Children.Add(new Label { Text = "Pas encore de compte ?", FontSize = 14,
            TextColor = Color.FromArgb("#64748B") });
        var regLink = new Label { Text = "S'inscrire gratuitement", FontSize = 14,
            TextColor = accent, FontAttributes = FontAttributes.Bold };
        regLink.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await Navigation.PushAsync(new RegisterPage(_api))) });
        regRow.Children.Add(regLink);
        formStack.Children.Add(regRow);

        var back = new Label
        {
            Text = "‹ Changer d'espace", TextColor = Color.FromArgb("#94A3B8"),
            FontSize = 13, HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 8)
        };
        back.GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await Navigation.PopAsync()) });
        formStack.Children.Add(back);

        return new Frame
        {
            CornerRadius = 36, BackgroundColor = Colors.White,
            Padding = new Thickness(28, 36, 28, 50),
            HasShadow = false, BorderColor = Colors.Transparent,
            Margin = new Thickness(0, -30, 0, 0),
            Content = formStack
        };
    }

    private static View FieldBox(string emoji, string label, Entry entry, View? trailing = null)
    {
        var cols = trailing != null
            ? new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
            : new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) };
        var grid = new Grid { ColumnDefinitions = cols, Padding = new Thickness(14, 0) };
        grid.Children.Add(new Label { Text = emoji, FontSize = 18, VerticalOptions = LayoutOptions.Center });
        Grid.SetColumn(entry, 1); grid.Children.Add(entry);
        if (trailing != null) { Grid.SetColumn(trailing, 2); grid.Children.Add(trailing); }
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") },
                new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = 0, HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = grid }
            }
        };
    }

    private async void OnLogin(object? s, EventArgs e)
    {
        _errFrame.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_email.Text) || string.IsNullOrWhiteSpace(_password.Text))
        { _errLabel.Text = "Veuillez remplir tous les champs."; _errFrame.IsVisible = true; return; }

        _btn.IsEnabled = false; _btn.Text = "Connexion en cours...";
        _spinner.IsRunning = true; _spinner.IsVisible = true;
        try
        {
            var result = await _api.LoginAsync(_email.Text.Trim(), _password.Text);
            if (!result.Success) { _errLabel.Text = result.Message ?? "Échec de connexion"; _errFrame.IsVisible = true; return; }
            if (result.Role != "Patient") { _errLabel.Text = "Ce compte n'est pas un compte Patient."; _errFrame.IsVisible = true; return; }

            Preferences.Set("Token",           result.Token           ?? "");
            Preferences.Set("UserId",          result.UserId?.ToString()    ?? "");
            Preferences.Set("ProfileId",       result.ProfileId?.ToString() ?? "");
            Preferences.Set("Role",            result.Role            ?? "");
            Preferences.Set("FullName",        result.FullName        ?? "");
            Preferences.Set("ProfileImageUrl", result.ProfileImageUrl ?? "");

            var signalR = ServiceHelper.GetService<SignalRService>();
            await signalR.ConnectAsync();
            Application.Current!.MainPage = new NavigationPage(
                new PatientDashboardPage(ServiceHelper.GetService<ApiService>(), signalR));
        }
        catch (Exception ex) { _errLabel.Text = $"Erreur de connexion : {ex.Message}"; _errFrame.IsVisible = true; }
        finally { _btn.IsEnabled = true; _btn.Text = "Se connecter"; _spinner.IsRunning = false; _spinner.IsVisible = false; }
    }
}

// =============================================
// DOCTOR LOGIN
// =============================================
public class DoctorLoginPage : ContentPage
{
    private readonly ApiService _api;
    private Entry _email = new(), _password = new();
    private Label _errLabel = new();
    private Frame _errFrame = new();
    private Button _btn = new();
    private ActivityIndicator _spinner = new();

    public DoctorLoginPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#3730A3");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new VerticalStackLayout { Spacing = 0 };

        root.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#3730A3"),
            Padding = new Thickness(28, 64, 28, 60),
            Children =
            {
                new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 14,
                    Children =
                    {
                        new Frame
                        {
                            WidthRequest = 80, HeightRequest = 80, CornerRadius = 24,
                            BackgroundColor = Color.FromArgb("#4F46E5"),
                            BorderColor = Colors.White.WithAlpha(0.3f),
                            HasShadow = false, Padding = 0, HorizontalOptions = LayoutOptions.Center,
                            Content = new Label { Text = "🩺", FontSize = 40, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                        },
                        new Label { Text = "Espace Médecin", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Gérez vos consultations\net suivez vos patients", FontSize = 13, TextColor = Colors.White.WithAlpha(0.75f), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }
                    }
                }
            }
        });

        root.Children.Add(BuildForm());
        Content = new ScrollView { Content = root };
    }

    private View BuildForm()
    {
        var accent = Color.FromArgb("#4F46E5");
        var formStack = new VerticalStackLayout { Spacing = 18 };

        formStack.Children.Add(new Label { Text = "Connexion Médecin", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") });
        formStack.Children.Add(new Label { Text = "Accès réservé au personnel médical", FontSize = 13, TextColor = Color.FromArgb("#64748B"), Margin = new Thickness(0, -10, 0, 4) });

        _email = new Entry { Placeholder = "votre@email.com", Keyboard = Keyboard.Email, BackgroundColor = Colors.Transparent, HeightRequest = 52, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF"), FontSize = 14 };
        _password = new Entry { Placeholder = "Mot de passe", IsPassword = true, BackgroundColor = Colors.Transparent, HeightRequest = 52, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF"), FontSize = 14 };

        formStack.Children.Add(FieldBox("✉️", "Adresse email", _email));
        var eye = new Label { Text = "👁", FontSize = 18, VerticalOptions = LayoutOptions.Center };
        eye.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _password.IsPassword = !_password.IsPassword) });
        formStack.Children.Add(FieldBox("🔒", "Mot de passe", _password, eye));

        _errLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
        _errFrame = new Frame { BackgroundColor = Color.FromArgb("#FEE2E2"), BorderColor = Color.FromArgb("#FECACA"), CornerRadius = 10, Padding = 12, IsVisible = false, Content = _errLabel };
        formStack.Children.Add(_errFrame);

        _btn = new Button { Text = "Se connecter", BackgroundColor = accent, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16, CornerRadius = 14, HeightRequest = 54, Margin = new Thickness(0, 4) };
        _btn.Clicked += OnLogin;
        formStack.Children.Add(_btn);

        _spinner = new ActivityIndicator { Color = accent, IsRunning = false, IsVisible = false, HorizontalOptions = LayoutOptions.Center };
        formStack.Children.Add(_spinner);

        // Register link
        var regRow = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center, Spacing = 4, Margin = new Thickness(0, 4),
            Children =
            {
                new Label { Text = "Pas encore inscrit ?", FontSize = 13, TextColor = Color.FromArgb("#64748B"), VerticalOptions = LayoutOptions.Center },
                new Label { Text = "S'inscrire gratuitement", FontSize = 13, TextColor = Color.FromArgb("#4F46E5"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }
            }
        };
        ((View)regRow.Children[1]).GestureRecognizers.Add(new TapGestureRecognizer
            { Command = new Command(async () => await Navigation.PushAsync(new DoctorRegisterPage(_api))) });
        formStack.Children.Add(regRow);

        var back = new Label { Text = "‹ Changer d'espace", TextColor = Color.FromArgb("#94A3B8"), FontSize = 13, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 4) };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        formStack.Children.Add(back);

        return new Frame { CornerRadius = 36, BackgroundColor = Colors.White, Padding = new Thickness(28, 36, 28, 50), HasShadow = false, BorderColor = Colors.Transparent, Margin = new Thickness(0, -30, 0, 0), Content = formStack };
    }

    private async void OnLogin(object? s, EventArgs e)
    {
        _errFrame.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_email.Text) || string.IsNullOrWhiteSpace(_password.Text))
        { _errLabel.Text = "Veuillez remplir tous les champs"; _errFrame.IsVisible = true; return; }

        _btn.IsEnabled = false; _btn.Text = "Connexion..."; _spinner.IsRunning = true; _spinner.IsVisible = true;
        try
        {
            var result = await _api.LoginAsync(_email.Text.Trim(), _password.Text);
            if (!result.Success) { _errLabel.Text = result.Message ?? "Échec de connexion"; _errFrame.IsVisible = true; return; }
            if (result.Role != "Doctor") { _errLabel.Text = "Ce compte n'est pas un compte Médecin"; _errFrame.IsVisible = true; return; }

            Preferences.Set("Token", result.Token ?? ""); Preferences.Set("UserId", result.UserId?.ToString() ?? "");
            Preferences.Set("ProfileId", result.ProfileId?.ToString() ?? ""); Preferences.Set("Role", result.Role ?? "");
            Preferences.Set("FullName", result.FullName ?? ""); Preferences.Set("ProfileImageUrl", result.ProfileImageUrl ?? "");

            var signalR = ServiceHelper.GetService<SignalRService>();
            await signalR.ConnectAsync();

            var doctorId = result.ProfileId ?? Guid.Empty;
            var detail = await ServiceHelper.GetService<ApiService>().GetDoctorByIdAsync(doctorId);
            var nextPage = detail?.IsProfileComplete == false
                ? (Page)new DoctorProfileSetupPage(ServiceHelper.GetService<ApiService>())
                : new DoctorDashboardPage(ServiceHelper.GetService<ApiService>(), signalR);
            Application.Current!.MainPage = new NavigationPage(nextPage);
        }
        catch (Exception ex) { _errLabel.Text = $"Erreur: {ex.Message}"; _errFrame.IsVisible = true; }
        finally { _btn.IsEnabled = true; _btn.Text = "Se connecter"; _spinner.IsRunning = false; _spinner.IsVisible = false; }
    }

    private static View FieldBox(string emoji, string label, Entry entry, View? trailing = null)
    {
        var cols = trailing != null
            ? new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
            : new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) };
        var grid = new Grid { ColumnDefinitions = cols, Padding = new Thickness(14, 0) };
        grid.Children.Add(new Label { Text = emoji, FontSize = 18, VerticalOptions = LayoutOptions.Center });
        Grid.SetColumn(entry, 1); grid.Children.Add(entry);
        if (trailing != null) { Grid.SetColumn(trailing, 2); grid.Children.Add(trailing); }
        return new VerticalStackLayout { Spacing = 6, Children = { new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") }, new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = 0, HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = grid } } };
    }
}

// =============================================
// ADMIN LOGIN
// =============================================
public class AdminLoginPage : ContentPage
{
    private readonly ApiService _api;
    private Entry _email = new(), _password = new();
    private Label _errLabel = new();
    private Frame _errFrame = new();
    private Button _btn = new();
    private ActivityIndicator _spinner = new();

    public AdminLoginPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#4C1D95");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new VerticalStackLayout { Spacing = 0 };

        root.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#4C1D95"),
            Padding = new Thickness(28, 64, 28, 60),
            Children =
            {
                new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 14,
                    Children =
                    {
                        new Frame
                        {
                            WidthRequest = 80, HeightRequest = 80, CornerRadius = 24,
                            BackgroundColor = Color.FromArgb("#7C3AED"),
                            BorderColor = Colors.White.WithAlpha(0.3f),
                            HasShadow = false, Padding = 0, HorizontalOptions = LayoutOptions.Center,
                            Content = new Label { Text = "🔑", FontSize = 40, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                        },
                        new Label { Text = "Administration", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Accès réservé aux administrateurs\nde l'établissement", FontSize = 13, TextColor = Colors.White.WithAlpha(0.75f), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }
                    }
                }
            }
        });

        root.Children.Add(BuildForm());
        Content = new ScrollView { Content = root };
    }

    private View BuildForm()
    {
        var accent = Color.FromArgb("#7C3AED");
        var formStack = new VerticalStackLayout { Spacing = 18 };

        formStack.Children.Add(new Label { Text = "Connexion Admin", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") });
        formStack.Children.Add(new Label { Text = "Accès sécurisé au panneau d'administration", FontSize = 13, TextColor = Color.FromArgb("#64748B"), Margin = new Thickness(0, -10, 0, 4) });

        _email = new Entry { Placeholder = "admin@email.com", Keyboard = Keyboard.Email, BackgroundColor = Colors.Transparent, HeightRequest = 52, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF"), FontSize = 14 };
        _password = new Entry { Placeholder = "Mot de passe", IsPassword = true, BackgroundColor = Colors.Transparent, HeightRequest = 52, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF"), FontSize = 14 };

        formStack.Children.Add(FieldBox("✉️", "Adresse email", _email));
        var eye = new Label { Text = "👁", FontSize = 18, VerticalOptions = LayoutOptions.Center };
        eye.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _password.IsPassword = !_password.IsPassword) });
        formStack.Children.Add(FieldBox("🔒", "Mot de passe", _password, eye));

        _errLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
        _errFrame = new Frame { BackgroundColor = Color.FromArgb("#FEE2E2"), BorderColor = Color.FromArgb("#FECACA"), CornerRadius = 10, Padding = 12, IsVisible = false, Content = _errLabel };
        formStack.Children.Add(_errFrame);

        _btn = new Button { Text = "Accéder", BackgroundColor = accent, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16, CornerRadius = 14, HeightRequest = 54, Margin = new Thickness(0, 4) };
        _btn.Clicked += OnLogin;
        formStack.Children.Add(_btn);

        _spinner = new ActivityIndicator { Color = accent, IsRunning = false, IsVisible = false, HorizontalOptions = LayoutOptions.Center };
        formStack.Children.Add(_spinner);

        var back = new Label { Text = "‹ Changer d'espace", TextColor = Color.FromArgb("#94A3B8"), FontSize = 13, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8) };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        formStack.Children.Add(back);

        return new Frame { CornerRadius = 36, BackgroundColor = Colors.White, Padding = new Thickness(28, 36, 28, 50), HasShadow = false, BorderColor = Colors.Transparent, Margin = new Thickness(0, -30, 0, 0), Content = formStack };
    }

    private async void OnLogin(object? s, EventArgs e)
    {
        _errFrame.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_email.Text) || string.IsNullOrWhiteSpace(_password.Text))
        { _errLabel.Text = "Veuillez remplir tous les champs"; _errFrame.IsVisible = true; return; }

        _btn.IsEnabled = false; _btn.Text = "Vérification..."; _spinner.IsRunning = true; _spinner.IsVisible = true;
        try
        {
            var result = await _api.LoginAsync(_email.Text.Trim(), _password.Text);
            if (!result.Success) { _errLabel.Text = result.Message ?? "Échec de connexion"; _errFrame.IsVisible = true; return; }
            if (result.Role != "Admin") { _errLabel.Text = "Ce compte n'est pas un compte Administrateur"; _errFrame.IsVisible = true; return; }

            Preferences.Set("Token", result.Token ?? ""); Preferences.Set("UserId", result.UserId?.ToString() ?? "");
            Preferences.Set("ProfileId", result.ProfileId?.ToString() ?? ""); Preferences.Set("Role", result.Role ?? "");
            Preferences.Set("FullName", result.FullName ?? ""); Preferences.Set("ProfileImageUrl", result.ProfileImageUrl ?? "");

            Application.Current!.MainPage = new NavigationPage(new AdminDashboardPage(ServiceHelper.GetService<ApiService>()));
        }
        catch (Exception ex) { _errLabel.Text = $"Erreur: {ex.Message}"; _errFrame.IsVisible = true; }
        finally { _btn.IsEnabled = true; _btn.Text = "Accéder"; _spinner.IsRunning = false; _spinner.IsVisible = false; }
    }

    private static View FieldBox(string emoji, string label, Entry entry, View? trailing = null)
    {
        var cols = trailing != null
            ? new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
            : new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) };
        var grid = new Grid { ColumnDefinitions = cols, Padding = new Thickness(14, 0) };
        grid.Children.Add(new Label { Text = emoji, FontSize = 18, VerticalOptions = LayoutOptions.Center });
        Grid.SetColumn(entry, 1); grid.Children.Add(entry);
        if (trailing != null) { Grid.SetColumn(trailing, 2); grid.Children.Add(trailing); }
        return new VerticalStackLayout { Spacing = 6, Children = { new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") }, new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = 0, HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = grid } } };
    }
}

// =============================================
// DOCTOR REGISTRATION — Step 1/3 : Identité
// =============================================
public class DoctorRegisterPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Entry _firstName = MakeReg("Prénom *");
    private readonly Entry _lastName  = MakeReg("Nom *");
    private readonly Entry _city      = MakeReg("Ville *");
    private readonly Entry _email     = MakeReg("@ Email *", Keyboard.Email);
    private readonly Picker _specialtyPicker = new Picker { Title = "  Sélectionner votre spécialité *", FontSize = 14, TextColor = Color.FromArgb("#1E1B4B"), BackgroundColor = Colors.Transparent };
    private readonly List<SpecialtyDto> _specialties = new();
    private readonly CheckBox _termsCheck   = new CheckBox { Color = Color.FromArgb("#4F46E5") };
    private readonly CheckBox _privacyCheck = new CheckBox { Color = Color.FromArgb("#4F46E5") };
    private readonly Label _errLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
    private readonly Frame _errFrame;
    private readonly Button _btn;

    public DoctorRegisterPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#3730A3");
        _errFrame = new Frame { BackgroundColor = Color.FromArgb("#FEE2E2"), BorderColor = Color.FromArgb("#FECACA"), CornerRadius = 10, Padding = 12, IsVisible = false, Content = _errLabel };
        _btn = new Button { Text = "Suivant  →", BackgroundColor = Color.FromArgb("#4F46E5"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16, CornerRadius = 14, HeightRequest = 54 };
        _btn.Clicked += OnNext;

        var root = new VerticalStackLayout { Spacing = 0 };
        root.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#3730A3"), Padding = new Thickness(28, 64, 28, 50),
            Children =
            {
                new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 12,
                    Children =
                    {
                        new Frame { WidthRequest = 80, HeightRequest = 80, CornerRadius = 24, BackgroundColor = Color.FromArgb("#4F46E5"), BorderColor = Colors.White.WithAlpha(0.3f), HasShadow = false, Padding = 0, HorizontalOptions = LayoutOptions.Center, Content = new Label { Text = "🩺", FontSize = 40, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } },
                        new Label { Text = "Inscription Médecin", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                        new Frame { BackgroundColor = Colors.White.WithAlpha(0.18f), CornerRadius = 8, Padding = new Thickness(12, 4), HasShadow = false, BorderColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Center, Content = new Label { Text = "GRATUIT", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White } }
                    }
                }
            }
        });

        var formStack = new VerticalStackLayout { Spacing = 14 };
        formStack.Children.Add(new Label { Text = "Créer votre espace médecin", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") });
        formStack.Children.Add(new Label { Text = "Champs obligatoires *", FontSize = 12, TextColor = Color.FromArgb("#94A3B8"), Margin = new Thickness(0, -6, 0, 0) });
        formStack.Children.Add(TwoColReg(_firstName, _lastName));
        formStack.Children.Add(EntryBoxReg(_city));
        formStack.Children.Add(EntryBoxReg(_email));
        formStack.Children.Add(new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = new Thickness(14, 4), HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = _specialtyPicker });
        formStack.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { _termsCheck, new Label { Text = "J'accepte les Conditions Générales d'utilisation.", FontSize = 12, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap } } });
        formStack.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { _privacyCheck, new Label { Text = "J'accepte la politique de confidentialité.", FontSize = 12, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center } } });
        formStack.Children.Add(new Frame { BackgroundColor = Color.FromArgb("#F0FDFA"), BorderColor = Color.FromArgb("#99F6E4"), CornerRadius = 10, Padding = 12, HasShadow = false, Content = new Label { Text = "Conformément à la loi 09-08, vous disposez d'un droit d'accès, de rectification et d'opposition au traitement de vos données personnelles. Ce traitement a été autorisé par la CNDP sous le N° A-S-908/2023.", FontSize = 11, TextColor = Color.FromArgb("#0F766E"), LineBreakMode = LineBreakMode.WordWrap } });
        formStack.Children.Add(_errFrame);
        formStack.Children.Add(_btn);

        var loginRow = new HorizontalStackLayout { HorizontalOptions = LayoutOptions.Center, Spacing = 4, Children = { new Label { Text = "Déjà inscrit ?", FontSize = 13, TextColor = Color.FromArgb("#64748B"), VerticalOptions = LayoutOptions.Center }, new Label { Text = "Se connecter", FontSize = 13, TextColor = Color.FromArgb("#4F46E5"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center } } };
        ((View)loginRow.Children[1]).GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        formStack.Children.Add(loginRow);

        root.Children.Add(new Frame { CornerRadius = 36, BackgroundColor = Colors.White, Padding = new Thickness(24, 36, 24, 50), HasShadow = false, BorderColor = Colors.Transparent, Margin = new Thickness(0, -30, 0, 0), Content = formStack });
        Content = new ScrollView { Content = root };
        Loaded += async (_, _) => await LoadSpecialtiesAsync();
    }

    private async Task LoadSpecialtiesAsync()
    {
        var list = await _api.GetSpecialtiesAsync();
        _specialties.Clear(); _specialties.AddRange(list);
        _specialtyPicker.ItemsSource = list.Select(s => s.Name).ToList();
    }

    private async void OnNext(object? s, EventArgs e)
    {
        _errFrame.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_firstName.Text) || string.IsNullOrWhiteSpace(_lastName.Text) ||
            string.IsNullOrWhiteSpace(_city.Text) || string.IsNullOrWhiteSpace(_email.Text))
        { _errLabel.Text = "Veuillez remplir tous les champs obligatoires."; _errFrame.IsVisible = true; return; }
        if (_specialtyPicker.SelectedIndex < 0)
        { _errLabel.Text = "Veuillez sélectionner votre spécialité."; _errFrame.IsVisible = true; return; }
        if (!_termsCheck.IsChecked || !_privacyCheck.IsChecked)
        { _errLabel.Text = "Veuillez accepter les conditions générales et la politique de confidentialité."; _errFrame.IsVisible = true; return; }

        var data = new DoctorRegistrationData
        {
            FirstName = _firstName.Text.Trim(), LastName = _lastName.Text.Trim(),
            Email = _email.Text.Trim(), City = _city.Text.Trim(),
            SpecialtyId = _specialties[_specialtyPicker.SelectedIndex].Id,
            SpecialtyName = _specialties[_specialtyPicker.SelectedIndex].Name
        };
        await Navigation.PushAsync(new DoctorCabinetInfoPage(_api, data));
    }

    private static Entry MakeReg(string placeholder, Keyboard? keyboard = null) => new Entry { Placeholder = placeholder, BackgroundColor = Colors.Transparent, FontSize = 14, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF"), Keyboard = keyboard ?? Keyboard.Default };
    private static Frame EntryBoxReg(Entry entry) => new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = new Thickness(14, 0), HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = entry };
    private static Grid TwoColReg(Entry left, Entry right)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(new GridLength(10)), new(GridLength.Star) } };
        g.Children.Add(new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = new Thickness(14, 0), HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = left });
        var rf = new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = new Thickness(14, 0), HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = right };
        Grid.SetColumn(rf, 2); g.Children.Add(rf);
        return g;
    }
}

// =============================================
// DOCTOR REGISTRATION — Step 2/3 : Cabinet
// =============================================
public class DoctorCabinetInfoPage : ContentPage
{
    private readonly ApiService _api;
    private readonly DoctorRegistrationData _data;
    private readonly Entry _address = MakeE("Adresse du cabinet *");
    private readonly Entry _postal  = MakeE("Code postal *", Keyboard.Numeric);
    private readonly Entry _phone   = MakeE("Numéro de téléphone *", Keyboard.Telephone);
    private readonly CheckBox _cbArabe    = new CheckBox { Color = Color.FromArgb("#0EA5E9") };
    private readonly CheckBox _cbFrancais = new CheckBox { Color = Color.FromArgb("#0EA5E9") };
    private readonly CheckBox _cbAnglais  = new CheckBox { Color = Color.FromArgb("#0EA5E9") };
    private readonly CheckBox _cbEspagnol = new CheckBox { Color = Color.FromArgb("#0EA5E9") };
    private readonly CheckBox _cbDarija   = new CheckBox { Color = Color.FromArgb("#0EA5E9") };
    private readonly CheckBox _cbAmazigh  = new CheckBox { Color = Color.FromArgb("#0EA5E9") };
    private readonly Label _errLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
    private readonly Frame _errFrame;

    public DoctorCabinetInfoPage(ApiService api, DoctorRegistrationData data)
    {
        _api = api; _data = data;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F4FF");
        _errFrame = new Frame { BackgroundColor = Color.FromArgb("#FEE2E2"), BorderColor = Color.FromArgb("#FECACA"), CornerRadius = 10, Padding = 12, IsVisible = false, Content = _errLabel };

        var btn = new Button { Text = "Suivant  →", BackgroundColor = Color.FromArgb("#0EA5E9"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16, CornerRadius = 14, HeightRequest = 54 };
        btn.Clicked += OnNext;

        var root = new VerticalStackLayout { Spacing = 0 };

        root.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#0369A1"), Padding = new Thickness(24, 56, 24, 32),
            Children =
            {
                new VerticalStackLayout { Spacing = 8, Children =
                {
                    new Label { Text = "📍 Informations du cabinet", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                    new Label { Text = "Prêt pour un gain de temps et plus de visibilité !", FontSize = 13, TextColor = Colors.White.WithAlpha(0.85f), LineBreakMode = LineBreakMode.WordWrap }
                }}
            }
        });

        root.Children.Add(BuildProgress(1));

        var form = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(24, 28, 24, 40) };

        form.Children.Add(new Label { Text = "Spécialité sélectionnée", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") });
        form.Children.Add(new Frame { BackgroundColor = Color.FromArgb("#E0F2FE"), BorderColor = Color.FromArgb("#0EA5E9"), CornerRadius = 20, Padding = new Thickness(14, 6), HasShadow = false, HorizontalOptions = LayoutOptions.Start, Content = new Label { Text = $"  {data.SpecialtyName}", FontSize = 13, TextColor = Color.FromArgb("#0369A1"), FontAttributes = FontAttributes.Bold } });

        form.Children.Add(LabeledField("Adresse du cabinet *", _address));
        form.Children.Add(TwoColE(_postal, _phone));

        form.Children.Add(new Label { Text = "Langues parlées", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151"), Margin = new Thickness(0, 4, 0, 0) });
        var langGrid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }, RowDefinitions = new RowDefinitionCollection { new(), new(), new() }, RowSpacing = 10, ColumnSpacing = 8 };
        AddLang(langGrid, _cbArabe,    "🇲🇦 Arabe",    0, 0);
        AddLang(langGrid, _cbFrancais, "🇫🇷 Français",  0, 1);
        AddLang(langGrid, _cbAnglais,  "🇬🇧 Anglais",   1, 0);
        AddLang(langGrid, _cbEspagnol, "🇪🇸 Espagnol",  1, 1);
        AddLang(langGrid, _cbDarija,   "🗣️ Darija",     2, 0);
        AddLang(langGrid, _cbAmazigh,  "🏔️ Amazigh",    2, 1);
        form.Children.Add(langGrid);

        form.Children.Add(_errFrame);
        form.Children.Add(btn);

        root.Children.Add(new Frame { CornerRadius = 28, BackgroundColor = Colors.White, HasShadow = false, BorderColor = Colors.Transparent, Margin = new Thickness(0, -20, 0, 0), Padding = 0, Content = form });
        Content = new ScrollView { Content = root };
    }

    private void OnNext(object? s, EventArgs e)
    {
        _errFrame.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_address.Text) || string.IsNullOrWhiteSpace(_postal.Text) || string.IsNullOrWhiteSpace(_phone.Text))
        { _errLabel.Text = "Veuillez remplir tous les champs obligatoires."; _errFrame.IsVisible = true; return; }

        _data.Address = _address.Text.Trim();
        _data.PostalCode = _postal.Text.Trim();
        _data.PhoneNumber = _phone.Text.Trim();
        _data.Languages = string.Join(";", new[] {
            _cbArabe.IsChecked    ? "Arabe"    : null,
            _cbFrancais.IsChecked ? "Français" : null,
            _cbAnglais.IsChecked  ? "Anglais"  : null,
            _cbEspagnol.IsChecked ? "Espagnol" : null,
            _cbDarija.IsChecked   ? "Darija"   : null,
            _cbAmazigh.IsChecked  ? "Amazigh"  : null
        }.Where(l => l != null));

        Navigation.PushAsync(new DoctorPasswordPage(_api, _data));
    }

    private static void AddLang(Grid g, CheckBox cb, string label, int row, int col)
    {
        var cell = new HorizontalStackLayout { Spacing = 6, Children = { cb, new Label { Text = label, FontSize = 13, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#374151") } } };
        Grid.SetRow(cell, row); Grid.SetColumn(cell, col);
        g.Children.Add(cell);
    }

    private static View LabeledField(string label, Entry entry) => new VerticalStackLayout { Spacing = 6, Children = { new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") }, new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 12, Padding = new Thickness(14, 0), HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = entry } } };

    private static Grid TwoColE(Entry left, Entry right)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(new GridLength(10)), new(GridLength.Star) } };
        g.Children.Add(new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 12, Padding = new Thickness(14, 0), HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = left });
        var rf = new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 12, Padding = new Thickness(14, 0), HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = right };
        Grid.SetColumn(rf, 2); g.Children.Add(rf);
        return g;
    }

    internal static View BuildProgress(int activeStep)
    {
        var teal  = Color.FromArgb("#0EA5E9");
        var gray  = Color.FromArgb("#CBD5E1");
        var c1bg  = teal;
        var c2bg  = activeStep >= 2 ? teal : gray;
        var lineBg = activeStep >= 2 ? teal : gray;

        Frame Circle(string n, Color bg) => new Frame { WidthRequest = 34, HeightRequest = 34, CornerRadius = 17, BackgroundColor = bg, BorderColor = Colors.Transparent, HasShadow = false, Padding = 0, Content = new Label { Text = n, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };

        var g = new Grid { BackgroundColor = Colors.White, Padding = new Thickness(48, 16), ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        var line = new BoxView { HeightRequest = 3, BackgroundColor = lineBg, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Center };
        var c2 = Circle("2", c2bg);
        g.Children.Add(Circle("1", c1bg));
        Grid.SetColumn(line, 1); g.Children.Add(line);
        Grid.SetColumn(c2, 2); g.Children.Add(c2);
        return g;
    }

    private static Entry MakeE(string placeholder, Keyboard? keyboard = null) => new Entry { Placeholder = placeholder, BackgroundColor = Colors.Transparent, FontSize = 14, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF"), Keyboard = keyboard ?? Keyboard.Default };
}

// =============================================
// DOCTOR REGISTRATION — Step 3/3 : Password
// =============================================
public class DoctorPasswordPage : ContentPage
{
    private readonly ApiService _api;
    private readonly DoctorRegistrationData _data;
    private readonly Entry _password = new Entry { Placeholder = "Mot de passe *", IsPassword = true, BackgroundColor = Colors.Transparent, FontSize = 14, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF") };
    private readonly Entry _confirm  = new Entry { Placeholder = "Confirmation du mot de passe *", IsPassword = true, BackgroundColor = Colors.Transparent, FontSize = 14, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF") };
    private readonly Label _errLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
    private readonly Frame _errFrame;
    private readonly Button _btnStart;
    private readonly ActivityIndicator _spinner = new ActivityIndicator { Color = Color.FromArgb("#0EA5E9"), IsRunning = false, IsVisible = false, HorizontalOptions = LayoutOptions.Center };

    public DoctorPasswordPage(ApiService api, DoctorRegistrationData data)
    {
        _api = api; _data = data;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F4FF");
        _errFrame = new Frame { BackgroundColor = Color.FromArgb("#FEE2E2"), BorderColor = Color.FromArgb("#FECACA"), CornerRadius = 10, Padding = 12, IsVisible = false, Content = _errLabel };
        _btnStart = new Button { Text = "Démarrer  →", BackgroundColor = Color.FromArgb("#0EA5E9"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16, CornerRadius = 14, HeightRequest = 54 };
        _btnStart.Clicked += OnStart;

        var root = new VerticalStackLayout { Spacing = 0 };

        root.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#0369A1"), Padding = new Thickness(24, 56, 24, 36),
            Children =
            {
                new VerticalStackLayout { Spacing = 8, Children =
                {
                    new Label { Text = $"On y est presque {data.FirstName} 🎉", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, LineBreakMode = LineBreakMode.WordWrap },
                    new Label { Text = "Plus qu'une étape pour créer votre compte.", FontSize = 13, TextColor = Colors.White.WithAlpha(0.85f) }
                }}
            }
        });

        root.Children.Add(DoctorCabinetInfoPage.BuildProgress(2));

        var form = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(24, 28, 24, 40) };

        form.Children.Add(new Frame { BackgroundColor = Color.FromArgb("#F0F9FF"), BorderColor = Color.FromArgb("#BAE6FD"), CornerRadius = 12, Padding = 14, HasShadow = false, Content = new HorizontalStackLayout { Spacing = 10, Children = { new Label { Text = "🔐", FontSize = 20, VerticalOptions = LayoutOptions.Center }, new Label { Text = "Vos données sont sécurisées et chiffrées.", FontSize = 13, TextColor = Color.FromArgb("#0369A1"), VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap } } } });

        var emailEntry = new Entry { Text = data.Email, IsReadOnly = true, BackgroundColor = Colors.Transparent, FontSize = 14, TextColor = Color.FromArgb("#64748B"), FontAttributes = FontAttributes.Italic };
        form.Children.Add(Wrap("Email", new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 12, Padding = new Thickness(14, 0), HasShadow = false, BackgroundColor = Color.FromArgb("#F1F5F9"), Content = emailEntry }));

        var eye1 = EyeBtn(_password); var pwGrid1 = PwGrid(_password, eye1);
        form.Children.Add(Wrap("Mot de passe *", new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 12, Padding = 0, HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = pwGrid1 }));

        var eye2 = EyeBtn(_confirm); var pwGrid2 = PwGrid(_confirm, eye2);
        form.Children.Add(Wrap("Confirmation du mot de passe *", new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 12, Padding = 0, HasShadow = false, BackgroundColor = Color.FromArgb("#F8FAFF"), Content = pwGrid2 }));

        form.Children.Add(new Label { Text = "* Champs obligatoires", FontSize = 11, TextColor = Color.FromArgb("#94A3B8") });
        form.Children.Add(_errFrame);
        form.Children.Add(_spinner);

        var backBtn = new Button { Text = "← Précédent", BackgroundColor = Color.FromArgb("#E2E8F0"), TextColor = Color.FromArgb("#374151"), FontSize = 15, CornerRadius = 14, HeightRequest = 50 };
        backBtn.Clicked += async (_, _) => await Navigation.PopAsync();
        var btnRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(new GridLength(12)), new(new GridLength(2, GridUnitType.Star)) } };
        btnRow.Children.Add(backBtn);
        Grid.SetColumn(_btnStart, 2); btnRow.Children.Add(_btnStart);
        form.Children.Add(btnRow);

        root.Children.Add(new Frame { CornerRadius = 28, BackgroundColor = Colors.White, HasShadow = false, BorderColor = Colors.Transparent, Margin = new Thickness(0, -20, 0, 0), Padding = 0, Content = form });
        Content = new ScrollView { Content = root };
    }

    private async void OnStart(object? s, EventArgs e)
    {
        _errFrame.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_password.Text))
        { _errLabel.Text = "Veuillez saisir un mot de passe."; _errFrame.IsVisible = true; return; }
        if (_password.Text.Length < 6)
        { _errLabel.Text = "Le mot de passe doit contenir au moins 6 caractères."; _errFrame.IsVisible = true; return; }
        if (_password.Text != _confirm.Text)
        { _errLabel.Text = "Les mots de passe ne correspondent pas."; _errFrame.IsVisible = true; return; }

        _btnStart.IsEnabled = false; _btnStart.Text = "Création..."; _spinner.IsRunning = true; _spinner.IsVisible = true;
        try
        {
            var result = await _api.RegisterDoctorAsync(new RegisterDoctorRequest
            {
                FirstName = _data.FirstName, LastName = _data.LastName,
                Email = _data.Email, Password = _password.Text,
                PhoneNumber = _data.PhoneNumber, City = _data.City,
                Address = _data.Address, PostalCode = _data.PostalCode,
                Languages = _data.Languages, SpecialtyId = _data.SpecialtyId
            });
            if (!result.Success) { _errLabel.Text = result.Message ?? "Erreur lors de l'inscription."; _errFrame.IsVisible = true; return; }

            Preferences.Set("Token", result.Token ?? ""); Preferences.Set("UserId", result.UserId?.ToString() ?? "");
            Preferences.Set("ProfileId", result.ProfileId?.ToString() ?? ""); Preferences.Set("Role", "Doctor");
            Preferences.Set("FullName", result.FullName ?? ""); Preferences.Set("ProfileImageUrl", "");

            var signalR = ServiceHelper.GetService<SignalRService>();
            await signalR.ConnectAsync();
            Application.Current!.MainPage = new NavigationPage(new DoctorOnboardingPage(ServiceHelper.GetService<ApiService>(), _data.FirstName));
        }
        catch (Exception ex) { _errLabel.Text = $"Erreur: {ex.Message}"; _errFrame.IsVisible = true; }
        finally { _btnStart.IsEnabled = true; _btnStart.Text = "Démarrer  →"; _spinner.IsRunning = false; _spinner.IsVisible = false; }
    }

    private static View Wrap(string label, View content) => new VerticalStackLayout { Spacing = 6, Children = { new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") }, content } };
    private static Label EyeBtn(Entry entry) { var l = new Label { Text = "👁", FontSize = 18, VerticalOptions = LayoutOptions.Center }; l.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => entry.IsPassword = !entry.IsPassword) }); return l; }
    private static Grid PwGrid(Entry entry, Label eye) { var g = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, Padding = new Thickness(14, 0) }; g.Children.Add(entry); Grid.SetColumn(eye, 1); g.Children.Add(eye); return g; }
}
