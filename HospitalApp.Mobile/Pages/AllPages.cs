using HospitalApp.Mobile.Models;
using HospitalApp.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;

namespace HospitalApp.Mobile.Pages;

public class PatientDashboardPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;

    public PatientDashboardPage(ApiService api, SignalRService signalR)
    {
        _api = api;
        _signalR = signalR;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#EEF2FF");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _signalR.IncomingCallReceived += OnIncomingCall;
        _signalR.NotificationReceived += OnChatNotification;
        Content = new ActivityIndicator
        {
            IsRunning = true, Color = Color.FromArgb("#4F46E5"),
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        };
        await LoadDashboard();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _signalR.IncomingCallReceived -= OnIncomingCall;
        _signalR.NotificationReceived -= OnChatNotification;
    }

    private void OnIncomingCall(IncomingCallDto call)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var page = new VideoCallPage(_signalR, Guid.Parse(call.CallerId), call.CallerName, call.IsVideo, isIncoming: true, sessionId: call.SessionId);
            await Navigation.PushModalAsync(new NavigationPage(page));
        });
    }

    private void OnChatNotification(NotificationDto notif)
    {
        if (notif.Type != "Chat" || string.IsNullOrEmpty(notif.SenderId)) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool open = await Application.Current!.MainPage!.DisplayAlert(
                "💬 " + notif.Title, notif.Message, "Ouvrir", "Plus tard");
            if (open && Guid.TryParse(notif.SenderId, out var senderGuid))
                await Navigation.PushAsync(new ChatPage(_api, _signalR, senderGuid,
                    notif.Title.Replace("New message from ", "")));
        });
    }

    private async Task LoadDashboard()
    {
        var patientId = Preferences.Get("ProfileId", "");
        List<AppointmentDto> appointments = new();
        if (!string.IsNullOrEmpty(patientId))
            try { appointments = await _api.GetPatientAppointmentsAsync(Guid.Parse(patientId)); } catch { }

        var upcoming  = appointments.Where(a => a.IsUpcoming).OrderBy(a => a.AppointmentDate).ToList();
        var completed = appointments.Count(a => a.Status == "Completed");
        var cancelled = appointments.Count(a => a.Status == "Cancelled");

        var fullName  = Preferences.Get("FullName", "Patient");
        var firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Patient";
        var initials  = string.Join("", fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(2).Select(w => char.ToUpper(w[0]).ToString()));

        // ═══════════════════════════════════════════════════════════
        // ROOT LAYOUT
        // ═══════════════════════════════════════════════════════════
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new(GridLength.Auto),  // header
                new(GridLength.Star),  // body
                new(GridLength.Auto)   // bottom nav
            },
            BackgroundColor = Color.FromArgb("#F0F7FA")
        };

        // ── HEADER ────────────────────────────────────────────────────
        var headerGrid = new Grid { Padding = new Thickness(22, 54, 22, 36) };
        headerGrid.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };

        var hRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star), new(GridLength.Auto)
            }
        };

        var avatarCircle = new Frame
        {
            WidthRequest = 42, HeightRequest = 42, CornerRadius = 21,
            BackgroundColor = Colors.White.WithAlpha(0.25f),
            BorderColor = Colors.White.WithAlpha(0.5f),
            HasShadow = false, Padding = 0,
            Content = new Label
            {
                Text = initials, FontSize = 15, FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var greetCol = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        greetCol.Children.Add(new Label
        {
            Text = $"Bonjour, {firstName} 👋",
            FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Colors.White
        });
        greetCol.Children.Add(new Label
        {
            Text = DateTime.Today.ToString("dddd, dd MMMM"),
            FontSize = 12, TextColor = Color.FromArgb("#A5B4FC")
        });

        var leftH = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
        leftH.Children.Add(avatarCircle);
        leftH.Children.Add(greetCol);
        hRow.Children.Add(leftH);

        var bell = new Frame
        {
            BackgroundColor = Colors.White.WithAlpha(0.18f),
            BorderColor = Colors.White.WithAlpha(0.35f),
            CornerRadius = 13, WidthRequest = 42, HeightRequest = 42,
            Padding = 0, HasShadow = false,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "🔔", FontSize = 18, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        bell.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new NotificationsPage()))
        });
        Grid.SetColumn(bell, 1);
        hRow.Children.Add(bell);
        headerGrid.Children.Add(hRow);
        root.Children.Add(headerGrid);

        // ── BODY ──────────────────────────────────────────────────────
        var body = new VerticalStackLayout { Padding = new Thickness(18, 16, 18, 20), Spacing = 18 };

        // ── STATS CARD (floats over header) ──────────────────────────
        var statsRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star), new(GridLength.Auto),
                new(GridLength.Star), new(GridLength.Auto),
                new(GridLength.Star)
            },
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, -22, 0, 0)
        };
        statsRow.SetValue(Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific
            .VisualElement.ElevationProperty, 4.0);

        var sDiv = new BoxView { WidthRequest = 1, HeightRequest = 30, BackgroundColor = Color.FromArgb("#E2E8F0"), VerticalOptions = LayoutOptions.Center };
        var sDiv2 = new BoxView { WidthRequest = 1, HeightRequest = 30, BackgroundColor = Color.FromArgb("#E2E8F0"), VerticalOptions = LayoutOptions.Center };

        var sc1 = StatPill(upcoming.Count.ToString(), "À venir",  "#126B82");
        var sc2 = StatPill(completed.ToString(),      "Terminés", "#059669");
        var sc3 = StatPill(cancelled.ToString(),      "Annulés",  "#DC2626");

        Grid.SetColumn(sc1, 0); Grid.SetColumn(sDiv, 1);
        Grid.SetColumn(sc2, 2); Grid.SetColumn(sDiv2, 3);
        Grid.SetColumn(sc3, 4);

        statsRow.Children.Add(sc1); statsRow.Children.Add(sDiv);
        statsRow.Children.Add(sc2); statsRow.Children.Add(sDiv2);
        statsRow.Children.Add(sc3);

        var statsCard = new Frame
        {
            CornerRadius = 20, Padding = new Thickness(0, 12),
            HasShadow = true, BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = statsRow
        };
        body.Children.Add(statsCard);

        // ── NEXT APPOINTMENT ─────────────────────────────────────────
        body.Children.Add(SectionHeader("Prochain Rendez-vous", null));

        if (upcoming.Any())
        {
            var next    = upcoming.First();
            var isVideo = next.ConsultationType == "Video";
            var docInit = (next.DoctorName ?? "Dr").Replace("Dr. ", "").Split(' ')
                              .Take(2).Aggregate("", (a, w) => a + char.ToUpper(w[0]));

            // White card with colored left accent
            var accentColor = isVideo ? Color.FromArgb("#4F46E5") : Color.FromArgb("#0EA5E9");

            var docAvatar = new Frame
            {
                WidthRequest = 52, HeightRequest = 52, CornerRadius = 16,
                BackgroundColor = accentColor.WithAlpha(0.12f),
                BorderColor = accentColor.WithAlpha(0.3f),
                HasShadow = false, Padding = 0,
                Content = new Label
                {
                    Text = docInit, FontSize = 18, FontAttributes = FontAttributes.Bold,
                    TextColor = accentColor,
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
                }
            };

            var typePill = new Frame
            {
                CornerRadius = 20,
                BackgroundColor = isVideo ? Color.FromArgb("#EEF2FF") : Color.FromArgb("#E0F2FE"),
                BorderColor = Colors.Transparent,
                HasShadow = false, Padding = new Thickness(10, 4),
                HorizontalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = isVideo ? "📹  Vidéo" : "🏥  Présentiel",
                    FontSize = 11, FontAttributes = FontAttributes.Bold,
                    TextColor = isVideo ? Color.FromArgb("#4F46E5") : Color.FromArgb("#0369A1")
                }
            };

            var docInfo = new VerticalStackLayout
            {
                Spacing = 3, VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    new Label { Text = next.DoctorName ?? "Médecin", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") },
                    new Label { Text = next.DoctorSpecialty ?? "Spécialiste", FontSize = 12, TextColor = Color.FromArgb("#64748B") }
                }
            };

            var topRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Auto), new(GridLength.Star)
                },
                ColumnSpacing = 14
            };
            topRow.Children.Add(docAvatar);
            Grid.SetColumn(docInfo, 1); topRow.Children.Add(docInfo);

            var dateRow = new HorizontalStackLayout { Spacing = 6 };
            dateRow.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"), BorderColor = Colors.Transparent,
                CornerRadius = 8, Padding = new Thickness(10, 5), HasShadow = false,
                Content = new Label { Text = $"📅  {next.AppointmentDate:dd MMM yyyy}", FontSize = 12, TextColor = Color.FromArgb("#334155") }
            });
            dateRow.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"), BorderColor = Colors.Transparent,
                CornerRadius = 8, Padding = new Thickness(10, 5), HasShadow = false,
                Content = new Label { Text = $"⏰  {next.StartTime:hh\\:mm}", FontSize = 12, TextColor = Color.FromArgb("#334155") }
            });

            var cardInner = new VerticalStackLayout { Spacing = 12 };
            cardInner.Children.Add(topRow);
            cardInner.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#F1F5F9") });
            cardInner.Children.Add(dateRow);
            cardInner.Children.Add(typePill);

            if (isVideo && next.AppointmentDate.Date == DateTime.Today && next.DoctorUserId.HasValue && _signalR != null)
            {
                var joinBtn = new Frame
                {
                    BackgroundColor = Color.FromArgb("#4F46E5"),
                    BorderColor = Colors.Transparent,
                    CornerRadius = 14, Padding = new Thickness(0, 13),
                    HasShadow = false,
                    Content = new Label
                    {
                        Text = "▶   Rejoindre la consultation",
                        FontSize = 14, FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center
                    }
                };
                var dId = next.DoctorUserId!.Value;
                var dName = next.DoctorName ?? "Médecin";
                joinBtn.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                        await Navigation.PushAsync(new VideoCallPage(_signalR, dId, dName, true)))
                });
                cardInner.Children.Add(joinBtn);
            }

            // Left accent border via Grid
            var accentBar = new BoxView { WidthRequest = 4, BackgroundColor = accentColor, CornerRadius = 4 };
            var apptLayout = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Auto), new(GridLength.Star)
                },
                ColumnSpacing = 14
            };
            apptLayout.Children.Add(accentBar);
            Grid.SetColumn(cardInner, 1);
            apptLayout.Children.Add(cardInner);

            body.Children.Add(new Frame
            {
                BackgroundColor = Colors.White, CornerRadius = 20,
                Padding = new Thickness(16), HasShadow = true,
                BorderColor = Color.FromArgb("#E2E8F0"),
                Content = apptLayout
            });
        }
        else
        {
            var noAppt = new Frame
            {
                BackgroundColor = Colors.White, CornerRadius = 20,
                Padding = new Thickness(20), HasShadow = false,
                BorderColor = Color.FromArgb("#E2E8F0")
            };
            var bookPill = new Frame
            {
                BackgroundColor = Color.FromArgb("#126B82"), BorderColor = Colors.Transparent,
                CornerRadius = 12, Padding = new Thickness(20, 11),
                HasShadow = false, HorizontalOptions = LayoutOptions.Start,
                Content = new Label { Text = "Prendre rendez-vous  →", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
            };
            bookPill.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Navigation.PushAsync(new SpecialtySelectionPage(_api)))
            });
            noAppt.Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = "Aucun rendez-vous à venir", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") },
                    new Label { Text = "Consultez un médecin facilement.", FontSize = 13, TextColor = Color.FromArgb("#64748B") },
                    bookPill
                }
            };
            body.Children.Add(noAppt);
        }

        // ── QUICK ACTIONS ─────────────────────────────────────────────
        body.Children.Add(SectionHeader("Actions rapides", null));

        var actions = new (string emoji, string title, string bg, string accent, Func<Task> nav)[]
        {
            ("📅", "Prendre\nRDV",       "#E0F4F8", "#126B82", async () => await Navigation.PushAsync(new DoctorSearchPage(_api, _signalR))),
            ("🗓", "Mes\nRDV",            "#ECFDF5", "#059669", async () => await Navigation.PushAsync(new PatientAppointmentsPage(_api, _signalR))),
            ("📹", "Consult.\nVidéo",     "#F0F4FF", "#4F46E5", async () => await Navigation.PushAsync(new PatientInstantConsultationsPage(_api, _signalR))),
            ("💬", "Messagerie",          "#FDF4FF", "#9333EA", async () => await Navigation.PushAsync(new ChatContactsPage(_api, _signalR))),
            ("👤", "Mon\nProfil",         "#FFF7ED", "#EA580C", async () => await Navigation.PushAsync(new PatientProfilePage(_api))),
            ("📋", "E-Ordon-\nnances",    "#F0FDF4", "#059669", async () => await Navigation.PushAsync(new PatientEOrdonnancesPage(_api))),
            ("📁", "Documents",           "#EFF6FF", "#2563EB", async () => await Navigation.PushAsync(new PatientDocumentsPage(_api))),
        };

        var actGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions    = new RowDefinitionCollection    { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) },
            ColumnSpacing = 10, RowSpacing = 10
        };

        for (int i = 0; i < actions.Length; i++)
        {
            var (emoji, title, bg, accent, nav) = actions[i];
            var tile = ActionTile(emoji, title, bg, accent);
            Grid.SetRow(tile, i / 3);
            Grid.SetColumn(tile, i % 3);
            var captured = nav;
            tile.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await captured())
            });
            actGrid.Children.Add(tile);
        }
        body.Children.Add(actGrid);

        // ── ASSEMBLE ROOT ─────────────────────────────────────────────
        var scrollBody = new ScrollView { Content = body };
        Grid.SetRow(scrollBody, 1);
        root.Children.Add(scrollBody);

        // ── BOTTOM NAV ────────────────────────────────────────────────
        var navBar = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(8, 10, 8, 22),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)
            }
        };

        var navDef = new (string icon, string label, Func<Task>? action)[]
        {
            ("🏠", "Accueil",    null),
            ("📅", "Rendez-vous", async () => await Navigation.PushAsync(new PatientAppointmentsPage(_api, _signalR))),
            ("💬", "Messages",   async () => await Navigation.PushAsync(new ChatContactsPage(_api, _signalR))),
            ("👤", "Profil",     async () => await Navigation.PushAsync(new PatientProfilePage(_api)))
        };

        for (int i = 0; i < navDef.Length; i++)
        {
            var (icon, label, navAction) = navDef[i];
            bool active = i == 0;
            var iconColor = active ? Color.FromArgb("#126B82") : Color.FromArgb("#94A3B8");

            var navItem = new VerticalStackLayout
            {
                Spacing = 3, HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = active ? Color.FromArgb("#E0F4F8") : Colors.Transparent,
                        BorderColor = Colors.Transparent, HasShadow = false,
                        CornerRadius = 12, Padding = new Thickness(14, 6),
                        HorizontalOptions = LayoutOptions.Center,
                        Content = new Label { Text = icon, FontSize = 20, HorizontalOptions = LayoutOptions.Center }
                    },
                    new Label
                    {
                        Text = label, FontSize = 9,
                        FontAttributes = active ? FontAttributes.Bold : FontAttributes.None,
                        TextColor = iconColor,
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            };

            if (navAction != null)
            {
                var cap = navAction;
                navItem.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await cap())
                });
            }

            Grid.SetColumn(navItem, i);
            navBar.Children.Add(navItem);
        }

        var navWrapper = new VerticalStackLayout
        {
            Children =
            {
                new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0") },
                navBar
            }
        };
        Grid.SetRow(navWrapper, 2);
        root.Children.Add(navWrapper);

        Content = root;
    }

    private static View SectionHeader(string title, string? actionText)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
                { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 10
        };
        var bar = new BoxView
        {
            WidthRequest = 4, HeightRequest = 18, CornerRadius = 2,
            BackgroundColor = Color.FromArgb("#126B82"),
            VerticalOptions = LayoutOptions.Center
        };
        var lbl = new Label
        {
            Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F2540"), VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(lbl, 1);
        row.Children.Add(bar);
        row.Children.Add(lbl);
        return row;
    }

    private static View StatPill(string value, string label, string colorHex) =>
        new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center, Spacing = 1, Padding = new Thickness(4, 2),
            Children =
            {
                new Label { Text = value, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(colorHex), HorizontalOptions = LayoutOptions.Center },
                new Label { Text = label, FontSize = 10,  TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center }
            }
        };

    private static Frame ActionTile(string emoji, string title, string bgHex, string accentHex)
    {
        var accent = Color.FromArgb(accentHex);
        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 18,
            Padding = new Thickness(16, 18), HasShadow = false,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Color.FromArgb(bgHex), CornerRadius = 14,
                        WidthRequest = 46, HeightRequest = 46, Padding = 0,
                        HasShadow = false, BorderColor = Colors.Transparent,
                        HorizontalOptions = LayoutOptions.Start,
                        Content = new Label { Text = emoji, FontSize = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    },
                    new Label
                    {
                        Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#0F172A"), LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
    }
}

public class PatientAppointmentsPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService? _signalR;
    private List<AppointmentDto> _all = new();
    private string _activeFilter = "Tous";
    private VerticalStackLayout _listStack = new();
    private Label _countLabel = new();

    private static readonly Dictionary<string, string> StatusFr = new()
    {
        ["Scheduled"] = "Planifié", ["Confirmed"] = "Confirmé",
        ["Completed"] = "Terminé",  ["Cancelled"] = "Annulé", ["NoShow"] = "Absent"
    };

    public PatientAppointmentsPage(ApiService api, SignalRService? signalR = null)
    {
        _api = api; _signalR = signalR;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F7FA");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAppointments();
    }

    private async Task LoadAppointments()
    {
        Content = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#126B82"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

        var patientId = Preferences.Get("ProfileId", "");
        if (string.IsNullOrEmpty(patientId)) { Content = new Label { Text = "Session expirée.", HorizontalOptions = LayoutOptions.Center }; return; }

        _all = await _api.GetPatientAppointmentsAsync(Guid.Parse(patientId));
        BuildPage();
    }

    private void BuildPage()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // ── HEADER ──────────────────────────────────────────────────
        var header = new Grid { Padding = new Thickness(20, 52, 20, 24) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };
        var back = new Label { Text = "‹", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        _countLabel = new Label { Text = $"{_all.Count} rendez-vous au total", FontSize = 12, TextColor = Colors.White.WithAlpha(0.75f) };
        header.Children.Add(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                back,
                new Label { Text = "Mes Rendez-vous", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                _countLabel
            }
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // ── FILTER CHIPS ────────────────────────────────────────────
        var filters = new string[] { "Tous", "À venir", "Passés", "Annulés" };
        var chipRow = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(16, 14, 16, 0) };
        foreach (var f in filters)
        {
            var chip = MakeChip(f, f == _activeFilter);
            var fCopy = f;
            chip.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => { _activeFilter = fCopy; BuildPage(); })
            });
            chipRow.Children.Add(chip);
        }

        // ── LIST ────────────────────────────────────────────────────
        _listStack = new VerticalStackLayout { Padding = new Thickness(16, 14, 16, 0), Spacing = 14 };
        RefreshList();

        var apptScroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children = { chipRow, _listStack, new BoxView { HeightRequest = 32, Color = Colors.Transparent } }
            }
        };
        Grid.SetRow(apptScroll, 1);
        root.Children.Add(apptScroll);
        Content = root;
    }

    private void RefreshList()
    {
        _listStack.Children.Clear();
        var filtered = _activeFilter switch
        {
            "À venir"  => _all.Where(a => a.IsUpcoming).ToList(),
            "Passés"   => _all.Where(a => a.AppointmentDate < DateTime.Today || a.Status == "Completed").ToList(),
            "Annulés"  => _all.Where(a => a.Status is "Cancelled" or "NoShow").ToList(),
            _          => _all
        };
        filtered = filtered.OrderByDescending(a => a.AppointmentDate).ToList();
        _countLabel.Text = $"{filtered.Count} rendez-vous";

        if (!filtered.Any())
        {
            _listStack.Children.Add(BuildEmptyState());
            return;
        }

        foreach (var appt in filtered)
            _listStack.Children.Add(BuildCard(appt));
    }

    private View BuildCard(AppointmentDto appt)
    {
        var isVideo = appt.ConsultationType == "Video";
        var accentColor = isVideo ? Color.FromArgb("#126B82") : Color.FromArgb("#059669");
        var statusLabel = StatusFr.GetValueOrDefault(appt.Status, appt.Status);

        // Status badge
        var statusBadge = new Frame
        {
            BackgroundColor = appt.StatusBg, CornerRadius = 10, Padding = new Thickness(10, 4),
            HasShadow = false, BorderColor = Colors.Transparent, VerticalOptions = LayoutOptions.Start,
            Content = new Label { Text = statusLabel, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = appt.StatusColor }
        };

        // Doctor info + date + type
        var topGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        topGrid.Children.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = appt.DoctorName ?? "Médecin", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540") },
                new Label { Text = appt.DoctorSpecialty ?? "", FontSize = 12, TextColor = Color.FromArgb("#126B82"), FontAttributes = FontAttributes.Bold },
            }
        });
        Grid.SetColumn(statusBadge, 1); topGrid.Children.Add(statusBadge);

        // Date / time / type row
        var metaRow = new HorizontalStackLayout { Spacing = 10 };
        metaRow.Children.Add(MetaPill($"📅 {appt.AppointmentDate:dd MMM yyyy}", "#F0F9FF", "#0369A1"));
        metaRow.Children.Add(MetaPill($"⏰ {appt.StartTime:hh\\:mm}", "#F0F9FF", "#0369A1"));
        metaRow.Children.Add(MetaPill(isVideo ? "📹 Vidéo" : "🏥 Présentiel", isVideo ? "#E0F4F8" : "#ECFDF5", isVideo ? "#126B82" : "#059669"));

        var cardContent = new VerticalStackLayout { Spacing = 10, Children = { topGrid, metaRow } };

        // Action buttons row
        var actionRow = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 2, 0, 0) };

        if (_signalR != null && appt.DoctorUserId.HasValue)
        {
            var chatBtn = ActionChip("💬 Discuter", "#E0F4F8", "#126B82", "#A8D8E4");
            var duid = appt.DoctorUserId.Value; var dname = appt.DoctorName ?? "Médecin";
            var sr = _signalR; var apiRef = _api;
            chatBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await Navigation.PushAsync(new ChatPage(apiRef, sr!, duid, dname))) });
            actionRow.Children.Add(chatBtn);
        }

        if (appt.CanCancel)
        {
            var cancelBtn = ActionChip("✕ Annuler", "#FEE2E2", "#DC2626", "#FECACA");
            var apptId = appt.Id;
            cancelBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    bool confirm = await DisplayAlert("Annuler le rendez-vous",
                        $"Confirmer l'annulation avec {appt.DoctorName} le {appt.AppointmentDate:dd MMM} ?",
                        "Oui, annuler", "Retour");
                    if (!confirm) return;
                    var ok = await _api.CancelAppointmentAsync(apptId);
                    if (ok)
                    {
                        var a = _all.FirstOrDefault(x => x.Id == apptId);
                        if (a != null) a.Status = "Cancelled";
                        RefreshList();
                    }
                    else
                        await DisplayAlert("Erreur", "Impossible d'annuler ce rendez-vous.", "OK");
                })
            });
            actionRow.Children.Add(cancelBtn);
        }

        if (appt.Status == "Completed")
        {
            var rateBtn = ActionChip("⭐ Évaluer", "#FEF9C3", "#92400E", "#FDE68A");
            var apptRef = appt;
            rateBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await Navigation.PushAsync(new RatingPage(_api, apptRef))) });
            actionRow.Children.Add(rateBtn);
        }

        if (actionRow.Children.Any())
            cardContent.Children.Add(actionRow);

        // Left accent bar
        var accentBar = new BoxView { WidthRequest = 4, BackgroundColor = accentColor, CornerRadius = 2 };
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 14
        };
        layout.Children.Add(accentBar);
        Grid.SetColumn(cardContent, 1); layout.Children.Add(cardContent);

        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 18,
            Padding = new Thickness(16), HasShadow = false,
            BorderColor = Color.FromArgb("#E2E8F0"), Content = layout
        };
    }

    private View BuildEmptyState()
    {
        var bookBtn = new Button
        {
            Text = "Prendre un rendez-vous",
            BackgroundColor = Color.FromArgb("#126B82"),
            TextColor = Colors.White, CornerRadius = 14,
            HeightRequest = 50, FontAttributes = FontAttributes.Bold,
            FontSize = 14, Margin = new Thickness(0, 8, 0, 0)
        };
        bookBtn.Clicked += async (_, _) => await Navigation.PushAsync(new DoctorSearchPage(_api, _signalR));

        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 20,
            Padding = new Thickness(30, 48), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center, Spacing = 8,
                Children =
                {
                    new Label { Text = "📅", FontSize = 52, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Aucune donnée disponible", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Vous n'avez pas encore de rendez-vous\ndans cette catégorie.", FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center },
                    bookBtn
                }
            }
        };
    }

    // ── small helpers ────────────────────────────────────────────────
    private static Frame MakeChip(string text, bool active) => new()
    {
        BackgroundColor = active ? Color.FromArgb("#126B82") : Colors.White,
        CornerRadius = 20, Padding = new Thickness(16, 8),
        HasShadow = false,
        BorderColor = active ? Color.FromArgb("#126B82") : Color.FromArgb("#CBD5E1"),
        Content = new Label
        {
            Text = text, FontSize = 13,
            TextColor = active ? Colors.White : Color.FromArgb("#475569"),
            FontAttributes = active ? FontAttributes.Bold : FontAttributes.None
        }
    };

    private static Frame MetaPill(string text, string bg, string fg) => new()
    {
        BackgroundColor = Color.FromArgb(bg), CornerRadius = 8,
        Padding = new Thickness(8, 4), HasShadow = false, BorderColor = Colors.Transparent,
        Content = new Label { Text = text, FontSize = 11, TextColor = Color.FromArgb(fg) }
    };

    private static Frame ActionChip(string text, string bg, string fg, string border) => new()
    {
        BackgroundColor = Color.FromArgb(bg), CornerRadius = 10,
        Padding = new Thickness(14, 8), HasShadow = false,
        BorderColor = Color.FromArgb(border),
        Content = new Label { Text = text, FontSize = 12, TextColor = Color.FromArgb(fg), FontAttributes = FontAttributes.Bold }
    };
}

public class PatientProfilePage : ContentPage
{
    private readonly ApiService _api;

    // ── editable fields ──────────────────────────────────────────────
    private readonly Entry _firstName  = MakeEntry("Prénom");
    private readonly Entry _lastName   = MakeEntry("Nom");
    private readonly Entry _phone      = MakeEntry("Téléphone", Keyboard.Telephone);
    private readonly Entry _city       = MakeEntry("Ville");
    private readonly Entry _address    = MakeEntry("Adresse");
    private readonly Picker _gender    = new() { Title = "Genre", FontSize = 14, TextColor = Color.FromArgb("#0F172A") };
    private readonly Picker _blood     = new() { Title = "Groupe sanguin", FontSize = 14, TextColor = Color.FromArgb("#0F172A") };
    private readonly Entry _height     = MakeEntry("Taille (cm)", Keyboard.Numeric);
    private readonly Entry _weight     = MakeEntry("Poids (kg)", Keyboard.Numeric);
    private readonly Entry _allergies  = MakeEntry("Allergies");
    private readonly Entry _chronic    = MakeEntry("Maladies chroniques");
    private readonly Entry _meds       = MakeEntry("Médicaments actuels");
    private readonly Entry _illnesses  = MakeEntry("Antécédents médicaux");
    private readonly Entry _insurer    = MakeEntry("Compagnie d'assurance");
    private readonly Entry _insureNum  = MakeEntry("Numéro de police");

    private readonly Label  _errorLabel = new() { TextColor = Color.FromArgb("#DC2626"), FontSize = 13, IsVisible = false, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Button _saveBtn;
    private Guid _patientId;
    private bool _uiBuilt;

    public PatientProfilePage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F7FA");

        _gender.ItemsSource = new List<string> { "Homme", "Femme", "Autre" };
        _blood.ItemsSource  = new List<string> { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

        _saveBtn = new Button
        {
            Text = "Enregistrer les modifications",
            BackgroundColor = Color.FromArgb("#126B82"),
            TextColor = Colors.White,
            CornerRadius = 14,
            HeightRequest = 54,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15
        };
        _saveBtn.Clicked += OnSave;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_uiBuilt)
            await RefreshValues();   // only re-fill values, never rebuild parents
        else
            await LoadProfile();
    }

    private async Task RefreshValues()
    {
        var profileIdStr = Preferences.Get("ProfileId", "");
        if (!Guid.TryParse(profileIdStr, out var pid)) return;
        PatientDetailDto? patient = null;
        try { patient = await _api.GetPatientAsync(pid); } catch { }
        if (patient == null) return;
        PopulateFields(patient);
    }

    private void PopulateFields(PatientDetailDto patient)
    {
        _firstName.Text  = patient.FirstName;
        _lastName.Text   = patient.LastName;
        _phone.Text      = patient.PhoneNumber ?? "";
        _city.Text       = patient.City ?? "";
        _address.Text    = patient.Address ?? "";
        _height.Text     = patient.Height?.ToString() ?? "";
        _weight.Text     = patient.Weight?.ToString() ?? "";
        _allergies.Text  = patient.Allergies ?? "";
        _chronic.Text    = patient.ChronicDiseases ?? "";
        _meds.Text       = patient.CurrentMedications ?? "";
        _illnesses.Text  = patient.PreviousIllnesses ?? "";
        _insurer.Text    = patient.InsuranceProvider ?? "";
        _insureNum.Text  = patient.InsuranceNumber ?? "";

        var genderMap = new Dictionary<string, string>
            { { "Male", "Homme" }, { "Female", "Femme" }, { "Other", "Autre" } };
        if (patient.Gender != null && genderMap.TryGetValue(patient.Gender, out var gFr))
            _gender.SelectedItem = gFr;
        if (patient.BloodType != null)
            _blood.SelectedItem = patient.BloodType;
    }

    private async Task LoadProfile()
    {
        Content = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#126B82"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

        var profileIdStr = Preferences.Get("ProfileId", "");
        PatientDetailDto? patient = null;
        if (!string.IsNullOrEmpty(profileIdStr) && Guid.TryParse(profileIdStr, out var pid))
        {
            _patientId = pid;
            try { patient = await _api.GetPatientAsync(pid); } catch { }
        }

        // pre-fill fields
        if (patient != null) PopulateFields(patient);

        var initials = patient != null
            ? $"{patient.FirstName.FirstOrDefault()}{patient.LastName.FirstOrDefault()}".ToUpper()
            : "?";
        var fullName = patient?.FullName ?? Preferences.Get("FullName", "Patient");

        // ── ROOT ────────────────────────────────────────────────────
        var root = new VerticalStackLayout { Spacing = 0 };

        // Header
        var headerGrid = new Grid { Padding = new Thickness(20, 52, 20, 32) };
        headerGrid.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };
        var backLbl = new Label { Text = "‹", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        backLbl.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        headerGrid.Children.Add(new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                backLbl,
                new HorizontalStackLayout
                {
                    Spacing = 16,
                    Children =
                    {
                        new Frame
                        {
                            BackgroundColor = Colors.White.WithAlpha(0.22f),
                            CornerRadius = 36, WidthRequest = 72, HeightRequest = 72,
                            Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                            Content = new Label { Text = initials, FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                        },
                        new VerticalStackLayout
                        {
                            VerticalOptions = LayoutOptions.Center, Spacing = 3,
                            Children =
                            {
                                new Label { Text = fullName, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                                new Label { Text = "Mon profil", FontSize = 13, TextColor = Colors.White.WithAlpha(0.75f) }
                            }
                        }
                    }
                }
            }
        });
        root.Children.Add(headerGrid);

        // ── FORM ────────────────────────────────────────────────────
        var formStack = new VerticalStackLayout { Padding = new Thickness(20, 20, 20, 0), Spacing = 16 };

        formStack.Children.Add(SectionCard("👤 Informations personnelles", new View[]
        {
            FormRow(_firstName, _lastName),
            FormField("Genre", _gender),
            FormField("Téléphone", _phone),
            FormField("Ville", _city),
            FormField("Adresse", _address),
        }));

        formStack.Children.Add(SectionCard("🩺 Dossier médical", new View[]
        {
            FormField("Groupe sanguin", _blood),
            FormRow(_height, _weight),
            FormField("Allergies", _allergies),
            FormField("Maladies chroniques", _chronic),
            FormField("Médicaments actuels", _meds),
            FormField("Antécédents", _illnesses),
        }));

        formStack.Children.Add(SectionCard("🛡️ Assurance", new View[]
        {
            FormField("Compagnie", _insurer),
            FormField("N° de police", _insureNum),
        }));

        formStack.Children.Add(_errorLabel);
        formStack.Children.Add(_saveBtn);

        // Documents shortcut button
        var docsBtn = new Button
        {
            Text = "📁  Mes Documents médicaux",
            BackgroundColor = Color.FromArgb("#EFF6FF"),
            TextColor = Color.FromArgb("#0369A1"),
            CornerRadius = 14,
            HeightRequest = 50,
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 4, 0, 0)
        };
        docsBtn.Clicked += async (_, _) => await Navigation.PushAsync(new PatientDocumentsPage(_api));
        formStack.Children.Add(docsBtn);

        // Logout button
        var logoutBtn = new Button
        {
            Text = "Se déconnecter",
            BackgroundColor = Color.FromArgb("#FEE2E2"),
            TextColor = Color.FromArgb("#DC2626"),
            CornerRadius = 14,
            HeightRequest = 50,
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 4, 0, 0)
        };
        logoutBtn.Clicked += (_, _) =>
        {
            Preferences.Clear();
            Application.Current!.MainPage = new NavigationPage(
                new LoginPage(ServiceHelper.GetService<ApiService>()));
        };
        formStack.Children.Add(logoutBtn);
        formStack.Children.Add(new BoxView { HeightRequest = 40, Color = Colors.Transparent });

        root.Children.Add(formStack);
        Content = new ScrollView { Content = root };
        _uiBuilt = true;
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        _errorLabel.IsVisible = false;
        _saveBtn.IsEnabled = false;
        _saveBtn.Text = "Enregistrement…";

        var genderReverseMap = new Dictionary<string, string>
            { { "Homme", "Male" }, { "Femme", "Female" }, { "Autre", "Other" } };
        var selectedGender = _gender.SelectedItem?.ToString();
        var genderEn = selectedGender != null && genderReverseMap.TryGetValue(selectedGender, out var gEn) ? gEn : null;

        var req = new UpdatePatientRequest
        {
            FirstName  = NullIfEmpty(_firstName.Text),
            LastName   = NullIfEmpty(_lastName.Text),
            PhoneNumber = NullIfEmpty(_phone.Text),
            City       = NullIfEmpty(_city.Text),
            Address    = NullIfEmpty(_address.Text),
            Gender     = genderEn,
            BloodType  = _blood.SelectedItem?.ToString(),
            Height     = decimal.TryParse(_height.Text, out var h) ? h : null,
            Weight     = decimal.TryParse(_weight.Text, out var w) ? w : null,
            Allergies  = NullIfEmpty(_allergies.Text),
            ChronicDiseases = NullIfEmpty(_chronic.Text),
            CurrentMedications = NullIfEmpty(_meds.Text),
            PreviousIllnesses = NullIfEmpty(_illnesses.Text),
            InsuranceProvider = NullIfEmpty(_insurer.Text),
            InsuranceNumber   = NullIfEmpty(_insureNum.Text),
        };

        var ok = await _api.UpdatePatientAsync(_patientId, req);

        _saveBtn.IsEnabled = true;
        _saveBtn.Text = "Enregistrer les modifications";

        if (ok)
        {
            if (req.FirstName != null || req.LastName != null)
                Preferences.Set("FullName", $"{req.FirstName ?? _firstName.Text} {req.LastName ?? _lastName.Text}".Trim());
            await DisplayAlert("✅ Succès", "Votre profil a été mis à jour.", "OK");
        }
        else
        {
            _errorLabel.Text = "Échec de la mise à jour. Veuillez réessayer.";
            _errorLabel.IsVisible = true;
        }
    }

    // ── helpers ─────────────────────────────────────────────────────
    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static Entry MakeEntry(string placeholder, Keyboard? kb = null)
        => new() { Placeholder = placeholder, FontSize = 14, TextColor = Color.FromArgb("#0F172A"),
                   PlaceholderColor = Color.FromArgb("#94A3B8"), Keyboard = kb ?? Keyboard.Default };

    private static Frame SectionCard(string title, View[] fields)
    {
        var inner = new VerticalStackLayout { Spacing = 14 };
        inner.Children.Add(new Label
        {
            Text = title, FontSize = 14, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F2540"), Margin = new Thickness(0, 0, 0, 2)
        });
        foreach (var f in fields) inner.Children.Add(f);
        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 18,
            Padding = new Thickness(18, 16), HasShadow = false,
            BorderColor = Color.FromArgb("#E2E8F0"), Content = inner
        };
    }

    private static View FormField(string label, View input)
    {
        var lbl = new Label { Text = label, FontSize = 12, TextColor = Color.FromArgb("#64748B"), Margin = new Thickness(0, 0, 0, 4) };
        View inputView = input switch
        {
            Entry e => new Frame { BackgroundColor = Color.FromArgb("#F8FAFC"), CornerRadius = 10, Padding = new Thickness(12, 0), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"), HeightRequest = 48, Content = e },
            Picker p => new Frame { BackgroundColor = Color.FromArgb("#F8FAFC"), CornerRadius = 10, Padding = new Thickness(12, 0), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"), HeightRequest = 48, Content = p },
            _ => input
        };
        return new VerticalStackLayout { Spacing = 0, Children = { lbl, inputView } };
    }

    private static View FormRow(Entry left, Entry right)
    {
        var leftField  = FormField(left.Placeholder,  left);
        var rightField = FormField(right.Placeholder, right);
        left.Placeholder  = "";
        right.Placeholder = "";
        var g = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(12) },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        g.Children.Add(leftField);
        Grid.SetColumn(rightField, 2);
        g.Children.Add(rightField);
        return g;
    }
}

// ═══════════════════════════════════════════════════════
// PATIENT DOCUMENTS PAGE
// ═══════════════════════════════════════════════════════
public class PatientDocumentsPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _patientId;
    private List<PatientDocumentDto> _docs = new();
    private VerticalStackLayout _listStack = new();
    private Label _countLabel = new();

    private static readonly List<string> Categories = new() { "Analyse", "Scanner", "Radio", "Rapport", "Autre" };

    public PatientDocumentsPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFF");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var idStr = Preferences.Get("ProfileId", "");
        if (Guid.TryParse(idStr, out _patientId))
            await LoadDocuments();
    }

    private async Task LoadDocuments()
    {
        Content = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#0369A1"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        _docs = await _api.GetDocumentsAsync(_patientId);
        BuildPage();
    }

    private void BuildPage()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // ── HEADER ──────────────────────────────────────────────────
        var header = new Grid { Padding = new Thickness(20, 52, 20, 24) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0C4A6E"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#0369A1"), Offset = 1f }
            }
        };
        var back = new Label { Text = "‹", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        _countLabel = new Label { Text = $"{_docs.Count} document(s)", FontSize = 12, TextColor = Colors.White.WithAlpha(0.75f) };

        // Upload button in header
        var uploadBtn = new Frame
        {
            BackgroundColor = Colors.White.WithAlpha(0.2f), CornerRadius = 12,
            Padding = new Thickness(14, 8), HasShadow = false, BorderColor = Colors.White.WithAlpha(0.4f),
            HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "+ Ajouter", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
        };
        uploadBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await PickAndUpload()) });

        var titleRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        titleRow.Children.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children = { new Label { Text = "Mes Documents", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }, _countLabel }
        });
        Grid.SetColumn(uploadBtn, 1); titleRow.Children.Add(uploadBtn);

        header.Children.Add(new VerticalStackLayout { Spacing = 12, Children = { back, titleRow } });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // ── BODY ────────────────────────────────────────────────────
        _listStack = new VerticalStackLayout { Padding = new Thickness(16, 18, 16, 0), Spacing = 12 };
        RefreshList();

        var docsScroll = new ScrollView
        {
            Content = new VerticalStackLayout { Spacing = 0, Children = { _listStack, new BoxView { HeightRequest = 32, Color = Colors.Transparent } } }
        };
        Grid.SetRow(docsScroll, 1);
        root.Children.Add(docsScroll);
        Content = root;
    }

    private void RefreshList()
    {
        _listStack.Children.Clear();
        _countLabel.Text = $"{_docs.Count} document(s)";

        if (!_docs.Any())
        {
            // Upload CTA card
            var ctaBtn = new Frame
            {
                BackgroundColor = Color.FromArgb("#0369A1"), CornerRadius = 14,
                Padding = new Thickness(20, 14), HasShadow = false, BorderColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label { Text = "+ Ajouter un document", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
            };
            ctaBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await PickAndUpload()) });

            _listStack.Children.Add(new Frame
            {
                BackgroundColor = Colors.White, CornerRadius = 20,
                Padding = new Thickness(30, 52), HasShadow = false, BorderColor = Color.FromArgb("#BFDBFE"),
                Content = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 12,
                    Children =
                    {
                        new Label { Text = "📁", FontSize = 52, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Aucune donnée disponible", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Ajoutez vos analyses, radios, scanners\net autres documents médicaux.", FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center },
                        ctaBtn
                    }
                }
            });
            return;
        }

        // Group by category
        foreach (var group in _docs.GroupBy(d => d.Category).OrderBy(g => g.Key))
        {
            _listStack.Children.Add(new Label
            {
                Text = $"{CategoryIcon(group.Key)} {group.Key}",
                FontSize = 13, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0369A1"), Margin = new Thickness(2, 4, 0, 0)
            });
            foreach (var doc in group)
                _listStack.Children.Add(BuildDocCard(doc));
        }
    }

    private View BuildDocCard(PatientDocumentDto doc)
    {
        var nameLabel = new Label { Text = doc.OriginalName, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), LineBreakMode = LineBreakMode.TailTruncation };
        var metaLabel = new Label { Text = $"{doc.SizeDisplay}  ·  {doc.DateDisplay}", FontSize = 11, TextColor = Color.FromArgb("#64748B") };

        var typeFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#EFF6FF"), CornerRadius = 8,
            Padding = new Thickness(8, 3), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = doc.TypeIcon, FontSize = 22 }
        };

        var deleteBtn = new Label { Text = "🗑", FontSize = 20, VerticalOptions = LayoutOptions.Center };
        var docCopy = doc;
        deleteBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                bool confirm = await DisplayAlert("Supprimer", $"Supprimer « {docCopy.OriginalName} » ?", "Oui", "Non");
                if (!confirm) return;
                var ok = await _api.DeleteDocumentAsync(docCopy.Id, _patientId);
                if (ok) { _docs.Remove(docCopy); RefreshList(); }
                else await DisplayAlert("Erreur", "Impossible de supprimer ce fichier.", "OK");
            })
        });

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 12
        };
        row.Children.Add(typeFrame);
        var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { nameLabel, metaLabel } };
        Grid.SetColumn(info, 1); row.Children.Add(info);
        Grid.SetColumn(deleteBtn, 2); row.Children.Add(deleteBtn);

        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 14,
            Padding = new Thickness(14, 12), HasShadow = false,
            BorderColor = Color.FromArgb("#DBEAFE"), Content = row
        };
    }

    private async Task PickAndUpload()
    {
        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Choisir un document médical",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/pdf", "image/*", "application/msword",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
                    { DevicePlatform.iOS, new[] { "public.pdf", "public.image", "com.microsoft.word.doc" } },
                    { DevicePlatform.WinUI, new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" } },
                })
            };

            var result = await FilePicker.PickAsync(options);
            if (result == null) return;

            // Category picker dialog
            var category = await DisplayActionSheet("Catégorie du document", "Annuler", null, Categories.ToArray());
            if (category == "Annuler" || category == null) return;

            // Show uploading state
            _saveIndicator(true);

            var contentType = result.ContentType ?? "application/octet-stream";
            var uploaded = await _api.UploadDocumentAsync(_patientId, result.FullPath, result.FileName, contentType, category);

            _saveIndicator(false);

            if (uploaded != null) { _docs.Insert(0, uploaded); RefreshList(); }
            else await DisplayAlert("Erreur", "Échec de l'envoi du fichier.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }

    private void _saveIndicator(bool show)
    {
        // No-op — upload is fast enough; the list refresh confirms success
    }

    private static string CategoryIcon(string cat) => cat switch
    {
        "Analyse" => "🔬", "Scanner" => "📡", "Radio" => "☢️", "Rapport" => "📄", _ => "📎"
    };
}

// ═══════════════════════════════════════════════════════
// PATIENT E-ORDONNANCES PAGE
// ═══════════════════════════════════════════════════════
public class PatientEOrdonnancesPage : ContentPage
{
    private readonly ApiService _api;

    public PatientEOrdonnancesPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0FDF4");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPrescriptions();
    }

    private async Task LoadPrescriptions()
    {
        Content = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#059669"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

        var patientId = Preferences.Get("ProfileId", "");
        List<PrescriptionDto> prescriptions = new();
        if (!string.IsNullOrEmpty(patientId) && Guid.TryParse(patientId, out var pid))
            prescriptions = await _api.GetPatientPrescriptionsAsync(pid);

        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // ── HEADER ──────────────────────────────────────────────────
        var header = new Grid { Padding = new Thickness(20, 52, 20, 28) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#064E3B"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#059669"), Offset = 1f }
            }
        };
        var back = new Label { Text = "‹", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        header.Children.Add(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                back,
                new Label { Text = "E-Ordonnances", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = $"{prescriptions.Count} ordonnance(s) électronique(s)", FontSize = 12, TextColor = Colors.White.WithAlpha(0.75f) }
            }
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var body = new VerticalStackLayout { Padding = new Thickness(16, 18, 16, 0), Spacing = 14 };

        if (!prescriptions.Any())
        {
            body.Children.Add(new Frame
            {
                BackgroundColor = Colors.White, CornerRadius = 20,
                Padding = new Thickness(30, 52), HasShadow = false, BorderColor = Color.FromArgb("#D1FAE5"),
                Content = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 10,
                    Children =
                    {
                        new Label { Text = "📋", FontSize = 52, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Aucune donnée disponible", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Vos ordonnances électroniques\napparaîtront ici après chaque consultation.", FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }
                    }
                }
            });
        }
        else
        {
            foreach (var p in prescriptions)
                body.Children.Add(BuildPrescriptionCard(p));
        }

        body.Children.Add(new BoxView { HeightRequest = 32, Color = Colors.Transparent });
        var ordoScroll = new ScrollView { Content = body };
        Grid.SetRow(ordoScroll, 1);
        root.Children.Add(ordoScroll);
        Content = root;
    }

    private View BuildPrescriptionCard(PrescriptionDto p)
    {
        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        headerRow.Children.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = p.DoctorName ?? "Médecin", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540") },
                new Label { Text = p.DoctorSpecialty ?? "", FontSize = 12, TextColor = Color.FromArgb("#059669"), FontAttributes = FontAttributes.Bold },
            }
        });
        var dateBadge = new Frame
        {
            BackgroundColor = Color.FromArgb("#ECFDF5"), CornerRadius = 10,
            Padding = new Thickness(10, 4), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Start,
            Content = new Label { Text = $"📅 {p.DateDisplay}", FontSize = 11, TextColor = Color.FromArgb("#059669"), FontAttributes = FontAttributes.Bold }
        };
        Grid.SetColumn(dateBadge, 1); headerRow.Children.Add(dateBadge);

        var divider = new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#D1FAE5"), Margin = new Thickness(0, 8) };

        var rxLabel = new HorizontalStackLayout { Spacing = 6, Children =
        {
            new Label { Text = "℞", FontSize = 22, TextColor = Color.FromArgb("#059669"), VerticalOptions = LayoutOptions.Center },
            new Label { Text = "Ordonnance", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#064E3B"), VerticalOptions = LayoutOptions.Center }
        }};

        var rxBox = new Frame
        {
            BackgroundColor = Color.FromArgb("#F0FDF4"), CornerRadius = 12,
            Padding = new Thickness(14, 12), HasShadow = false, BorderColor = Color.FromArgb("#A7F3D0"),
            Content = new Label { Text = p.Prescription, FontSize = 13, TextColor = Color.FromArgb("#065F46"), LineHeight = 1.5 }
        };

        var typePill = new Frame
        {
            BackgroundColor = Color.FromArgb("#ECFDF5"), CornerRadius = 8,
            Padding = new Thickness(10, 4), HasShadow = false, BorderColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label { Text = $"{p.TypeIcon} {(p.ConsultationType == "Video" ? "Consultation vidéo" : "Consultation présentielle")}", FontSize = 11, TextColor = Color.FromArgb("#059669") }
        };

        // Action buttons row
        var printBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#0D9488"), CornerRadius = 8,
            Padding = new Thickness(12, 7), HasShadow = false, BorderColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label { Text = "🖨 Imprimer", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
        };
        printBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                var html = PrescriptionPreviewPage.BuildPatientHtml(p);
                await Navigation.PushAsync(new PrescriptionPreviewPage(html,
                    $"Ordonnance_{p.DateDisplay.Replace(" ", "_")}"));
            })
        });

        var pharmBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#ECFDF5"), CornerRadius = 8,
            Padding = new Thickness(12, 7), HasShadow = false, BorderColor = Color.FromArgb("#6EE7B7"),
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label { Text = "💊 Pharmacies proches", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#059669") }
        };
        pharmBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new NearbyPharmaciesPage()))
        });

        var btnRow = new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(0, 4, 0, 0), Children = { printBtn, pharmBtn } };

        var accentBar = new BoxView { WidthRequest = 4, BackgroundColor = Color.FromArgb("#059669"), CornerRadius = 2 };
        var inner = new VerticalStackLayout { Spacing = 8, Children = { headerRow, divider, rxLabel, rxBox, typePill, btnRow } };
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 14
        };
        layout.Children.Add(accentBar);
        Grid.SetColumn(inner, 1); layout.Children.Add(inner);

        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 18,
            Padding = new Thickness(16), HasShadow = false,
            BorderColor = Color.FromArgb("#D1FAE5"), Content = layout
        };
    }
}

// ═══════════════════════════════════════════════════════
// PATIENT INSTANT CONSULTATIONS PAGE
// ═══════════════════════════════════════════════════════
public class PatientInstantConsultationsPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService? _signalR;

    public PatientInstantConsultationsPage(ApiService api, SignalRService? signalR = null)
    {
        _api = api; _signalR = signalR;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F7FA");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadConsultations();
    }

    private async Task LoadConsultations()
    {
        Content = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#4F46E5"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

        var patientId = Preferences.Get("ProfileId", "");
        List<AppointmentDto> videoAppts = new();
        if (!string.IsNullOrEmpty(patientId))
        {
            var all = await _api.GetPatientAppointmentsAsync(Guid.Parse(patientId));
            videoAppts = all.Where(a => a.ConsultationType == "Video").ToList();
        }

        var today    = videoAppts.Where(a => a.AppointmentDate.Date == DateTime.Today && a.Status is "Scheduled" or "Confirmed").OrderBy(a => a.StartTime).ToList();
        var upcoming = videoAppts.Where(a => a.AppointmentDate.Date > DateTime.Today  && a.Status is "Scheduled" or "Confirmed").OrderBy(a => a.AppointmentDate).ToList();
        var history  = videoAppts.Where(a => a.AppointmentDate.Date < DateTime.Today  || a.Status is "Completed" or "Cancelled" or "NoShow").OrderByDescending(a => a.AppointmentDate).ToList();

        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // ── HEADER ──────────────────────────────────────────────────
        var header = new Grid { Padding = new Thickness(20, 52, 20, 28) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#1E1B4B"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#4F46E5"), Offset = 1f }
            }
        };
        var back = new Label { Text = "‹", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        header.Children.Add(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                back,
                new Label { Text = "Consultations vidéo", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = $"{videoAppts.Count} consultation(s) au total", FontSize = 12, TextColor = Colors.White.WithAlpha(0.75f) }
            }
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var body = new VerticalStackLayout { Padding = new Thickness(16, 18, 16, 0), Spacing = 20 };

        // ── BOOK NEW CTA ─────────────────────────────────────────────
        var ctaFrame = new Frame
        {
            CornerRadius = 18, Padding = new Thickness(20, 18),
            HasShadow = false, BorderColor = Colors.Transparent
        };
        ctaFrame.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 0),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#4F46E5"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#7C3AED"), Offset = 1f }
            }
        };
        var bookVideoBtn = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 12,
            Padding = new Thickness(18, 11), HasShadow = false, BorderColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label { Text = "📅  Réserver une consultation vidéo", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4F46E5") }
        };
        bookVideoBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorSearchPage(_api, _signalR)))
        });
        ctaFrame.Content = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new HorizontalStackLayout { Spacing = 10, Children =
                {
                    new Label { Text = "📹", FontSize = 28, VerticalOptions = LayoutOptions.Center },
                    new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children =
                    {
                        new Label { Text = "Consultez à distance", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        new Label { Text = "Rejoignez votre médecin en vidéo", FontSize = 12, TextColor = Colors.White.WithAlpha(0.8f) }
                    }}
                }},
                bookVideoBtn
            }
        };
        body.Children.Add(ctaFrame);

        // ── TODAY ─────────────────────────────────────────────────────
        if (today.Any())
        {
            body.Children.Add(SectionLabel("🔴 Aujourd'hui"));
            foreach (var a in today) body.Children.Add(BuildCard(a, isToday: true));
        }

        // ── UPCOMING ──────────────────────────────────────────────────
        if (upcoming.Any())
        {
            body.Children.Add(SectionLabel("📅 À venir"));
            foreach (var a in upcoming) body.Children.Add(BuildCard(a, isToday: false));
        }

        // ── HISTORY ───────────────────────────────────────────────────
        if (history.Any())
        {
            body.Children.Add(SectionLabel("🕐 Historique"));
            foreach (var a in history) body.Children.Add(BuildCard(a, isToday: false));
        }

        // ── EMPTY ─────────────────────────────────────────────────────
        if (!videoAppts.Any())
        {
            var emptyBook = new Button
            {
                Text = "Réserver une consultation vidéo",
                BackgroundColor = Color.FromArgb("#4F46E5"), TextColor = Colors.White,
                CornerRadius = 14, HeightRequest = 50, FontAttributes = FontAttributes.Bold,
                FontSize = 14, Margin = new Thickness(0, 8, 0, 0)
            };
            emptyBook.Clicked += async (_, _) => await Navigation.PushAsync(new DoctorSearchPage(_api, _signalR));

            body.Children.Add(new Frame
            {
                BackgroundColor = Colors.White, CornerRadius = 20,
                Padding = new Thickness(30, 48), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
                Content = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 8,
                    Children =
                    {
                        new Label { Text = "📹", FontSize = 52, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Aucune donnée disponible", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Vous n'avez pas encore de consultations\nvidéo programmées.", FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center },
                        emptyBook
                    }
                }
            });
        }

        body.Children.Add(new BoxView { HeightRequest = 32, Color = Colors.Transparent });
        var videoScroll = new ScrollView { Content = body };
        Grid.SetRow(videoScroll, 1);
        root.Children.Add(videoScroll);
        Content = root;
    }

    private View BuildCard(AppointmentDto a, bool isToday)
    {
        var statusFr = new Dictionary<string, string>
        {
            ["Scheduled"] = "Planifié", ["Confirmed"] = "Confirmé",
            ["Completed"] = "Terminé",  ["Cancelled"] = "Annulé", ["NoShow"] = "Absent"
        };
        var statusLabel = statusFr.GetValueOrDefault(a.Status, a.Status);

        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        topRow.Children.Add(new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = a.DoctorName ?? "Médecin", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540") },
                new Label { Text = a.DoctorSpecialty ?? "", FontSize = 12, TextColor = Color.FromArgb("#4F46E5"), FontAttributes = FontAttributes.Bold },
            }
        });
        var badge = new Frame
        {
            BackgroundColor = a.StatusBg, CornerRadius = 10, Padding = new Thickness(10, 4),
            HasShadow = false, BorderColor = Colors.Transparent, VerticalOptions = LayoutOptions.Start,
            Content = new Label { Text = statusLabel, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = a.StatusColor }
        };
        Grid.SetColumn(badge, 1); topRow.Children.Add(badge);

        var metaRow = new HorizontalStackLayout { Spacing = 8 };
        metaRow.Children.Add(Pill($"📅 {a.AppointmentDate:dd MMM yyyy}", "#F0F4FF", "#4F46E5"));
        metaRow.Children.Add(Pill($"⏰ {a.StartTime:hh\\:mm}", "#F0F4FF", "#4F46E5"));

        var content = new VerticalStackLayout { Spacing = 10, Children = { topRow, metaRow } };

        if (isToday && a.DoctorUserId.HasValue && _signalR != null)
        {
            var joinBtn = new Frame
            {
                CornerRadius = 12, Padding = new Thickness(0, 12), HasShadow = false, BorderColor = Colors.Transparent,
                Content = new Label { Text = "▶  Rejoindre la consultation", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center }
            };
            joinBtn.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0), EndPoint = new Point(1, 0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#4F46E5"), Offset = 0f },
                    new GradientStop { Color = Color.FromArgb("#7C3AED"), Offset = 1f }
                }
            };
            var dId = a.DoctorUserId.Value; var dName = a.DoctorName ?? "Médecin";
            var sr = _signalR!;
            joinBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await Navigation.PushAsync(new VideoCallPage(sr, dId, dName, true))) });
            content.Children.Add(joinBtn);
        }

        var bar = new BoxView { WidthRequest = 4, BackgroundColor = Color.FromArgb("#4F46E5"), CornerRadius = 2 };
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 14
        };
        layout.Children.Add(bar);
        Grid.SetColumn(content, 1); layout.Children.Add(content);

        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 18,
            Padding = new Thickness(16), HasShadow = false,
            BorderColor = isToday ? Color.FromArgb("#C7D2FE") : Color.FromArgb("#E2E8F0"),
            Content = layout
        };
    }

    private static Label SectionLabel(string text) => new()
    {
        Text = text, FontSize = 14, FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#0F2540"), Margin = new Thickness(2, 0, 0, -6)
    };

    private static Frame Pill(string text, string bg, string fg) => new()
    {
        BackgroundColor = Color.FromArgb(bg), CornerRadius = 8,
        Padding = new Thickness(8, 4), HasShadow = false, BorderColor = Colors.Transparent,
        Content = new Label { Text = text, FontSize = 11, TextColor = Color.FromArgb(fg) }
    };
}

// ═══════════════════════════════════════════════════════
// DOCTOR SEARCH PAGE  (choose: by name OR by specialty)
// ═══════════════════════════════════════════════════════
public class DoctorSearchPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService? _signalR;

    public DoctorSearchPage(ApiService api, SignalRService? signalR = null)
    {
        _api = api; _signalR = signalR;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F5F7FF");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new VerticalStackLayout { Spacing = 0 };

        // ── HEADER ──────────────────────────────────────────────────
        var header = new Grid { Padding = new Thickness(20, 52, 20, 32) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };
        var backLbl = new Label { Text = "‹", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        backLbl.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        header.Children.Add(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                backLbl,
                new Label { Text = "Prendre un Rendez-vous", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = "Choisissez votre type de consultation", FontSize = 13, TextColor = Colors.White.WithAlpha(0.8f) }
            }
        });
        root.Children.Add(header);

        // ── 2 × 2 SERVICE GRID ──────────────────────────────────────
        var services = new (string emoji, string title, string bg, string accent, string icon, string serviceType)[]
        {
            ("🏥", "Consultation cabinet",    "#EFF6FF", "#2563EB", "🏥", "InPerson"),
            ("💻", "Téléconsultation",        "#F0FDF4", "#16A34A", "💻", "Video"),
            ("🏠", "À domicile",              "#FFF7ED", "#EA580C", "🏠", "Home"),
            ("🔬", "Analyses médicales",      "#F5F3FF", "#7C3AED", "🔬", "Analysis"),
        };

        var grid = new Grid
        {
            Padding = new Thickness(20, 24, 20, 20),
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions    = new RowDefinitionCollection    { new(GridLength.Auto), new(GridLength.Auto) },
            ColumnSpacing = 14, RowSpacing = 14
        };

        for (int i = 0; i < services.Length; i++)
        {
            var (emoji, title, bg, accent, _, svcType) = services[i];
            var card = ServiceCard(emoji, title, bg, accent);
            Grid.SetRow(card, i / 2);
            Grid.SetColumn(card, i % 2);
            var st = svcType; var ti = title;
            card.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    Preferences.Set("BookingServiceType", st);
                    await Navigation.PushAsync(new DoctorSearchModesPage(_api, _signalR, ti));
                })
            });
            grid.Children.Add(card);
        }

        root.Children.Add(grid);
        Content = root;
    }

    private static Frame ServiceCard(string emoji, string title, string bg, string accent)
    {
        return new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 22,
            Padding = new Thickness(16, 24),
            HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center, Spacing = 14,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Color.FromArgb(bg),
                        CornerRadius = 36, WidthRequest = 72, HeightRequest = 72,
                        Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                        HorizontalOptions = LayoutOptions.Center,
                        Content = new Label { Text = emoji, FontSize = 34, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    },
                    new Label
                    {
                        Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#0F2540"),
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
    }

    private static Frame ModeCard(string emoji, string title, string subtitle, string accent, string bg, Func<Task> onTap)
    {
        var accentColor = Color.FromArgb(accent);
        var card = new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 20, Padding = new Thickness(20), HasShadow = false,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
                ColumnSpacing = 16,
                Children =
                {
                    new Frame
                    {
                        WidthRequest = 58, HeightRequest = 58, CornerRadius = 18,
                        BackgroundColor = Color.FromArgb(bg),
                        BorderColor = Colors.Transparent, HasShadow = false, Padding = 0,
                        Content = new Label { Text = emoji, FontSize = 28, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    }
                }
            }
        };
        var grid = (Grid)card.Content;
        var textStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center,
            Children = {
                new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") },
                new Label { Text = subtitle, FontSize = 12, TextColor = Color.FromArgb("#64748B"), LineBreakMode = LineBreakMode.WordWrap }
            }};
        Grid.SetColumn(textStack, 1); grid.Children.Add(textStack);
        var arrow = new Label { Text = "›", FontSize = 30, TextColor = accentColor, VerticalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold };
        Grid.SetColumn(arrow, 2); grid.Children.Add(arrow);
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await onTap()) });
        return card;
    }
}

// ═══════════════════════════════════════════════════════
// DOCTOR SEARCH MODES PAGE  (step 2 after service choice)
// ═══════════════════════════════════════════════════════
public class DoctorSearchModesPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService? _signalR;
    private readonly string _serviceName;

    private static readonly Dictionary<string, (string emoji, string bg, string accent)> ServiceStyles = new()
    {
        ["InPerson"]  = ("🏥", "#EFF6FF", "#2563EB"),
        ["Video"]     = ("💻", "#F0FDF4", "#16A34A"),
        ["Home"]      = ("🏠", "#FFF7ED", "#EA580C"),
        ["Analysis"]  = ("🔬", "#F5F3FF", "#7C3AED"),
    };

    public DoctorSearchModesPage(ApiService api, SignalRService? signalR, string serviceName)
    {
        _api = api; _signalR = signalR; _serviceName = serviceName;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F5F7FF");
        BuildUI();
    }

    private void BuildUI()
    {
        var svcType = Preferences.Get("BookingServiceType", "InPerson");
        var (emoji, bg, accent) = ServiceStyles.GetValueOrDefault(svcType, ("🏥", "#EFF6FF", "#2563EB"));

        var root = new VerticalStackLayout { Spacing = 0 };

        // ── HEADER ──────────────────────────────────────────────────
        var header = new Grid { Padding = new Thickness(20, 52, 20, 28) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };
        var back = new Label { Text = "‹", FontSize = 28, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        // Service pill badge
        var pill = new Frame
        {
            BackgroundColor = Colors.White.WithAlpha(0.18f), CornerRadius = 20,
            Padding = new Thickness(14, 6), HasShadow = false, BorderColor = Colors.White.WithAlpha(0.35f),
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label { Text = $"{emoji}  {_serviceName}", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
        };

        header.Children.Add(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                back,
                new Label { Text = "Choisir un médecin", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = "Comment voulez-vous trouver votre médecin ?", FontSize = 13, TextColor = Colors.White.WithAlpha(0.8f) },
                pill
            }
        });
        root.Children.Add(header);

        // ── SEARCH MODE CARDS ────────────────────────────────────────
        var body = new VerticalStackLayout { Padding = new Thickness(20, 24), Spacing = 16 };

        body.Children.Add(ModeCard(
            "🔍", "Rechercher par nom",
            "Trouvez un médecin en tapant son nom ou prénom",
            accent, bg,
            async () => await Navigation.PushAsync(new DoctorSearchByNamePage(_api, _signalR))
        ));

        body.Children.Add(ModeCard(
            "🩺", "Rechercher par spécialité",
            "Parcourez les spécialités et choisissez votre ville",
            accent, bg,
            async () => await Navigation.PushAsync(new SpecialtySelectionPage(_api))
        ));

        body.Children.Add(new BoxView { HeightRequest = 20, Color = Colors.Transparent });
        root.Children.Add(body);
        Content = root;
    }

    private static Frame ModeCard(string emoji, string title, string subtitle, string accent, string bg, Func<Task> onTap)
    {
        var card = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 20,
            Padding = new Thickness(20), HasShadow = false,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
                ColumnSpacing = 16,
                Children =
                {
                    new Frame
                    {
                        WidthRequest = 60, HeightRequest = 60, CornerRadius = 18,
                        BackgroundColor = Color.FromArgb(bg),
                        BorderColor = Colors.Transparent, HasShadow = false, Padding = 0,
                        Content = new Label { Text = emoji, FontSize = 28, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    }
                }
            }
        };
        var grid = (Grid)card.Content;
        var text = new VerticalStackLayout
        {
            Spacing = 4, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") },
                new Label { Text = subtitle, FontSize = 12, TextColor = Color.FromArgb("#64748B"), LineBreakMode = LineBreakMode.WordWrap }
            }
        };
        var arrow = new Label { Text = "›", FontSize = 30, TextColor = Color.FromArgb(accent), VerticalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold };
        Grid.SetColumn(text, 1); grid.Children.Add(text);
        Grid.SetColumn(arrow, 2); grid.Children.Add(arrow);
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await onTap()) });
        return card;
    }
}

// ═══════════════════════════════════════════════════════
// SEARCH BY NAME PAGE
// ═══════════════════════════════════════════════════════
public class DoctorSearchByNamePage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService? _signalR;
    private VerticalStackLayout _results = new();

    public DoctorSearchByNamePage(ApiService api, SignalRService? signalR = null)
    {
        _api = api; _signalR = signalR;
        Title = "Recherche par nom";
        BackgroundColor = Color.FromArgb("#F5F7FF");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        var header = new Grid { BackgroundColor = Color.FromArgb("#1E1B4B"), Padding = new Thickness(20, 52, 20, 20) };
        var hStack = new VerticalStackLayout { Spacing = 10 };
        var backRow = new HorizontalStackLayout { Spacing = 10 };
        var back = new Label { Text = "‹", FontSize = 26, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        backRow.Children.Add(back);
        backRow.Children.Add(new Label { Text = "Rechercher un médecin", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center });
        hStack.Children.Add(backRow);

        // Search box inside header
        var searchEntry = new Entry
        {
            Placeholder = "Nom ou prénom du médecin...",
            BackgroundColor = Colors.Transparent, HeightRequest = 48,
            TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#94A3B8"), FontSize = 14
        };
        var searchBox = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 14, Padding = new Thickness(12, 0),
            HasShadow = false, BorderColor = Colors.Transparent,
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
                ColumnSpacing = 8, Padding = 0,
                Children =
                {
                    new Label { Text = "🔍", FontSize = 18, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#94A3B8") }
                }
            }
        };
        var searchGrid = (Grid)searchBox.Content;
        Grid.SetColumn(searchEntry, 1); searchGrid.Children.Add(searchEntry);
        hStack.Children.Add(searchBox);
        header.Children.Add(hStack);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        _results = new VerticalStackLayout { Padding = new Thickness(16, 12), Spacing = 12 };
        _results.Children.Add(new Label
        {
            Text = "Commencez à taper pour rechercher...",
            FontSize = 14, TextColor = Color.FromArgb("#94A3B8"),
            HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 20)
        });

        var searchScroll = new ScrollView { Content = _results };
        Grid.SetRow(searchScroll, 1);
        root.Children.Add(searchScroll);
        Content = root;

        searchEntry.TextChanged += async (s, e) =>
        {
            var query = e.NewTextValue?.Trim() ?? "";
            if (query.Length < 2) { _results.Children.Clear(); return; }
            var doctors = await _api.GetDoctorsAsync(search: query);
            ShowResults(doctors);
        };
    }

    private void ShowResults(List<DoctorListDto> doctors)
    {
        _results.Children.Clear();
        if (!doctors.Any())
        {
            _results.Children.Add(new Label { Text = "Aucun médecin trouvé.", FontSize = 14, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 20) });
            return;
        }
        foreach (var doc in doctors)
        {
            var card = DoctorCard(doc);
            _results.Children.Add(card);
        }
    }

    private View DoctorCard(DoctorListDto doc)
    {
        var initials = doc.FullName.Replace("Dr. ", "").Split(' ').Take(2)
            .Aggregate("", (a, w) => a + (w.Length > 0 ? char.ToUpper(w[0]).ToString() : ""));
        var accentColor = Color.FromArgb(doc.SpecialtyColor ?? "#4F46E5");

        var card = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 18, Padding = new Thickness(16),
            HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
                ColumnSpacing = 14,
                Children =
                {
                    new Frame
                    {
                        WidthRequest = 52, HeightRequest = 52, CornerRadius = 16,
                        BackgroundColor = accentColor.WithAlpha(0.12f), BorderColor = accentColor.WithAlpha(0.3f),
                        HasShadow = false, Padding = 0,
                        Content = new Label { Text = initials, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = accentColor, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    }
                }
            }
        };
        var grid = (Grid)card.Content;
        var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = doc.FullName, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") },
                new Label { Text = doc.SpecialtyName, FontSize = 12, TextColor = accentColor },
                new Label { Text = $"⭐ {doc.Rating:F1}  •  {doc.ConsultationFee} MAD  •  {doc.City ?? "—"}", FontSize = 11, TextColor = Color.FromArgb("#64748B") }
            }};
        Grid.SetColumn(info, 1); grid.Children.Add(info);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                var detail = await _api.GetDoctorByIdAsync(doc.Id);
                if (detail != null) await Navigation.PushAsync(new DoctorProfilePage(_api, _signalR, detail));
            })
        });
        return card;
    }
}

// ═══════════════════════════════════════════════════════
// CITY SELECTION PAGE  (after specialty)
// ═══════════════════════════════════════════════════════
public class CitySelectionPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Guid _specialtyId;
    private readonly string _specialtyName;

    private static readonly string[] MoroccanCities =
    {
        "Casablanca", "Rabat", "Fès", "Marrakech", "Agadir", "Tanger", "Meknès",
        "Oujda", "Kénitra", "Tétouan", "Safi", "El Jadida", "Nador", "Beni Mellal",
        "Mohammedia", "Khémisset", "Settat", "Khouribga", "Berrechid", "Salé"
    };

    public CitySelectionPage(ApiService api, Guid specialtyId, string specialtyName)
    {
        _api = api; _specialtyId = specialtyId; _specialtyName = specialtyName;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F5F7FF");
        BuildUI();
    }

    private void BuildUI()
    {
        var stack = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#F5F7FF") };

        // ── HEADER ──────────────────────────────────────────────────
        var header = new Grid { BackgroundColor = Color.FromArgb("#1E1B4B"), Padding = new Thickness(20, 52, 20, 28) };
        var hStack = new VerticalStackLayout { Spacing = 6 };
        var backRow = new HorizontalStackLayout { Spacing = 10 };
        var back = new Label { Text = "‹", FontSize = 26, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        backRow.Children.Add(back);
        backRow.Children.Add(new Label { Text = _specialtyName, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center });
        hStack.Children.Add(backRow);
        hStack.Children.Add(new Label { Text = "Choisissez votre ville", FontSize = 13, TextColor = Color.FromArgb("#A5B4FC") });
        header.Children.Add(hStack);
        stack.Children.Add(header);

        // ── CITY LIST ───────────────────────────────────────────────
        var body = new VerticalStackLayout { Padding = new Thickness(16, 16), Spacing = 10 };

        foreach (var city in MoroccanCities)
        {
            var cityName = city;
            var cityCard = new Frame
            {
                BackgroundColor = Colors.White, CornerRadius = 14,
                Padding = new Thickness(18, 14), HasShadow = false,
                BorderColor = Color.FromArgb("#E2E8F0"),
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
                    ColumnSpacing = 14,
                    Children =
                    {
                        new Label { Text = "📍", FontSize = 20, VerticalOptions = LayoutOptions.Center },
                    }
                }
            };
            var cGrid = (Grid)cityCard.Content;
            var cityLbl = new Label { Text = cityName, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A"), VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(cityLbl, 1); cGrid.Children.Add(cityLbl);
            var arrow = new Label { Text = "›", FontSize = 24, TextColor = Color.FromArgb("#94A3B8"), VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(arrow, 2); cGrid.Children.Add(arrow);

            cityCard.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    var page = new DoctorListPage(_api) { SpecialtyId = _specialtyId, CityFilter = cityName };
                    await Navigation.PushAsync(page);
                })
            });
            body.Children.Add(cityCard);
        }

        body.Children.Add(new BoxView { HeightRequest = 32, Color = Colors.Transparent });
        stack.Children.Add(body);

        Content = new ScrollView { Content = stack };
    }
}

// ═══════════════════════════════════════════════════════
// DOCTOR PROFILE PAGE  (Doctori.ma style)
// ═══════════════════════════════════════════════════════
public class DoctorProfilePage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService? _signalR;
    private readonly DoctorDetailDto _doc;
    private readonly VerticalStackLayout _reviewsStack = new() { Spacing = 10 };
    private bool _reviewsLoaded;

    private static readonly Color Teal  = Color.FromArgb("#4A8B9E");
    private static readonly Color Navy  = Color.FromArgb("#1E2D4A");

    public DoctorProfilePage(ApiService api, SignalRService? signalR, DoctorDetailDto doc)
    {
        _api = api; _signalR = signalR; _doc = doc;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.White;
        BuildUI();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_reviewsLoaded) { _reviewsLoaded = true; await LoadReviewsAsync(); }
    }

    private async Task LoadReviewsAsync()
    {
        _reviewsStack.Children.Clear();
        _reviewsStack.Children.Add(new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#126B82"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8) });
        var reviews = await _api.GetDoctorReviewsAsync(_doc.Id);
        _reviewsStack.Children.Clear();
        if (!reviews.Any())
        {
            _reviewsStack.Children.Add(new Label { Text = "Aucun avis pour ce médecin.", FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8) });
            return;
        }
        foreach (var r in reviews.Take(5))
        {
            _reviewsStack.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#F8FAFC"), CornerRadius = 10, Padding = new Thickness(12),
                HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new HorizontalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = r.StarsDisplay, FontSize = 14 },
                                new Label { Text = r.PatientName, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), VerticalOptions = LayoutOptions.Center }
                            }
                        },
                        string.IsNullOrEmpty(r.Comment)
                            ? (View)new BoxView { HeightRequest = 0 }
                            : new Label { Text = r.Comment, FontSize = 12, TextColor = Color.FromArgb("#374151"), LineBreakMode = LineBreakMode.WordWrap },
                        new Label { Text = r.TimeDisplay, FontSize = 11, TextColor = Color.FromArgb("#94A3B8") }
                    }
                }
            });
        }
    }

    private void BuildUI()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // ── HEADER BAND (navy) ────────────────────────────────────────
        var header = new Grid { BackgroundColor = Color.FromArgb("#1E1B4B"), Padding = new Thickness(20, 52, 20, 24) };
        var hStack = new VerticalStackLayout { Spacing = 14 };

        var backRow = new HorizontalStackLayout { Spacing = 10 };
        var backLbl = new Label { Text = "‹", FontSize = 26, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        backLbl.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        backRow.Children.Add(backLbl);
        backRow.Children.Add(new Label { Text = "Profil du médecin", FontSize = 16, TextColor = Color.FromArgb("#A5B4FC"), VerticalOptions = LayoutOptions.Center });
        hStack.Children.Add(backRow);

        // Doctor identity card
        var initials = _doc.FullName.Replace("Dr. ", "").Split(' ').Take(2)
            .Aggregate("", (a, w) => a + (w.Length > 0 ? char.ToUpper(w[0]).ToString() : ""));
        var accentColor = Color.FromArgb(_doc.SpecialtyColor ?? "#4F46E5");

        var idRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 16
        };
        idRow.Children.Add(new Frame
        {
            WidthRequest = 70, HeightRequest = 70, CornerRadius = 22,
            BackgroundColor = accentColor.WithAlpha(0.2f), BorderColor = accentColor.WithAlpha(0.5f),
            HasShadow = false, Padding = 0,
            Content = new Label { Text = initials, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        });
        var nameStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        nameStack.Children.Add(new Label { Text = _doc.FullName, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
        nameStack.Children.Add(new Label { Text = _doc.SpecialtyName, FontSize = 13, TextColor = Color.FromArgb("#A5B4FC") });
        nameStack.Children.Add(new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = $"⭐ {_doc.Rating:F1} ({_doc.TotalReviews} avis)", FontSize = 12, TextColor = Color.FromArgb("#FCD34D") },
                new Label { Text = $"• {_doc.YearsOfExperience} ans", FontSize = 12, TextColor = Color.FromArgb("#A5B4FC") }
            }
        });
        Grid.SetColumn(nameStack, 1); idRow.Children.Add(nameStack);
        hStack.Children.Add(idRow);
        header.Children.Add(hStack);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // ── BOOK BUTTON (teal bar, sticky-style) ─────────────────────
        var bookBar = new Frame
        {
            BackgroundColor = Teal, CornerRadius = 0, Padding = new Thickness(20, 16),
            HasShadow = false, BorderColor = Colors.Transparent,
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
                ColumnSpacing = 12,
                Children =
                {
                    new Label { Text = "📅", FontSize = 22, VerticalOptions = LayoutOptions.Center }
                }
            }
        };
        var bookGrid = (Grid)bookBar.Content;
        var bookLbl = new Label { Text = "Prendre Rendez-vous en ligne", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(bookLbl, 1); bookGrid.Children.Add(bookLbl);
        bookBar.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
                await Navigation.PushAsync(new BookAppointmentPage(_api) { DoctorId = _doc.Id }))
        });
        Grid.SetRow(bookBar, 1);
        root.Children.Add(bookBar);

        // ── BODY (white cards) ────────────────────────────────────────
        var body = new VerticalStackLayout { Padding = new Thickness(16, 16), Spacing = 14, BackgroundColor = Color.FromArgb("#F8FAFC") };

        // Infos pratiques card
        var infoCard = SectionCard();
        var infoStack = (VerticalStackLayout)infoCard.Content;

        // Address
        if (!string.IsNullOrEmpty(_doc.Address) || !string.IsNullOrEmpty(_doc.City))
        {
            infoStack.Children.Add(InfoRow("📍", "Adresse", _doc.Address ?? _doc.City ?? ""));
            infoStack.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#F1F5F9"), Margin = new Thickness(0, 4) });
        }

        // Fee + payment
        infoStack.Children.Add(new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            Children =
            {
                new VerticalStackLayout { Spacing = 4, Children =
                {
                    new Label { Text = "Modes de règlement", FontSize = 12, TextColor = Color.FromArgb("#64748B"), FontAttributes = FontAttributes.Bold },
                    new HorizontalStackLayout { Spacing = 6, Children =
                    {
                        new Frame { BackgroundColor = Color.FromArgb("#E0F2FE"), BorderColor = Colors.Transparent, CornerRadius = 6, Padding = new Thickness(8, 4), HasShadow = false, Content = new Label { Text = "💵 Espèces", FontSize = 11, TextColor = Teal } },
                        new Frame { BackgroundColor = Color.FromArgb("#E0F2FE"), BorderColor = Colors.Transparent, CornerRadius = 6, Padding = new Thickness(8, 4), HasShadow = false, Content = new Label { Text = "💳 CB", FontSize = 11, TextColor = Teal } }
                    }}
                }}
            }
        });
        var feeStack = new VerticalStackLayout { Spacing = 4 };
        feeStack.Children.Add(new Label { Text = "Tarif de consultation", FontSize = 12, TextColor = Color.FromArgb("#64748B"), FontAttributes = FontAttributes.Bold });
        feeStack.Children.Add(new Label { Text = $"{_doc.ConsultationFee} Dh", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Navy });
        var feeGrid = (Grid)((VerticalStackLayout)((Grid)infoStack.Children.Last()).Children.First()).Parent;

        // Add fee to the grid
        var practicalGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 12
        };
        var payStack = new VerticalStackLayout { Spacing = 4,
            Children =
            {
                new Label { Text = "Modes de règlement", FontSize = 12, TextColor = Color.FromArgb("#64748B"), FontAttributes = FontAttributes.Bold },
                new HorizontalStackLayout { Spacing = 6, Children =
                {
                    new Frame { BackgroundColor = Color.FromArgb("#E0F2FE"), BorderColor = Colors.Transparent, CornerRadius = 6, Padding = new Thickness(8, 4), HasShadow = false, Content = new Label { Text = "💵 Espèces", FontSize = 11, TextColor = Teal } },
                    new Frame { BackgroundColor = Color.FromArgb("#E0F2FE"), BorderColor = Colors.Transparent, CornerRadius = 6, Padding = new Thickness(8, 4), HasShadow = false, Content = new Label { Text = "💳 CB", FontSize = 11, TextColor = Teal } }
                }}
            }};
        practicalGrid.Children.Add(payStack);
        Grid.SetColumn(feeStack, 1); practicalGrid.Children.Add(feeStack);
        infoStack.Children.Clear();
        if (!string.IsNullOrEmpty(_doc.Address) || !string.IsNullOrEmpty(_doc.City))
        {
            infoStack.Children.Add(InfoRow("📍", "Adresse:", _doc.Address ?? _doc.City ?? ""));
            infoStack.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#F1F5F9"), Margin = new Thickness(0, 6) });
        }
        infoStack.Children.Add(practicalGrid);

        // Specialty tags
        if (!string.IsNullOrEmpty(_doc.SpecialtyName))
        {
            infoStack.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#F1F5F9"), Margin = new Thickness(0, 6) });
            infoStack.Children.Add(new Label { Text = "Spécialités:", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Navy });
            var tagRow = new HorizontalStackLayout { Spacing = 8 };
            tagRow.Children.Add(new Frame
            {
                BackgroundColor = Navy, BorderColor = Colors.Transparent, CornerRadius = 20,
                Padding = new Thickness(14, 6), HasShadow = false,
                Content = new Label { Text = _doc.SpecialtyName, FontSize = 12, TextColor = Colors.White }
            });
            infoStack.Children.Add(tagRow);
        }

        body.Children.Add(infoCard);

        // Map card
        if (_doc.Latitude.HasValue && _doc.Longitude.HasValue)
        {
            var mapCard = SectionCard("Géolocalisation:");
            var mapContent = (VerticalStackLayout)mapCard.Content;
            var mapBtn = new Frame
            {
                HeightRequest = 140, CornerRadius = 12, Padding = 0, HasShadow = false,
                BackgroundColor = Color.FromArgb("#E0F2FE"), BorderColor = Color.FromArgb("#BAE6FD"),
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 8,
                    Children =
                    {
                        new Label { Text = "🗺️", FontSize = 40, HorizontalOptions = LayoutOptions.Center },
                        new Frame
                        {
                            BackgroundColor = Teal, BorderColor = Colors.Transparent, CornerRadius = 10,
                            Padding = new Thickness(20, 10), HasShadow = false,
                            HorizontalOptions = LayoutOptions.Center,
                            Content = new Label { Text = "Cliquez pour afficher la carte", FontSize = 13, TextColor = Colors.White, FontAttributes = FontAttributes.Bold }
                        }
                    }
                }
            };
            var lat = _doc.Latitude.Value; var lon = _doc.Longitude.Value;
            mapBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    var url = $"https://www.google.com/maps/search/?api=1&query={lat},{lon}";
                    await Launcher.OpenAsync(new Uri(url));
                })
            });
            mapContent.Children.Add(mapBtn);
            body.Children.Add(mapCard);
        }

        // Biography
        if (!string.IsNullOrEmpty(_doc.Biography))
        {
            var bioCard = SectionCard("Présentation du cabinet:");
            ((VerticalStackLayout)bioCard.Content).Children.Add(
                new Label { Text = _doc.Biography, FontSize = 13, TextColor = Color.FromArgb("#374151"), LineBreakMode = LineBreakMode.WordWrap });
            body.Children.Add(bioCard);
        }

        // Diplomas
        if (_doc.DiplomaList.Count > 0)
        {
            var dipCard = SectionCard("Diplôme et formation:");
            var dipStack = (VerticalStackLayout)dipCard.Content;
            foreach (var dip in _doc.DiplomaList)
                dipStack.Children.Add(new Label { Text = dip, FontSize = 13, TextColor = Color.FromArgb("#374151"), LineBreakMode = LineBreakMode.WordWrap });
            body.Children.Add(dipCard);
        }

        // Languages
        if (_doc.LanguageList.Count > 0)
        {
            var langCard = SectionCard("Langues Parlées:");
            var langStack = (VerticalStackLayout)langCard.Content;
            foreach (var lang in _doc.LanguageList)
                langStack.Children.Add(new Label { Text = $"• {lang}", FontSize = 13, TextColor = Color.FromArgb("#374151") });
            body.Children.Add(langCard);
        }

        // Reviews section
        var reviewsCard = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 16, Padding = new Thickness(16),
            HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            new Label { Text = "⭐ Avis patients", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E2D4A") },
                            new Label { Text = $"({_doc.TotalReviews} avis)", FontSize = 12, TextColor = Color.FromArgb("#64748B"), VerticalOptions = LayoutOptions.Center }
                        }
                    },
                    _reviewsStack
                }
            }
        };
        body.Children.Add(reviewsCard);

        // Bottom spacing
        body.Children.Add(new BoxView { HeightRequest = 20, BackgroundColor = Colors.Transparent });
        var profileScroll = new ScrollView { Content = body };
        Grid.SetRow(profileScroll, 2);
        root.Children.Add(profileScroll);
        Content = root;
    }

    private static Frame SectionCard(string? title = null)
    {
        var inner = new VerticalStackLayout { Spacing = 10 };
        if (title != null) inner.Children.Add(new Label { Text = title, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E2D4A") });
        return new Frame { BackgroundColor = Colors.White, CornerRadius = 16, Padding = new Thickness(16), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"), Content = inner };
    }

    private static View InfoRow(string icon, string label, string value) =>
        new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = label, FontSize = 12, TextColor = Color.FromArgb("#64748B"), FontAttributes = FontAttributes.Bold },
                new HorizontalStackLayout { Spacing = 6, Children =
                {
                    new Label { Text = icon, FontSize = 14 },
                    new Label { Text = value, FontSize = 13, TextColor = Color.FromArgb("#1E2D4A"), LineBreakMode = LineBreakMode.WordWrap }
                }}
            }
        };
}

// ═══════════════════════════════════════════════════════
// SPECIALTY SELECTION PAGE
// ═══════════════════════════════════════════════════════
public class SpecialtySelectionPage : ContentPage
{
    private readonly ApiService _api;

    public SpecialtySelectionPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F7FA");
        BuildUI();
    }

    private void BuildUI()
    {
        var specialties = new List<(string Emoji, string Name, Guid Id, string Color)>
        {
            ("❤️", "Cardiologie",        Guid.Parse("11111111-0000-0000-0000-000000000001"), "#FEE2E2"),
            ("🧴", "Dermatologie",       Guid.Parse("11111111-0000-0000-0000-000000000002"), "#FEF3C7"),
            ("🧠", "Neurologie",         Guid.Parse("11111111-0000-0000-0000-000000000003"), "#EDE9FE"),
            ("🦴", "Orthopédie",         Guid.Parse("11111111-0000-0000-0000-000000000004"), "#DBEAFE"),
            ("👶", "Pédiatrie",          Guid.Parse("11111111-0000-0000-0000-000000000005"), "#D1FAE5"),
            ("🩺", "Généraliste",        Guid.Parse("11111111-0000-0000-0000-000000000006"), "#CCFBF1"),
            ("👁️", "Ophtalmologie",     Guid.Parse("11111111-0000-0000-0000-000000000007"), "#FFEDD5"),
            ("🌸", "Gynécologie",        Guid.Parse("11111111-0000-0000-0000-000000000008"), "#FCE7F3"),
            ("🧘", "Psychiatrie",        Guid.Parse("11111111-0000-0000-0000-000000000009"), "#F1F5F9"),
            ("🔬", "Radiologie",         Guid.Parse("11111111-0000-0000-0000-000000000010"), "#F5F5F4"),
            ("🦷", "Dentaire",           Guid.Parse("11111111-0000-0000-0000-000000000011"), "#FFF9C4"),
            ("🫃", "Gastrologie",        Guid.Parse("11111111-0000-0000-0000-000000000012"), "#E8F5E9"),
            ("💪", "Kinésithérapie",     Guid.Parse("11111111-0000-0000-0000-000000000013"), "#FBE9E7"),
            ("💼", "Méd. du travail",    Guid.Parse("11111111-0000-0000-0000-000000000014"), "#E3F2FD"),
            ("🦋", "Endocrinologie",     Guid.Parse("11111111-0000-0000-0000-000000000015"), "#F3E5F5"),
            ("👂", "ORL",               Guid.Parse("11111111-0000-0000-0000-000000000016"), "#FFF3E0"),
            ("🫁", "Pneumologie",        Guid.Parse("11111111-0000-0000-0000-000000000017"), "#E0F7FA"),
            ("🦿", "Rhumatologie",       Guid.Parse("11111111-0000-0000-0000-000000000018"), "#FAFAFA"),
        };

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // Header
        var specHeader = new Grid { Padding = new Thickness(20, 52, 20, 28) };
        specHeader.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };
        var shRow = new HorizontalStackLayout { Spacing = 12 };
        var shBack = new Label { Text = "‹", FontSize = 26, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        shBack.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        shRow.Children.Add(shBack);
        var shText = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        shText.Children.Add(new Label { Text = "Choisir une spécialité", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
        shText.Children.Add(new Label { Text = "Sélectionnez votre domaine médical", FontSize = 12, TextColor = Colors.White.WithAlpha(0.75f) });
        shRow.Children.Add(shText);
        specHeader.Children.Add(shRow);
        Grid.SetRow(specHeader, 0);
        outerGrid.Children.Add(specHeader);

        var innerStack = new VerticalStackLayout { Padding = new Thickness(16, 16), Spacing = 12 };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12,
            RowSpacing = 12
        };

        for (int i = 0; i < (specialties.Count + 1) / 2; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < specialties.Count; i++)
        {
            var (emoji, name, id, color) = specialties[i];
            var card = new Frame
            {
                BackgroundColor = Color.FromArgb(color),
                CornerRadius = 16,
                Padding = new Thickness(16),
                HasShadow = false,
                BorderColor = Colors.Transparent,
                Content = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 8,
                    Children =
                    {
                        new Label { Text = emoji, FontSize = 36, HorizontalOptions = LayoutOptions.Center },
                        new Label
                        {
                            Text = name, FontSize = 13, FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#1E293B"),
                            HorizontalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                }
            };
            var specialtyId = id;
            var specialtyName = name;
            card.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                    await Navigation.PushAsync(new CitySelectionPage(_api, specialtyId, specialtyName)))
            });
            Grid.SetRow(card, i / 2);
            Grid.SetColumn(card, i % 2);
            grid.Children.Add(card);
        }

        innerStack.Children.Add(grid);
        var scrollView = new ScrollView { Content = innerStack };
        Grid.SetRow(scrollView, 1);
        outerGrid.Children.Add(scrollView);
        Content = outerGrid;
    }
}

public class DoctorListPage : ContentPage
{
    private readonly ApiService _api;
    public Guid? SpecialtyId { get; set; }
    public string? CityFilter { get; set; }

    private List<DoctorListDto> _allDoctors = new();
    private VerticalStackLayout? _listStack;
    private string _activeSort = "rating"; // rating, priceAsc, priceDesc, distAsc, distDesc

    public DoctorListPage(ApiService api)
    {
        _api = api;
        Title = "Choisir un Médecin";
        BackgroundColor = Color.FromArgb("#F0F4FF");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDoctors();
    }

    private async Task LoadDoctors()
    {
        var loading = new ActivityIndicator
        {
            IsRunning = true,
            Color = Color.FromArgb("#4F46E5"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 60)
        };
        Content = new VerticalStackLayout { Children = { loading } };

        _allDoctors = await _api.GetDoctorsAsync(SpecialtyId, city: CityFilter);

        // Try geolocation for distance calculation
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync()
                ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Low));
            if (location != null)
            {
                foreach (var d in _allDoctors)
                {
                    if (d.Latitude.HasValue && d.Longitude.HasValue)
                    {
                        var dist = Location.CalculateDistance(
                            location.Latitude, location.Longitude,
                            d.Latitude.Value, d.Longitude.Value,
                            DistanceUnits.Kilometers);
                        d.DistanceKm = Math.Round(dist, 1);
                    }
                }
            }
        }
        catch { /* geolocation not available */ }

        BuildPage();
    }

    private void BuildPage()
    {
        // Filter bar
        var filterScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Margin = new Thickness(12, 10, 12, 0)
        };

        var filterRow = new HorizontalStackLayout { Spacing = 8 };
        var filters = new[]
        {
            ("rating",    "⭐ Meilleure note"),
            ("priceAsc",  "💰 Prix croissant"),
            ("priceDesc", "💰 Prix décroissant"),
            ("distAsc",   "📍 Plus proche"),
            ("distDesc",  "📍 Plus loin"),
        };
        foreach (var (key, label) in filters)
        {
            var k = key;
            var btn = new Frame
            {
                CornerRadius = 20,
                Padding = new Thickness(14, 7),
                HasShadow = false,
                BackgroundColor = _activeSort == key ? Color.FromArgb("#4F46E5") : Colors.White,
                BorderColor = _activeSort == key ? Color.FromArgb("#4F46E5") : Color.FromArgb("#C7D2FE")
            };
            btn.Content = new Label
            {
                Text = label,
                FontSize = 13,
                FontAttributes = _activeSort == key ? FontAttributes.Bold : FontAttributes.None,
                TextColor = _activeSort == key ? Colors.White : Color.FromArgb("#4F46E5")
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => { _activeSort = k; BuildPage(); })
            });
            filterRow.Children.Add(btn);
        }
        filterScroll.Content = filterRow;

        // Sort doctors
        var sorted = _activeSort switch
        {
            "priceAsc"  => _allDoctors.OrderBy(d => d.ConsultationFee).ToList(),
            "priceDesc" => _allDoctors.OrderByDescending(d => d.ConsultationFee).ToList(),
            "distAsc"   => _allDoctors.OrderBy(d => d.DistanceKm ?? double.MaxValue).ToList(),
            "distDesc"  => _allDoctors.OrderByDescending(d => d.DistanceKm ?? -1).ToList(),
            _           => _allDoctors.OrderByDescending(d => d.Rating).ToList()
        };

        _listStack = new VerticalStackLayout { Padding = new Thickness(12, 10, 12, 20), Spacing = 12 };

        foreach (var doc in sorted)
            _listStack.Children.Add(BuildDoctorCard(doc));

        if (!sorted.Any())
            _listStack.Children.Add(new Label
            {
                Text = "Aucun médecin trouvé",
                FontSize = 16,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40)
            });

        var pageStack = new VerticalStackLayout
        {
            Children =
            {
                filterScroll,
                new ScrollView { Content = _listStack }
            }
        };
        Content = pageStack;
    }

    private View BuildDoctorCard(DoctorListDto doc)
    {
        // Avatar circle
        var initials = doc.FullName.Replace("Dr. ", "").Split(' ');
        var avatarText = initials.Length >= 2
            ? $"{initials[0][0]}{initials[1][0]}"
            : doc.FullName[..Math.Min(2, doc.FullName.Length)];
        var specialtyColor = Color.FromArgb(doc.SpecialtyColor ?? "#4F46E5");

        var avatar = new Frame
        {
            WidthRequest = 56, HeightRequest = 56,
            CornerRadius = 28, Padding = 0,
            BackgroundColor = specialtyColor,
            BorderColor = Colors.Transparent, HasShadow = false,
            Content = new Label
            {
                Text = avatarText.ToUpper(),
                FontSize = 18, FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        // Specialty badge
        var badge = new Frame
        {
            CornerRadius = 10, Padding = new Thickness(8, 3),
            BackgroundColor = specialtyColor.WithAlpha(0.12f),
            BorderColor = Colors.Transparent, HasShadow = false,
            Content = new Label
            {
                Text = doc.SpecialtyName,
                FontSize = 11, FontAttributes = FontAttributes.Bold,
                TextColor = specialtyColor
            }
        };

        // Location row
        var locationRow = new HorizontalStackLayout { Spacing = 4 };
        if (!string.IsNullOrEmpty(doc.LocationDisplay))
        {
            locationRow.Children.Add(new Label { Text = "🏥", FontSize = 11 });
            locationRow.Children.Add(new Label { Text = doc.LocationDisplay, FontSize = 11, TextColor = Color.FromArgb("#64748B") });
        }
        if (doc.DistanceKm.HasValue)
        {
            locationRow.Children.Add(new Label { Text = "  📍", FontSize = 11 });
            locationRow.Children.Add(new Label { Text = doc.DistanceDisplay, FontSize = 11, TextColor = Color.FromArgb("#4F46E5"), FontAttributes = FontAttributes.Bold });
        }

        var infoStack = new VerticalStackLayout
        {
            Spacing = 5,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                new Label { Text = doc.FullName, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") },
                badge,
                new HorizontalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        new Label { Text = doc.RatingDisplay, FontSize = 12, TextColor = Color.FromArgb("#F59E0B") },
                        new Label { Text = $"({doc.TotalReviews})", FontSize = 12, TextColor = Color.FromArgb("#94A3B8") },
                        new Label { Text = doc.ExperienceDisplay, FontSize = 12, TextColor = Color.FromArgb("#64748B") }
                    }
                },
                locationRow
            }
        };

        var feeStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = $"{doc.ConsultationFee:F0}",
                    FontSize = 18, FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#4F46E5"),
                    HorizontalOptions = LayoutOptions.End
                },
                new Label
                {
                    Text = "MAD/séance",
                    FontSize = 10, TextColor = Color.FromArgb("#94A3B8"),
                    HorizontalOptions = LayoutOptions.End
                },
                new Frame
                {
                    CornerRadius = 8, Padding = new Thickness(10, 5),
                    BackgroundColor = Color.FromArgb("#4F46E5"), BorderColor = Colors.Transparent, HasShadow = false,
                    Content = new Label { Text = "Réserver", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
                }
            }
        };

        var cardGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };
        cardGrid.Children.Add(avatar);
        Grid.SetColumn(infoStack, 1);
        cardGrid.Children.Add(infoStack);
        Grid.SetColumn(feeStack, 2);
        cardGrid.Children.Add(feeStack);

        var card = new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 18,
            Padding = new Thickness(14),
            HasShadow = true,
            BorderColor = Color.FromArgb("#E0E7FF"),
            Content = cardGrid
        };

        var docId = doc.Id;
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                var detail = await _api.GetDoctorByIdAsync(docId);
                if (detail != null)
                    await Navigation.PushAsync(new DoctorProfilePage(_api, null, detail));
            })
        });
        return card;
    }
}

// ═══════════════════════════════════════════════════════
// RATING PAGE
// ═══════════════════════════════════════════════════════
public class RatingPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AppointmentDto _appointment;
    private int _selectedRating;
    private readonly Label[] _stars = new Label[5];
    private Editor? _commentEditor;

    public RatingPage(ApiService api, AppointmentDto appointment)
    {
        _api = api; _appointment = appointment;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F7FA");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        var header = new Grid { Padding = new Thickness(20, 52, 20, 28) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };
        var hRow = new HorizontalStackLayout { Spacing = 12 };
        var back = new Label { Text = "‹", FontSize = 26, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        hRow.Children.Add(back);
        hRow.Children.Add(new Label { Text = "Évaluer le médecin", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center });
        header.Children.Add(hRow);
        Grid.SetRow(header, 0); root.Children.Add(header);

        var scroll = new ScrollView();
        var body = new VerticalStackLayout { Padding = new Thickness(20), Spacing = 16 };

        // Doctor info
        body.Children.Add(new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 16, Padding = new Thickness(16),
            HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = _appointment.DoctorName ?? "Médecin", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540") },
                    new Label { Text = _appointment.DoctorSpecialty ?? "", FontSize = 13, TextColor = Color.FromArgb("#126B82") },
                    new Label { Text = $"Consultation du {_appointment.AppointmentDate:dd MMM yyyy}", FontSize = 12, TextColor = Color.FromArgb("#64748B") }
                }
            }
        });

        // Star rating
        var starsRow = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
        for (int i = 0; i < 5; i++)
        {
            var starLbl = new Label { Text = "☆", FontSize = 44, TextColor = Color.FromArgb("#D1D5DB") };
            _stars[i] = starLbl;
            var idx = i + 1;
            starLbl.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SetRating(idx)) });
            starsRow.Children.Add(starLbl);
        }

        var ratingLabels = new[] { "", "Mauvais", "Passable", "Bien", "Très bien", "Excellent" };
        var ratingHint = new Label { Text = "Sélectionnez une note", FontSize = 13, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center };

        body.Children.Add(new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 16, Padding = new Thickness(16),
            HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 14, HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    new Label { Text = "Votre note *", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), HorizontalOptions = LayoutOptions.Center },
                    starsRow,
                    ratingHint
                }
            }
        });

        // Store ratingHint reference via closure for SetRating
        _ratingHint = ratingHint;
        _ratingLabels = ratingLabels;

        // Comment
        _commentEditor = new Editor
        {
            Placeholder = "Partagez votre expérience (optionnel)...",
            HeightRequest = 100, BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#0F2540"), FontSize = 14
        };
        body.Children.Add(new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 16, Padding = new Thickness(16),
            HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Votre avis (optionnel)", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540") },
                    _commentEditor
                }
            }
        });

        // Submit button
        var submitBtn = new Button
        {
            Text = "Envoyer l'évaluation",
            BackgroundColor = Color.FromArgb("#126B82"), TextColor = Colors.White,
            FontSize = 16, FontAttributes = FontAttributes.Bold,
            CornerRadius = 16, HeightRequest = 56, Margin = new Thickness(0, 8)
        };
        submitBtn.Clicked += async (_, _) => await SubmitAsync(submitBtn);
        body.Children.Add(submitBtn);

        scroll.Content = body;
        Grid.SetRow(scroll, 1); root.Children.Add(scroll);
        Content = root;
    }

    private Label? _ratingHint;
    private string[]? _ratingLabels;

    private void SetRating(int rating)
    {
        _selectedRating = rating;
        for (int i = 0; i < 5; i++)
        {
            _stars[i].Text = i < rating ? "⭐" : "☆";
            _stars[i].TextColor = i < rating ? Color.FromArgb("#F59E0B") : Color.FromArgb("#D1D5DB");
        }
        if (_ratingHint != null && _ratingLabels != null)
            _ratingHint.Text = _ratingLabels[rating];
    }

    private async Task SubmitAsync(Button btn)
    {
        if (_selectedRating == 0) { await DisplayAlert("", "Veuillez sélectionner une note.", "OK"); return; }
        btn.IsEnabled = false; btn.Text = "Envoi...";
        var ok = await _api.SubmitReviewAsync(_appointment.Id, _selectedRating, _commentEditor?.Text?.Trim());
        if (ok) { await DisplayAlert("✅ Merci!", "Votre évaluation a été envoyée avec succès.", "OK"); await Navigation.PopAsync(); }
        else { await DisplayAlert("Erreur", "Impossible d'envoyer l'évaluation. Avez-vous déjà évalué ce médecin?", "OK"); }
        btn.IsEnabled = true; btn.Text = "Envoyer l'évaluation";
    }
}

public class BookAppointmentPage : ContentPage
{
    private readonly ApiService _api;
    private readonly string? _presetType;
    public Guid DoctorId { get; set; }
    private TimeSlotDto? _selectedSlot;
    private DateTime _selectedDate = DateTime.Today;
    private VerticalStackLayout? _slotsStack;
    private Editor? _detailsEntry;
    private Button? _bookButton;
    private Label? _selectedSlotLabel;
    private Label? _doctorNameLabel;
    private Label? _doctorSpecLabel;
    private Label? _doctorFeeLabel;
    private Label? _doctorLocationLabel;
    private Image? _doctorImage;
    private CheckBox? _returningPatient;
    private bool _uiBuilt;
    private string? _selectedServiceType;
    private string? _selectedMotif;
    private readonly List<(Frame Card, string Bg, string Accent)> _serviceCards = new();
    private readonly List<Frame> _motifChips = new();
    private FlexLayout? _motifFlex;

    private static readonly Dictionary<string, string[]> SpecialtyMotifs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cardiologie"]    = new[] { "Douleur thoracique", "Palpitations", "Hypertension artérielle", "Essoufflement", "Malaise ou perte de connaissance", "Suivi cardiaque" },
        ["Dermatologie"]   = new[] { "Acné", "Eczéma", "Chute de cheveux", "Allergies cutanées", "Taches ou boutons suspects", "Mycoses / démangeaisons" },
        ["Neurologie"]     = new[] { "Maux de tête / migraines", "Vertiges", "Troubles de mémoire", "Crises d'épilepsie", "Engourdissements", "Tremblements" },
        ["Orthopédie"]     = new[] { "Douleurs articulaires", "Fracture", "Entorse", "Mal de dos", "Blessure sportive", "Difficulté à marcher" },
        ["Pédiatrie"]      = new[] { "Fièvre chez l'enfant", "Vaccination", "Toux / rhume", "Troubles de croissance", "Douleurs abdominales", "Contrôle de routine" },
        ["Généraliste"]    = new[] { "Consultation générale", "Fatigue", "Fièvre", "Infection", "Bilan de santé", "Orientation vers spécialiste" },
        ["Ophtalmologie"]  = new[] { "Vision floue", "Douleur oculaire", "Rougeur des yeux", "Contrôle de la vue", "Cataracte", "Port de lunettes/lentilles" },
        ["Gynécologie"]    = new[] { "Suivi de grossesse", "Douleurs pelviennes", "Troubles menstruels", "Contraception", "Infection gynécologique", "Dépistage / frottis" },
        ["Psychiatrie"]    = new[] { "Anxiété", "Dépression", "Stress", "Troubles du sommeil", "Attaques de panique", "Troubles du comportement" },
        ["Radiologie"]     = new[] { "Radiographie", "Échographie", "Scanner", "IRM", "Douleur à explorer", "Contrôle après blessure" },
        ["Dentaire"]       = new[] { "Rage de dent", "Carie", "Détartrage", "Sensibilité dentaire", "Gencives qui saignent", "Extraction dentaire" },
        ["Gastrologie"]    = new[] { "Douleurs abdominales", "Reflux gastrique", "Ballonnements", "Constipation", "Diarrhée", "Nausées / vomissements" },
    };

    private static readonly string[] DefaultMotifs = { "Consultation générale", "Suivi médical", "Urgence", "Bilan de santé", "Renouvellement d'ordonnance", "Autre" };

    // These 2 appear first for every specialty
    private static readonly string[] CommonMotifs = { "Première consultation", "Consultation de suivi" };

    private static readonly (string Emoji, string Name, string Bg, string Accent, string Type)[] ServiceTypes =
    {
        ("🏥", "Consultation cabinet", "#EFF6FF", "#2563EB", "InPerson"),
        ("💻", "Téléconsultation",     "#F0FDF4", "#16A34A", "Video"),
        ("🏠", "À domicile",           "#FFF7ED", "#EA580C", "Home"),
        ("🔬", "Analyses médicales",   "#F5F3FF", "#7C3AED", "Analysis"),
    };

    public BookAppointmentPage(ApiService api)
    {
        _api = api;
        _presetType = Preferences.Get("BookingServiceType", "");
        Preferences.Remove("BookingServiceType");
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F7FA");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_uiBuilt) { BuildUI(); _uiBuilt = true; }
        _ = LoadDoctorDetails();
        _ = LoadSlots(_selectedDate);
    }

    private void BuildUI()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // ── Header ──
        var header = new Grid { Padding = new Thickness(20, 52, 20, 28) };
        header.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0A3D4A"), Offset = 0f },
                new GradientStop { Color = Color.FromArgb("#1A8FA8"), Offset = 1f }
            }
        };
        var hRow = new HorizontalStackLayout { Spacing = 12 };
        var back = new Label { Text = "‹", FontSize = 26, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        hRow.Children.Add(back);
        hRow.Children.Add(new Label { Text = "Prendre Rendez-vous", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center });
        header.Children.Add(hRow);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var scroll = new ScrollView();
        var stack = new VerticalStackLayout { Padding = new Thickness(16, 16), Spacing = 14 };

        // ── Doctor summary card ──
        _doctorImage = new Image
        {
            WidthRequest = 60, HeightRequest = 60,
            Aspect = Aspect.AspectFill,
            Source = "doctor_placeholder.png"
        };
        var doctorImageFrame = new Frame
        {
            WidthRequest = 60, HeightRequest = 60, CornerRadius = 30,
            Padding = 0, IsClippedToBounds = true, HasShadow = false,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = _doctorImage
        };
        _doctorNameLabel = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540"), Text = "Chargement..." };
        _doctorSpecLabel = new Label { FontSize = 13, TextColor = Color.FromArgb("#126B82"), Text = "" };
        _doctorFeeLabel  = new Label { FontSize = 13, TextColor = Color.FromArgb("#059669"), Text = "" };
        _doctorLocationLabel = new Label { FontSize = 12, TextColor = Color.FromArgb("#64748B"), Text = "" };
        var doctorInfo = new VerticalStackLayout
        {
            Spacing = 3, VerticalOptions = LayoutOptions.Center,
            Children = { _doctorNameLabel, _doctorSpecLabel, _doctorFeeLabel, _doctorLocationLabel }
        };
        var doctorRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
                { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 14
        };
        doctorRow.Children.Add(doctorImageFrame);
        Grid.SetColumn(doctorInfo, 1);
        doctorRow.Children.Add(doctorInfo);
        stack.Children.Add(BookSectionCard("👨‍⚕️ Médecin", doctorRow));

        // ── Date picker ──
        var datePicker = new DatePicker
        {
            MinimumDate = DateTime.Today,
            TextColor = Color.FromArgb("#0F2540"),
            BackgroundColor = Colors.Transparent
        };
        datePicker.DateSelected += (s, e) =>
        {
            _selectedDate = e.NewDate;
            _selectedSlot = null;
            UpdateSlotLabel();
            _ = LoadSlots(_selectedDate);
        };
        stack.Children.Add(BookSectionCard("📅 Choisir une date", datePicker));

        // ── Time slots ──
        _slotsStack = new VerticalStackLayout { Spacing = 8 };
        _selectedSlotLabel = new Label
        {
            FontSize = 13, TextColor = Color.FromArgb("#126B82"),
            FontAttributes = FontAttributes.Bold, IsVisible = false,
            HorizontalOptions = LayoutOptions.Center
        };
        stack.Children.Add(BookSectionCard("⏰ Créneaux disponibles",
            new VerticalStackLayout { Spacing = 8, Children = { _slotsStack, _selectedSlotLabel } }));

        // ── Consultation form card ──
        var formStack = new VerticalStackLayout { Spacing = 16 };

        // Motif de la consultation (chips, populated after doctor loads)
        _motifFlex = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
            AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Start
        };
        _motifFlex.Add(new Label
        {
            Text = "Chargement des motifs...",
            FontSize = 12, TextColor = Color.FromArgb("#94A3B8"),
            Margin = new Thickness(4, 2)
        });
        formStack.Children.Add(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Motif de la consultation *", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") },
                _motifFlex
            }
        });

        // Service type visual card grid (2×2)
        formStack.Children.Add(new Label { Text = "Type de consultation *", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") });

        var svcGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions    = new RowDefinitionCollection    { new(GridLength.Auto), new(GridLength.Auto) },
            ColumnSpacing = 12, RowSpacing = 12
        };

        _serviceCards.Clear();
        for (int idx = 0; idx < ServiceTypes.Length; idx++)
        {
            var (emoji, name, bg, accent, svcType) = ServiceTypes[idx];
            bool isPreset = _presetType == svcType;
            if (isPreset) _selectedServiceType = svcType;

            var card = new Frame
            {
                BackgroundColor = isPreset ? Color.FromArgb(bg) : Colors.White,
                CornerRadius = 16, Padding = new Thickness(10, 16),
                HasShadow = false,
                BorderColor = isPreset ? Color.FromArgb(accent) : Color.FromArgb("#E2E8F0"),
                Content = new VerticalStackLayout
                {
                    Spacing = 10, HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Frame
                        {
                            WidthRequest = 56, HeightRequest = 56, CornerRadius = 28,
                            BackgroundColor = Color.FromArgb(bg),
                            BorderColor = Colors.Transparent, HasShadow = false, Padding = 0,
                            HorizontalOptions = LayoutOptions.Center,
                            Content = new Label { Text = emoji, FontSize = 26, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                        },
                        new Label
                        {
                            Text = name, FontSize = 12, FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#0F2540"),
                            HorizontalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                }
            };

            _serviceCards.Add((card, bg, accent));
            var capturedIdx = idx;
            card.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => SelectServiceCard(capturedIdx))
            });

            Grid.SetRow(card, idx / 2);
            Grid.SetColumn(card, idx % 2);
            svcGrid.Children.Add(card);
        }
        formStack.Children.Add(svcGrid);

        // Renseignements médicaux
        _detailsEntry = new Editor
        {
            Placeholder = "Décrivez vos symptômes, antécédents ou informations utiles pour le médecin...",
            HeightRequest = 100,
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            TextColor = Color.FromArgb("#0F2540"),
            FontSize = 13,
            AutoSize = EditorAutoSizeOption.TextChanges
        };
        formStack.Children.Add(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = "Renseignements médicaux", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") },
                new Frame { BackgroundColor = Color.FromArgb("#F8FAFC"), CornerRadius = 12, Padding = new Thickness(8, 4), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"), Content = _detailsEntry }
            }
        });

        // Returning patient checkbox
        _returningPatient = new CheckBox { Color = Color.FromArgb("#126B82") };
        var checkRow = new HorizontalStackLayout
        {
            Spacing = 10, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                _returningPatient,
                new Label { Text = "Je suis déjà patient de ce médecin", FontSize = 13, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center }
            }
        };
        formStack.Children.Add(checkRow);

        stack.Children.Add(BookSectionCard("📋 Détails de la consultation", formStack));

        // ── Confirm button ──
        _bookButton = new Button
        {
            Text = "Confirmer le rendez-vous",
            BackgroundColor = Color.FromArgb("#126B82"),
            TextColor = Colors.White, FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 16, HeightRequest = 58,
            IsEnabled = false, Margin = new Thickness(0, 4)
        };
        _bookButton.Clicked += BookButton_Clicked;
        stack.Children.Add(_bookButton);
        stack.Children.Add(new BoxView { HeightRequest = 40, Color = Colors.Transparent });

        scroll.Content = stack;
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        Content = root;
    }

    private static Frame BookSectionCard(string? title, View content)
    {
        var inner = new VerticalStackLayout { Spacing = 12 };
        if (title != null)
            inner.Children.Add(new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F2540") });
        inner.Children.Add(content);
        return new Frame { BackgroundColor = Colors.White, CornerRadius = 18, Padding = new Thickness(16), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"), Content = inner };
    }

    private void UpdateSlotLabel()
    {
        if (_selectedSlotLabel == null) return;
        if (_selectedSlot == null) { _selectedSlotLabel.IsVisible = false; return; }
        _selectedSlotLabel.Text = $"✅ Créneau sélectionné : {_selectedSlot.DisplayTime}";
        _selectedSlotLabel.IsVisible = true;
    }

    private void SelectServiceCard(int selectedIdx)
    {
        _selectedServiceType = ServiceTypes[selectedIdx].Type;
        for (int i = 0; i < _serviceCards.Count; i++)
        {
            var (card, bg, accent) = _serviceCards[i];
            if (i == selectedIdx)
            {
                card.BackgroundColor = Color.FromArgb(bg);
                card.BorderColor     = Color.FromArgb(accent);
            }
            else
            {
                card.BackgroundColor = Colors.White;
                card.BorderColor     = Color.FromArgb("#E2E8F0");
            }
        }
        UpdateBookButton();
    }

    private void UpdateBookButton()
    {
        if (_bookButton != null)
            _bookButton.IsEnabled = _selectedSlot != null && _selectedServiceType != null && _selectedMotif != null;
    }

    private async Task LoadDoctorDetails()
    {
        if (DoctorId == Guid.Empty) return;
        var doctor = await _api.GetDoctorByIdAsync(DoctorId);
        if (doctor == null) return;
        if (_doctorNameLabel != null) _doctorNameLabel.Text = doctor.FullName;
        if (_doctorSpecLabel != null) _doctorSpecLabel.Text = doctor.SpecialtyName;
        if (_doctorFeeLabel  != null) _doctorFeeLabel.Text  = doctor.FeeDisplay;
        if (_doctorLocationLabel != null) _doctorLocationLabel.Text = doctor.LocationDisplay;
        if (_doctorImage != null && !string.IsNullOrEmpty(doctor.ProfileImageUrl))
            _doctorImage.Source = ImageSource.FromUri(new Uri(doctor.ProfileImageUrl));
        PopulateMotifs(doctor.SpecialtyName);
    }

    private void PopulateMotifs(string specialtyName)
    {
        if (_motifFlex == null) return;
        _motifFlex.Clear();
        _motifChips.Clear();
        _selectedMotif = null;

        var specific = SpecialtyMotifs.TryGetValue(specialtyName, out var found) ? found : DefaultMotifs;
        var motifs = CommonMotifs.Concat(specific);

        foreach (var motif in motifs)
        {
            var chip = new Frame
            {
                BackgroundColor = Colors.White,
                BorderColor     = Color.FromArgb("#CBD5E1"),
                CornerRadius    = 20,
                Padding         = new Thickness(14, 7),
                HasShadow       = false,
                Margin          = new Thickness(0, 0, 8, 8),
                Content         = new Label
                {
                    Text      = motif,
                    FontSize  = 12,
                    TextColor = Color.FromArgb("#374151")
                }
            };
            var capturedMotif = motif;
            var capturedChip  = chip;
            chip.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    _selectedMotif = capturedMotif;
                    foreach (var c in _motifChips)
                    {
                        c.BackgroundColor = Colors.White;
                        c.BorderColor     = Color.FromArgb("#CBD5E1");
                        ((Label)c.Content).TextColor = Color.FromArgb("#374151");
                    }
                    capturedChip.BackgroundColor = Color.FromArgb("#E0F4F8");
                    capturedChip.BorderColor     = Color.FromArgb("#126B82");
                    ((Label)capturedChip.Content).TextColor = Color.FromArgb("#126B82");
                    UpdateBookButton();
                })
            });
            _motifChips.Add(chip);
            _motifFlex.Add(chip);
        }
    }

    private async Task LoadSlots(DateTime date)
    {
        if (_slotsStack == null) return;
        _slotsStack.Children.Clear();
        _slotsStack.Children.Add(new ActivityIndicator
        {
            IsRunning = true, Color = Color.FromArgb("#126B82"),
            HorizontalOptions = LayoutOptions.Center
        });

        var slots = await _api.GetAvailableSlotsAsync(DoctorId, date);
        _slotsStack.Children.Clear();

        if (!slots.Any())
        {
            _slotsStack.Children.Add(new Label
            {
                Text = "Aucun créneau disponible pour ce jour.",
                TextColor = Color.FromArgb("#94A3B8"),
                FontSize = 13, HorizontalOptions = LayoutOptions.Center
            });
            return;
        }

        var slotsGrid = new Grid
        {
            ColumnSpacing = 8, RowSpacing = 8,
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        for (int i = 0; i < (slots.Count + 2) / 3; i++)
            slotsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var isToday = _selectedDate.Date == DateTime.Today;
        var nowTime = DateTime.Now.TimeOfDay;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            bool isPast = isToday && slot.StartTime <= nowTime;

            Color bg, border, textColor, subColor;
            string statusLabel;
            bool canTap;

            if (isPast)
            {
                bg = Color.FromArgb("#F3F4F6"); border = Color.FromArgb("#E5E7EB");
                textColor = Color.FromArgb("#9CA3AF"); subColor = Color.FromArgb("#9CA3AF");
                statusLabel = "Passé"; canTap = false;
            }
            else if (!slot.IsAvailable)
            {
                bg = Color.FromArgb("#F9FAFB"); border = Color.FromArgb("#E2E8F0");
                textColor = Color.FromArgb("#9CA3AF"); subColor = Color.FromArgb("#9CA3AF");
                statusLabel = "Réservé"; canTap = false;
            }
            else
            {
                bg = Color.FromArgb("#E0F4F8"); border = Color.FromArgb("#7ECFDF");
                textColor = Color.FromArgb("#126B82"); subColor = Color.FromArgb("#1A8FA8");
                statusLabel = "Libre"; canTap = true;
            }

            var slotFrame = new Frame
            {
                BackgroundColor = bg, BorderColor = border, CornerRadius = 12,
                Padding = new Thickness(8, 10), HasShadow = false,
                Opacity = isPast ? 0.5 : 1.0,
                Content = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center, Spacing = 4,
                    Children =
                    {
                        new Label { Text = slot.DisplayTime, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = textColor, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = statusLabel, FontSize = 10, TextColor = subColor, HorizontalOptions = LayoutOptions.Center }
                    }
                }
            };

            if (canTap)
            {
                var capturedSlot = slot;
                slotFrame.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        _selectedSlot = capturedSlot;
                        UpdateSlotLabel();
                        UpdateBookButton();
                    })
                });
            }

            Grid.SetRow(slotFrame, i / 3);
            Grid.SetColumn(slotFrame, i % 3);
            slotsGrid.Children.Add(slotFrame);
        }
        _slotsStack.Children.Add(slotsGrid);
    }

    private async void BookButton_Clicked(object? sender, EventArgs e)
    {
        if (_selectedSlot == null)
        {
            await DisplayAlert("", "Veuillez sélectionner un créneau horaire.", "OK");
            return;
        }
        if (_selectedServiceType == null)
        {
            await DisplayAlert("", "Veuillez choisir le type de consultation.", "OK");
            return;
        }

        var typeName    = ServiceTypes.FirstOrDefault(s => s.Type == _selectedServiceType).Name ?? "Consultation";
        var details     = _detailsEntry?.Text?.Trim() ?? "";
        var motifPart   = _selectedMotif ?? typeName;
        var reason      = string.IsNullOrEmpty(details) ? $"{typeName} — {motifPart}" : $"{typeName} — {motifPart} — {details}";
        var consultType = _selectedServiceType!;

        if (_bookButton != null) { _bookButton.IsEnabled = false; _bookButton.Text = "Confirmation..."; }
        try
        {
            var result = await _api.BookAppointmentAsync(DoctorId, _selectedDate, _selectedSlot.StartTime, reason, consultType);
            if (result.Success)
            {
                // Save reminder info in preferences for in-app reminder check
                Preferences.Set("NextApptDateTime", _selectedDate.Date.Add(_selectedSlot.StartTime).ToString("o"));
                Preferences.Set("NextApptDoctor", _doctorNameLabel?.Text ?? "votre médecin");
                AppointmentReminderHelper.ScheduleAndroidReminder(
                    result.AppointmentId, _selectedDate.Date.Add(_selectedSlot.StartTime),
                    _doctorNameLabel?.Text ?? "votre médecin");

                await DisplayAlert("✅ Réservé!", "Votre rendez-vous a été confirmé avec succès!\n\n⏰ Un rappel vous sera envoyé 1h avant.", "Parfait!");
                await Navigation.PopToRootAsync();
            }
            else
                await DisplayAlert("Erreur", result.Message ?? "Impossible de réserver", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Erreur", ex.Message, "OK"); }
        finally
        {
            if (_bookButton != null) { _bookButton.IsEnabled = true; _bookButton.Text = "Confirmer le rendez-vous"; }
        }
    }
}

// ===== DOCTOR PAGES =====
public class DoctorDashboardPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;
    private Guid _doctorId;

    // Agenda state
    private DateTime _agendaWeekStart;
    private List<AppointmentDto> _weekAppts = new();
    private Grid _agendaGrid = new();
    private Label _monthLabel = new();

    // Drawer
    private ScrollView _mainScroll = new() { BackgroundColor = Color.FromArgb("#F2F5FA") };
    private View _drawerPanel = new ContentView();
    private BoxView _overlay = new();
    private bool _isDrawerOpen;
    private const double DrawerW = 270.0;

    public DoctorDashboardPage(ApiService api, SignalRService signalR)
    {
        _api = api;
        _signalR = signalR;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        _agendaWeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        NavigationPage.SetHasNavigationBar(this, false);
        InitShell();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _signalR.IncomingCallReceived += OnIncomingCall;
        _signalR.NotificationReceived += OnChatNotification;
        await LoadDashboard();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _signalR.IncomingCallReceived -= OnIncomingCall;
        _signalR.NotificationReceived -= OnChatNotification;
    }

    private void OnIncomingCall(IncomingCallDto call)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var page = new VideoCallPage(_signalR, Guid.Parse(call.CallerId), call.CallerName, call.IsVideo, isIncoming: true, sessionId: call.SessionId);
            await Navigation.PushModalAsync(new NavigationPage(page));
        });
    }

    private void OnChatNotification(NotificationDto notif)
    {
        if (notif.Type != "Chat" || string.IsNullOrEmpty(notif.SenderId)) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool open = await Application.Current!.MainPage!.DisplayAlert(
                "💬 " + notif.Title, notif.Message, "Ouvrir", "Plus tard");
            if (open && Guid.TryParse(notif.SenderId, out var senderGuid))
                await Navigation.PushAsync(new ChatPage(_api, _signalR, senderGuid, notif.Title.Replace("New message from ", "")));
        });
    }

    // ── Drawer shell ─────────────────────────────────────────────
    private void InitShell()
    {
        _mainScroll = new ScrollView { BackgroundColor = Color.FromArgb("#F2F5FA") };

        _overlay = new BoxView { BackgroundColor = Color.FromArgb("#80000000"), IsVisible = false, Opacity = 0 };
        var overlayTap = new TapGestureRecognizer();
        overlayTap.Tapped += (_, _) => _ = CloseDrawerAsync();
        _overlay.GestureRecognizers.Add(overlayTap);

        _drawerPanel = BuildDrawerPanel();
        _drawerPanel.TranslationX = -DrawerW;
        _drawerPanel.WidthRequest = DrawerW;
        _drawerPanel.HorizontalOptions = LayoutOptions.Start;
        _drawerPanel.VerticalOptions = LayoutOptions.Fill;

        // Single-cell Grid stacks children as layers (Z-order = insertion order)
        var root = new Grid();
        root.Children.Add(_mainScroll);   // bottom: main content
        root.Children.Add(_overlay);      // middle: dim overlay
        root.Children.Add(_drawerPanel);  // top: sliding drawer
        Content = root;
    }

    private View BuildDrawerPanel()
    {
        var fullName  = Preferences.Get("FullName", "Docteur");
        var specialty = Preferences.Get("Specialty", "Médecin");
        var initials  = string.Join("", fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(2).Select(w => char.ToUpper(w[0]).ToString()));

        var closeBtn = new Label { Text = "✕", FontSize = 20, TextColor = Colors.White.WithAlpha(0.8f), HorizontalOptions = LayoutOptions.End };
        closeBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await CloseDrawerAsync()) });

        var avatarFrame = new Frame
        {
            WidthRequest = 72, HeightRequest = 72, CornerRadius = 36,
            BackgroundColor = Color.FromArgb("#0F766E"), BorderColor = Colors.White.WithAlpha(0.5f),
            HasShadow = false, Padding = 0, HorizontalOptions = LayoutOptions.Start,
            Content = new Label { Text = initials, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        var editBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#0F766E"), CornerRadius = 8,
            Padding = new Thickness(12, 6), HasShadow = false, BorderColor = Colors.White.WithAlpha(0.25f),
            HorizontalOptions = LayoutOptions.Start,
            Content = new HorizontalStackLayout { Spacing = 6, Children = { new Label { Text = "✏", FontSize = 12, TextColor = Colors.White }, new Label { Text = "Modifier mon profil", FontSize = 12, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center } } }
        };
        editBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => { await CloseDrawerAsync(); await Navigation.PushAsync(new DoctorProfileSetupPage(ServiceHelper.GetService<ApiService>())); }) });

        var profileSection = new VerticalStackLayout
        {
            BackgroundColor = Color.FromArgb("#0D9488"),
            Padding = new Thickness(20, 54, 20, 24), Spacing = 10,
            Children = { closeBtn, avatarFrame, new Label { Text = "Dr. " + fullName, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }, new Label { Text = specialty, FontSize = 13, TextColor = Colors.White.WithAlpha(0.8f) }, editBtn }
        };

        var navStack = new VerticalStackLayout { BackgroundColor = Colors.White, Spacing = 0, Padding = new Thickness(0, 8, 0, 0) };
        var navItems = new (string icon, string label, Func<Task> action)[]
        {
            ("🏠", "Tableau de bord",  async () => await CloseDrawerAsync()),
            ("📅", "Agenda",           async () => { await CloseDrawerAsync(); await Navigation.PushAsync(new DoctorCalendarPage(_api)); }),
            ("📋", "Rendez-vous",      async () => { await CloseDrawerAsync(); await Navigation.PushAsync(new DoctorAppointmentsListPage(_api)); }),
            ("💺", "Salle d'attente",  async () => { await CloseDrawerAsync(); await Navigation.PushAsync(new DoctorWorkflowPage(_api)); }),
            ("👥", "Patients",         async () => { await CloseDrawerAsync(); await Navigation.PushAsync(new DoctorPatientsPage(_api)); }),
            ("📄", "E-ordonnance",     async () => { await CloseDrawerAsync(); await Navigation.PushAsync(new DoctorOrdonnancesPage(_api)); }),
        };
        foreach (var (icon, label, action) in navItems)
        {
            var a = action;
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection { new(48), new(GridLength.Star) },
                Padding = new Thickness(12, 0), MinimumHeightRequest = 52
            };
            row.Children.Add(new Label { Text = icon, FontSize = 18, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center });
            var lbl = new Label { Text = label, FontSize = 15, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(lbl, 1); row.Children.Add(lbl);
            row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _ = a()) });
            navStack.Children.Add(row);
            navStack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6") });
        }

        var logoutRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(48), new(GridLength.Star) },
            Padding = new Thickness(12, 0), MinimumHeightRequest = 52, BackgroundColor = Colors.White
        };
        logoutRow.Children.Add(new Label { Text = "🚪", FontSize = 18, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center });
        var logoutLbl = new Label { Text = "Déconnexion", FontSize = 15, TextColor = Color.FromArgb("#EF4444"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(logoutLbl, 1); logoutRow.Children.Add(logoutLbl);
        logoutRow.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                await CloseDrawerAsync();
                Preferences.Clear();
                Application.Current!.MainPage = new NavigationPage(new RoleSelectorPage(ServiceHelper.GetService<ApiService>()));
            })
        });

        var navScroll = new ScrollView { Content = navStack };
        var panel = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            BackgroundColor = Colors.White
        };
        panel.Children.Add(profileSection);
        Grid.SetRow(navScroll, 1); panel.Children.Add(navScroll);
        Grid.SetRow(logoutRow, 2); panel.Children.Add(logoutRow);
        return panel;
    }

    private async Task OpenDrawerAsync()
    {
        if (_isDrawerOpen) return;
        _isDrawerOpen = true;
        _overlay.IsVisible = true;
        await Task.WhenAll(
            _drawerPanel.TranslateTo(0, 0, 220, Easing.CubicOut),
            _overlay.FadeTo(1, 220)
        );
    }

    private async Task CloseDrawerAsync()
    {
        if (!_isDrawerOpen) return;
        _isDrawerOpen = false;
        await Task.WhenAll(
            _drawerPanel.TranslateTo(-DrawerW, 0, 180, Easing.CubicIn),
            _overlay.FadeTo(0, 180)
        );
        _overlay.IsVisible = false;
    }

    private async Task LoadDashboard()
    {
        var spinner = new ActivityIndicator
        {
            IsRunning = true, Color = Color.FromArgb("#0D9488"),
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        };
        _mainScroll.Content = new Grid { Children = { spinner } };

        var today = DateTime.Today;
        try { _weekAppts = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, _agendaWeekStart); } catch { }

        var todayAppts = _weekAppts.Where(a => a.AppointmentDate.Date == today && a.Status != "Cancelled")
                                   .OrderBy(a => a.StartTime).ToList();

        var recentPatients = _weekAppts
            .Where(a => a.PatientName != null)
            .GroupBy(a => a.PatientName)
            .Select(g => g.First())
            .Take(5)
            .ToList();

        var fullName = Preferences.Get("FullName", "Doctor");
        var firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;

        var outerStack = new VerticalStackLayout { Spacing = 0 };

        // ── Top header bar ────────────────────────────────────────
        var headerGrid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 48, 16, 14),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var menuBtn = new Label { Text = "☰", FontSize = 24, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center, Padding = new Thickness(0, 0, 10, 0) };
        menuBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await OpenDrawerAsync()) });
        headerGrid.Children.Add(menuBtn);

        var greetingStack = new VerticalStackLayout
        {
            Spacing = 1, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    FormattedText = new FormattedString
                    {
                        Spans =
                        {
                            new Span { Text = "Bonjour ", FontSize = 15, TextColor = Color.FromArgb("#374151") },
                            new Span { Text = firstName + " 👋", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827") }
                        }
                    }
                },
                new Label { Text = "Voici ce qui est prévu pour aujourd'hui", FontSize = 12, TextColor = Color.FromArgb("#6B7280") }
            }
        };
        Grid.SetColumn(greetingStack, 1); headerGrid.Children.Add(greetingStack);

        var workflowBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#F0FDF4"), CornerRadius = 8,
            Padding = new Thickness(10, 6), HasShadow = false,
            BorderColor = Color.FromArgb("#6EE7B7"), VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "⚡ Workflow", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488") }
        };
        workflowBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new DoctorWorkflowPage(_api))) });
        Grid.SetColumn(workflowBtn, 2); headerGrid.Children.Add(workflowBtn);
        outerStack.Children.Add(headerGrid);

        // thin separator
        outerStack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") });

        // ── Three-panel horizontal scroll ─────────────────────────
        var panelScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var panelRow = new HorizontalStackLayout { Spacing = 0, Padding = new Thickness(16, 0, 16, 0) };

        // Panel 1 — Rendez-vous
        panelRow.Children.Add(BuildAppointmentsPanel(todayAppts, today));

        // spacer
        panelRow.Children.Add(new BoxView { WidthRequest = 14, Color = Colors.Transparent });

        // Panel 2 — Derniers Patients
        panelRow.Children.Add(BuildPatientsPanel(recentPatients));

        // spacer
        panelRow.Children.Add(new BoxView { WidthRequest = 14, Color = Colors.Transparent });

        // Panel 3 — Agenda
        panelRow.Children.Add(BuildAgendaPanel());

        panelScroll.Content = panelRow;
        outerStack.Children.Add(panelScroll);

        // ── Footer ───────────────────────────────────────────────
        outerStack.Children.Add(new Label
        {
            Text = "© Copyrights MediCare+ 2026 - Tous droits réservés.",
            FontSize = 11, TextColor = Color.FromArgb("#9CA3AF"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 32, 0, 24)
        });

        _mainScroll.Content = outerStack;

        // Render agenda after layout
        MainThread.BeginInvokeOnMainThread(() => RefreshAgendaGrid(today));
    }

    // ── Panel builders ────────────────────────────────────────

    private View BuildAppointmentsPanel(List<AppointmentDto> todayAppts, DateTime today)
    {
        var panel = DashPanel("Rendez-vous", "📋");

        if (!todayAppts.Any())
        {
            panel.Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    BuildAppointmentsHeader(),
                    new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 12,
                        Padding = new Thickness(24, 40),
                        Children =
                        {
                            new Label { Text = "🗂️", FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                            new Label { Text = "Aucune donnée disponible", FontSize = 13, TextColor = Color.FromArgb("#9CA3AF"), HorizontalOptions = LayoutOptions.Center }
                        }
                    }
                }
            };
            return panel;
        }

        var innerScroll = new ScrollView { HeightRequest = 380 };
        var apptList = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16) };

        foreach (var appt in todayAppts)
        {
            var capturedAppt = appt;
            var statusColor = appt.Status switch
            {
                "Confirmed" => "#10B981", "Completed" => "#8B5CF6",
                "Cancelled" => "#EF4444", _ => "#F59E0B"
            };

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = 54 },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 10, RowSpacing = 0
            };

            // Time column
            var timeCol = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = appt.StartTime.ToString(@"hh\:mm"), FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = appt.EndTime.ToString(@"hh\:mm"), FontSize = 10, TextColor = Color.FromArgb("#9CA3AF"), HorizontalOptions = LayoutOptions.Center }
                }
            };

            // Info column
            var infoCol = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center, Spacing = 2,
                Children =
                {
                    new Label { Text = appt.PatientName ?? "Patient", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), LineBreakMode = LineBreakMode.TailTruncation },
                    new Label { Text = appt.Reason, FontSize = 11, TextColor = Color.FromArgb("#6B7280"), LineBreakMode = LineBreakMode.TailTruncation },
                    new Label { Text = appt.ConsultationType == "Video" ? "📹 Vidéo" : "🏥 Présentiel", FontSize = 10, TextColor = Color.FromArgb("#0D9488") }
                }
            };

            // Status badge
            var statusBadge = new Frame
            {
                BackgroundColor = appt.StatusBg, CornerRadius = 6,
                Padding = new Thickness(6, 2), HasShadow = false, BorderColor = Colors.Transparent,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label { Text = appt.Status, FontSize = 9, TextColor = Color.FromArgb(statusColor), FontAttributes = FontAttributes.Bold }
            };

            row.Children.Add(timeCol);
            Grid.SetColumn(infoCol, 1); row.Children.Add(infoCol);
            Grid.SetColumn(statusBadge, 2); row.Children.Add(statusBadge);

            // Action buttons under
            var actions = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(0, 6, 0, 0) };
            if (appt.Status == "Scheduled")
            {
                var btn = SmallActionBtn("Confirmer", "#D1FAE5", "#065F46");
                btn.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => { await _api.UpdateAppointmentStatusAsync(capturedAppt.Id, "Confirmed"); await LoadDashboard(); })
                });
                actions.Children.Add(btn);
            }
            if (appt.Status is "Scheduled" or "Confirmed")
            {
                var btn = SmallActionBtn("Terminer", "#EDE9FE", "#5B21B6");
                btn.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => { await _api.UpdateAppointmentStatusAsync(capturedAppt.Id, "Completed"); await LoadDashboard(); })
                });
                actions.Children.Add(btn);
            }

            var card = new Frame
            {
                BackgroundColor = Color.FromArgb("#F9FAFB"),
                CornerRadius = 10, Padding = new Thickness(12, 10),
                HasShadow = false, BorderColor = Color.FromArgb("#E5E7EB"),
                Content = new VerticalStackLayout
                {
                    Spacing = 0,
                    Children = { row }
                }
            };
            if (actions.Children.Count > 0)
                ((VerticalStackLayout)card.Content).Children.Add(actions);

            apptList.Children.Add(card);
        }

        innerScroll.Content = apptList;
        panel.Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                BuildAppointmentsHeader(),
                innerScroll
            }
        };
        return panel;
    }

    private View BuildPatientsHeader()
    {
        var grid = new Grid
        {
            Padding = new Thickness(16, 14, 16, 10),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        grid.Children.Add(new Label { Text = "👥", FontSize = 18, VerticalOptions = LayoutOptions.Center });
        var titleLabel = new Label
        {
            FontSize = 15, VerticalOptions = LayoutOptions.Center,
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = "Derniers", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), FontSize = 15 },
                    new Span { Text = " Patients", TextColor = Color.FromArgb("#6B7280"), FontSize = 15 }
                }
            }
        };
        Grid.SetColumn(titleLabel, 1); grid.Children.Add(titleLabel);
        var viewAllLbl = new Label { Text = "Voir tout →", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center };
        viewAllLbl.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorPatientsPage(_api)))
        });
        Grid.SetColumn(viewAllLbl, 2); grid.Children.Add(viewAllLbl);
        var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6") };
        return new VerticalStackLayout { Spacing = 0, Children = { grid, sep } };
    }

    private View BuildPatientsPanel(List<AppointmentDto> recentPatients)
    {
        if (!recentPatients.Any())
        {
            var f = DashPanel("Derniers Patients", "👥");
            f.Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    BuildPatientsHeader(),
                    new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center,
                        Spacing = 12, Padding = new Thickness(24, 40),
                        Children =
                        {
                            new Label { Text = "🗂️", FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                            new Label { Text = "Aucune donnée disponible", FontSize = 13, TextColor = Color.FromArgb("#9CA3AF"), HorizontalOptions = LayoutOptions.Center }
                        }
                    }
                }
            };
            return f;
        }

        var innerScroll = new ScrollView { HeightRequest = 380 };
        var list = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16) };

        foreach (var appt in recentPatients)
        {
            var initials = string.Join("", (appt.PatientName ?? "?").Split(' ')
                .Take(2).Select(w => char.ToUpper(w[0]).ToString()));
            var card = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12
            };
            var avatar = new Frame
            {
                WidthRequest = 40, HeightRequest = 40, CornerRadius = 20,
                BackgroundColor = Color.FromArgb("#CCFBF1"), BorderColor = Colors.Transparent,
                HasShadow = false, Padding = 0,
                Content = new Label { Text = initials, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            var info = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center, Spacing = 2,
                Children =
                {
                    new Label { Text = appt.PatientName ?? "", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827") },
                    new Label { Text = appt.AppointmentDate.ToString("dd MMM yyyy"), FontSize = 11, TextColor = Color.FromArgb("#6B7280") }
                }
            };
            card.Children.Add(avatar);
            Grid.SetColumn(info, 1); card.Children.Add(info);

            list.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#F9FAFB"), CornerRadius = 10,
                Padding = new Thickness(12, 10), HasShadow = false,
                BorderColor = Color.FromArgb("#E5E7EB"), Content = card
            });
        }

        innerScroll.Content = list;
        var panel = DashPanel("Derniers Patients", "👥");
        panel.Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { BuildPatientsHeader(), innerScroll }
        };
        return panel;
    }

    private Frame BuildAgendaPanel()
    {
        var year = DateTime.Today.Year;
        _monthLabel = new Label
        {
            Text = DateTime.Today.ToString("MMMM"), FontSize = 13, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center
        };

        var prevBtn = new Label { Text = "◀", FontSize = 16, TextColor = Color.FromArgb("#6B7280"), VerticalOptions = LayoutOptions.Center, Padding = new Thickness(4, 0) };
        prevBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                _agendaWeekStart = _agendaWeekStart.AddDays(-7);
                _monthLabel.Text = _agendaWeekStart.ToString("MMMM");
                try { _weekAppts = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, _agendaWeekStart); } catch { }
                RefreshAgendaGrid(DateTime.Today);
            })
        });
        var nextBtn = new Label { Text = "▶", FontSize = 16, TextColor = Color.FromArgb("#6B7280"), VerticalOptions = LayoutOptions.Center, Padding = new Thickness(4, 0) };
        nextBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                _agendaWeekStart = _agendaWeekStart.AddDays(7);
                _monthLabel.Text = _agendaWeekStart.ToString("MMMM");
                try { _weekAppts = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, _agendaWeekStart); } catch { }
                RefreshAgendaGrid(DateTime.Today);
            })
        });

        var navRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Padding = new Thickness(16, 12, 16, 8)
        };
        var monthRow = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center, Children = { _monthLabel } };
        navRow.Children.Add(monthRow);
        Grid.SetColumn(prevBtn, 1); navRow.Children.Add(prevBtn);
        Grid.SetColumn(nextBtn, 2); navRow.Children.Add(nextBtn);

        _agendaGrid = new Grid { Margin = new Thickness(16, 0, 16, 16) };

        var agendaScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            HeightRequest = 360,
            Content = _agendaGrid
        };

        var titleRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Padding = new Thickness(16, 16, 16, 0)
        };
        titleRow.Children.Add(new Label
        {
            Text = $"Agenda {year}", FontSize = 16, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827")
        });
        var calBtn = new Label { Text = "≡", FontSize = 20, TextColor = Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center };
        calBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorCalendarPage(_api)))
        });
        Grid.SetColumn(calBtn, 1); titleRow.Children.Add(calBtn);

        var panel = new Frame
        {
            WidthRequest = 320, CornerRadius = 12,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            HasShadow = false, Padding = 0,
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children = { titleRow, navRow, agendaScroll }
            }
        };
        return panel;
    }

    private void RefreshAgendaGrid(DateTime today)
    {
        _agendaGrid.Children.Clear();
        _agendaGrid.ColumnDefinitions.Clear();
        _agendaGrid.RowDefinitions.Clear();

        // 3 day columns (Mon, Tue, Wed of current week)
        var days = new[] { _agendaWeekStart, _agendaWeekStart.AddDays(1), _agendaWeekStart.AddDays(2) };
        var colW = 88.0;

        _agendaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colW });
        _agendaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colW });
        _agendaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colW });

        // Time slots: 09:00–16:45 in 15-min steps
        var slots = Enumerable.Range(0, 32).Select(i => TimeSpan.FromMinutes(9 * 60 + i * 15)).ToArray();
        // header row + slot rows
        _agendaGrid.RowDefinitions.Add(new RowDefinition { Height = 42 }); // header
        foreach (var _ in slots)
            _agendaGrid.RowDefinitions.Add(new RowDefinition { Height = 30 });

        // Day headers
        for (int c = 0; c < days.Length; c++)
        {
            var d = days[c];
            bool isToday = d.Date == today;
            var dayStr = d.ToString("ddd").ToLower();
            var dateStr = d.ToString("dd/MM");
            var bg = isToday ? Color.FromArgb("#0D9488") : Color.FromArgb("#F3F4F6");
            var fg = isToday ? Colors.White : Color.FromArgb("#374151");

            var headerCell = new Frame
            {
                BackgroundColor = bg, CornerRadius = 6,
                Padding = new Thickness(4, 6), HasShadow = false,
                BorderColor = Colors.Transparent, Margin = new Thickness(1),
                Content = new VerticalStackLayout
                {
                    Spacing = 0, HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = $"{dayStr}. {dateStr}", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = fg, HorizontalOptions = LayoutOptions.Center }
                    }
                }
            };
            Grid.SetRow(headerCell, 0);
            Grid.SetColumn(headerCell, c);
            _agendaGrid.Children.Add(headerCell);
        }

        // Time slot rows
        for (int r = 0; r < slots.Length; r++)
        {
            var slot = slots[r];
            bool altRow = r % 2 == 0;
            var rowBg = altRow ? Color.FromArgb("#F9FAFB") : Colors.White;

            for (int c = 0; c < days.Length; c++)
            {
                var day = days[c];
                var apptInSlot = _weekAppts.FirstOrDefault(a =>
                    a.AppointmentDate.Date == day.Date &&
                    a.StartTime <= slot && a.EndTime > slot &&
                    a.Status != "Cancelled");

                View cellContent;
                if (apptInSlot != null)
                {
                    cellContent = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#CCFBF1"),
                        CornerRadius = 4, Padding = new Thickness(3, 1),
                        HasShadow = false, BorderColor = Color.FromArgb("#5EEAD4"),
                        Margin = new Thickness(1),
                        Content = new Label
                        {
                            Text = apptInSlot.PatientName?.Split(' ').FirstOrDefault() ?? "RDV",
                            FontSize = 8, TextColor = Color.FromArgb("#0D9488"),
                            LineBreakMode = LineBreakMode.TailTruncation
                        }
                    };
                }
                else
                {
                    cellContent = new Frame
                    {
                        BackgroundColor = rowBg, CornerRadius = 0,
                        Padding = new Thickness(0), HasShadow = false,
                        BorderColor = Color.FromArgb("#F0F0F0"), Margin = new Thickness(0),
                        Content = new Label
                        {
                            Text = slot.ToString(@"hh\:mm"),
                            FontSize = 10, TextColor = Color.FromArgb("#9CA3AF"),
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                }

                Grid.SetRow(cellContent, r + 1);
                Grid.SetColumn(cellContent, c);
                _agendaGrid.Children.Add(cellContent);
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    private static Frame DashPanel(string title, string icon)
    {
        return new Frame
        {
            WidthRequest = 290, CornerRadius = 12,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            HasShadow = false, Padding = 0,
            Content = new VerticalStackLayout { Spacing = 0 }
        };
    }

    private static Frame DashPanelWithEmpty(string title, string icon, string emptyMsg)
    {
        var f = DashPanel(title, icon);
        f.Content = PanelWithEmpty(title, icon, emptyMsg);
        return f;
    }

    private static View PanelWithEmpty(string title, string icon, string emptyMsg)
    {
        return new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                PanelHeader(title, icon),
                new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 12,
                    Padding = new Thickness(24, 40),
                    Children =
                    {
                        new Label { Text = "🗂️", FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                        new Label
                        {
                            Text = emptyMsg, FontSize = 13, TextColor = Color.FromArgb("#9CA3AF"),
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                }
            }
        };
    }

    private static View PanelHeader(string title, string icon)
    {
        var headerGrid = new Grid
        {
            Padding = new Thickness(16, 14, 16, 10),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        headerGrid.Children.Add(new Label { Text = icon, FontSize = 18, VerticalOptions = LayoutOptions.Center });
        var titleLabel = new Label
        {
            FontSize = 15, VerticalOptions = LayoutOptions.Center,
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = title.Split(' ')[0], FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), FontSize = 15 },
                    new Span { Text = title.Contains(' ') ? " " + string.Join(" ", title.Split(' ').Skip(1)) : "", TextColor = Color.FromArgb("#6B7280"), FontSize = 15 }
                }
            }
        };
        Grid.SetColumn(titleLabel, 1); headerGrid.Children.Add(titleLabel);
        var menuIcon = new Label { Text = "≡", FontSize = 18, TextColor = Color.FromArgb("#D1D5DB"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(menuIcon, 2); headerGrid.Children.Add(menuIcon);

        var separator = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6") };
        return new VerticalStackLayout { Spacing = 0, Children = { headerGrid, separator } };
    }

    private static Frame SmallActionBtn(string text, string bg, string fg)
    {
        return new Frame
        {
            BackgroundColor = Color.FromArgb(bg), CornerRadius = 6,
            Padding = new Thickness(10, 4), HasShadow = false,
            BorderColor = Colors.Transparent,
            Content = new Label { Text = text, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(fg) }
        };
    }

    private View BuildAppointmentsHeader()
    {
        var grid = new Grid
        {
            Padding = new Thickness(16, 14, 16, 10),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        grid.Children.Add(new Label { Text = "📋", FontSize = 18, VerticalOptions = LayoutOptions.Center });

        var titleLabel = new Label
        {
            FontSize = 15, VerticalOptions = LayoutOptions.Center,
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = "Rendez", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), FontSize = 15 },
                    new Span { Text = "-vous", TextColor = Color.FromArgb("#6B7280"), FontSize = 15 }
                }
            }
        };
        Grid.SetColumn(titleLabel, 1); grid.Children.Add(titleLabel);

        var viewAllLbl = new Label
        {
            Text = "Voir tout →", FontSize = 12, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center
        };
        viewAllLbl.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorAppointmentsListPage(_api)))
        });
        Grid.SetColumn(viewAllLbl, 2); grid.Children.Add(viewAllLbl);

        var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6") };
        return new VerticalStackLayout { Spacing = 0, Children = { grid, sep } };
    }
}

public class DoctorCalendarPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _doctorId;
    private DateTime _displayMonth;
    private List<AppointmentDto> _monthAppts = new();
    private List<DoctorScheduleItemDto> _schedule = new();

    private Label _monthYearLabel = new();
    private VerticalStackLayout _calendarBody = new();
    private ActivityIndicator _spinner = new();
    private VerticalStackLayout _filtersPanel = new();
    private bool _filtersExpanded = false;

    private string _statusFilter = "Tous";
    private string _typeFilter = "Tous";

    private const double ColW = 82.0;
    private static readonly string[] DayHdrs = { "DIM.", "LUN.", "MAR.", "MER.", "JEU.", "VEN.", "SAM." };
    private static readonly TimeSpan[] PreviewSlots =
    {
        new TimeSpan(9, 0, 0), new TimeSpan(9, 15, 0),
        new TimeSpan(9, 30, 0), new TimeSpan(9, 45, 0)
    };

    public DoctorCalendarPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildLayout();
    }

    private void BuildLayout()
    {
        // ── Header bar ──────────────────────────────────────────
        var headerBar = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 48, 16, 12),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };
        var backLbl = new Label { Text = "←", FontSize = 22, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center };
        backLbl.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        headerBar.Children.Add(backLbl);

        var titleRow = new HorizontalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, Spacing = 8,
            Children =
            {
                new Label { Text = "📅", FontSize = 20, VerticalOptions = LayoutOptions.Center },
                new Label { Text = "Agenda", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), VerticalOptions = LayoutOptions.Center }
            }
        };
        Grid.SetColumn(titleRow, 1); headerBar.Children.Add(titleRow);

        var workBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#F0FDF4"), CornerRadius = 8,
            Padding = new Thickness(10, 6), HasShadow = false,
            BorderColor = Color.FromArgb("#6EE7B7"), VerticalOptions = LayoutOptions.Center,
            Content = new HorizontalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Heures De Travail", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center },
                    new Label { Text = "⏰", FontSize = 12 }
                }
            }
        };
        workBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorScheduleSetupPage(ServiceHelper.GetService<ApiService>())))
        });
        Grid.SetColumn(workBtn, 2); headerBar.Children.Add(workBtn);

        // ── Navigation bar ──────────────────────────────────────
        var navBar = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 8),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        var prevBtn = CalNavBtn("Précédent");
        prevBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => { _displayMonth = _displayMonth.AddMonths(-1); await LoadMonthAsync(); })
        });
        var nextBtn = CalNavBtn("Suivant");
        nextBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => { _displayMonth = _displayMonth.AddMonths(1); await LoadMonthAsync(); })
        });
        var todayBtn = CalNavBtn("Aujourd'hui");
        todayBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => { _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); await LoadMonthAsync(); })
        });
        navBar.Children.Add(new HorizontalStackLayout { Spacing = 6, Children = { prevBtn, nextBtn, todayBtn } });
        _monthYearLabel = new Label
        {
            FontSize = 14, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827"),
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(_monthYearLabel, 1); navBar.Children.Add(_monthYearLabel);

        // ── Filter toggle ────────────────────────────────────────
        var filterToggleRow = new Grid
        {
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            Padding = new Thickness(16, 10),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        filterToggleRow.Children.Add(new Label
        {
            Text = "Statut de consultation", FontSize = 12, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center
        });
        var chevron = new Label { Text = "▼", FontSize = 10, TextColor = Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(chevron, 1); filterToggleRow.Children.Add(chevron);
        filterToggleRow.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                _filtersExpanded = !_filtersExpanded;
                _filtersPanel.IsVisible = _filtersExpanded;
                chevron.Text = _filtersExpanded ? "▲" : "▼";
            })
        });

        _filtersPanel = BuildFiltersPanel();
        _filtersPanel.IsVisible = false;

        // ── Day column headers (inside scroll area) ───────────────
        var dayHdrGrid = new Grid { BackgroundColor = Colors.White, Padding = new Thickness(0, 8) };
        for (int i = 0; i < 7; i++)
            dayHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = ColW });
        for (int i = 0; i < 7; i++)
        {
            var lbl = new Label
            {
                Text = DayHdrs[i], FontSize = 10, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7280"), HorizontalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(lbl, i); dayHdrGrid.Children.Add(lbl);
        }

        _calendarBody = new VerticalStackLayout { Spacing = 0 };
        _spinner = new ActivityIndicator { IsRunning = false, Color = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 30) };

        var calScrollContent = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { dayHdrGrid, _calendarBody, _spinner }
        };
        var calScroll = new ScrollView { Orientation = ScrollOrientation.Both, Content = calScrollContent };

        // ── Main page layout ─────────────────────────────────────
        var pageGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto }, // 0: header
                new RowDefinition { Height = GridLength.Auto }, // 1: nav
                new RowDefinition { Height = GridLength.Auto }, // 2: sep
                new RowDefinition { Height = GridLength.Auto }, // 3: filter toggle
                new RowDefinition { Height = GridLength.Auto }, // 4: filter panel
                new RowDefinition { Height = GridLength.Star }  // 5: calendar scroll
            }
        };
        pageGrid.Children.Add(headerBar);
        Grid.SetRow(navBar, 1); pageGrid.Children.Add(navBar);
        var sepLine = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") };
        Grid.SetRow(sepLine, 2); pageGrid.Children.Add(sepLine);
        Grid.SetRow(filterToggleRow, 3); pageGrid.Children.Add(filterToggleRow);
        Grid.SetRow(_filtersPanel, 4); pageGrid.Children.Add(_filtersPanel);
        Grid.SetRow(calScroll, 5); pageGrid.Children.Add(calScroll);

        Content = pageGrid;
    }

    private VerticalStackLayout BuildFiltersPanel()
    {
        var panel = new VerticalStackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 12),
            Spacing = 0
        };

        // Status section
        panel.Children.Add(new Label { Text = "Statut de consultation", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151"), Margin = new Thickness(0, 0, 0, 8) });
        var statusItems = new (string, Color?)[]
        {
            ("Tous",                    null),
            ("Libre",                   Color.FromArgb("#E5E7EB")),
            ("Déjà pris confirmé",      Color.FromArgb("#10B981")),
            ("Déjà pris non confirmé",  Color.FromArgb("#3B82F6")),
            ("Contrôle",               Color.FromArgb("#F59E0B"))
        };
        foreach (var (lbl, dot) in statusItems)
        {
            var capturedStatus = lbl;
            var row = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 0, 0, 6) };
            var cb = new CheckBox { IsChecked = _statusFilter == lbl, Color = Color.FromArgb("#0D9488") };
            cb.CheckedChanged += (s, e) => { if (e.Value) { _statusFilter = capturedStatus; BuildCalendarContent(); } };
            row.Children.Add(cb);
            if (dot != null)
                row.Children.Add(new BoxView { WidthRequest = 14, HeightRequest = 14, Color = dot, CornerRadius = 3, VerticalOptions = LayoutOptions.Center });
            row.Children.Add(new Label { Text = lbl, FontSize = 12, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center });
            panel.Children.Add(row);
        }

        panel.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6"), Margin = new Thickness(0, 8) });

        // Type section
        panel.Children.Add(new Label { Text = "Type de consultation", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151"), Margin = new Thickness(0, 0, 0, 8) });
        foreach (var typeOpt in new[] { "Tous", "Présentiel", "Téléconsultation" })
        {
            var capturedType = typeOpt;
            var row = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 0, 0, 6) };
            var cb = new CheckBox { IsChecked = _typeFilter == typeOpt, Color = Color.FromArgb("#0D9488") };
            cb.CheckedChanged += (s, e) => { if (e.Value) { _typeFilter = capturedType; BuildCalendarContent(); } };
            row.Children.Add(cb);
            row.Children.Add(new Label { Text = typeOpt, FontSize = 12, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center });
            panel.Children.Add(row);
        }

        panel.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6"), Margin = new Thickness(0, 8) });

        // Motif section
        panel.Children.Add(new Label { Text = "Motif de la consultation", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151"), Margin = new Thickness(0, 0, 0, 8) });
        var motifRow = new HorizontalStackLayout { Spacing = 10 };
        motifRow.Children.Add(new CheckBox { IsChecked = true, Color = Color.FromArgb("#0D9488") });
        motifRow.Children.Add(new Label { Text = "Tous", FontSize = 12, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center });
        panel.Children.Add(motifRow);

        return panel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMonthAsync();
    }

    private async Task LoadMonthAsync()
    {
        _spinner.IsRunning = true;
        _calendarBody.Children.Clear();
        _monthYearLabel.Text = _displayMonth.ToString("MMMM yyyy");

        try
        {
            _schedule = await _api.GetDoctorScheduleAsync(_doctorId);
            _monthAppts = new List<AppointmentDto>();

            // Load all weeks that overlap with the displayed month
            var firstDay = _displayMonth;
            var sunday = firstDay.AddDays(-(int)firstDay.DayOfWeek);
            for (int w = 0; w < 6; w++)
            {
                var ws = sunday.AddDays(w * 7);
                if (ws >= firstDay.AddMonths(1).AddDays(7)) break;
                var data = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, ws);
                _monthAppts.AddRange(data.Where(a => !_monthAppts.Any(x => x.Id == a.Id)));
            }
        }
        catch { }

        _spinner.IsRunning = false;
        BuildCalendarContent();
    }

    private void BuildCalendarContent()
    {
        _calendarBody.Children.Clear();
        var firstDay = _displayMonth;
        var sunday = firstDay.AddDays(-(int)firstDay.DayOfWeek);

        for (int w = 0; w < 6; w++)
        {
            var ws = sunday.AddDays(w * 7);
            if (ws >= firstDay.AddMonths(1).AddDays(7)) break;
            if (ws.AddDays(6) < firstDay) continue;
            _calendarBody.Children.Add(BuildWeekBlock(ws));
        }
    }

    private Grid BuildWeekBlock(DateTime weekSunday)
    {
        var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
        for (int i = 0; i < 7; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ColW });

        // Row 0: top border line
        // Row 1: date numbers
        // Rows 2-5: 4 preview slots
        // Row 6: "+N RDV" footer
        grid.RowDefinitions.Add(new RowDefinition { Height = 1 });  // separator
        grid.RowDefinitions.Add(new RowDefinition { Height = 34 }); // date row
        for (int i = 0; i < 4; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = 26 }); // slot rows
        grid.RowDefinitions.Add(new RowDefinition { Height = 34 }); // footer

        // Top separator line spanning all columns
        var sepBar = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E2E8F0"), BackgroundColor = Color.FromArgb("#E2E8F0") };
        Grid.SetRow(sepBar, 0); Grid.SetColumnSpan(sepBar, 7);
        grid.Children.Add(sepBar);

        var filtered = GetFilteredAppts();

        for (int d = 0; d < 7; d++)
        {
            var date = weekSunday.AddDays(d);
            bool inMonth = date.Month == _displayMonth.Month;
            bool isToday = date.Date == DateTime.Today;
            bool isPast = date.Date < DateTime.Today;

            // ── Date number cell ──────────────────────────────────
            var dateBg = isToday ? Color.FromArgb("#0D9488") : Colors.White;
            var dateFg = isToday ? Colors.White : (inMonth ? Color.FromArgb("#374151") : Color.FromArgb("#CBD5E1"));

            var dateCell = new Frame
            {
                BackgroundColor = dateBg, HasShadow = false,
                BorderColor = Colors.Transparent, Padding = new Thickness(6, 6, 6, 2),
                Content = new Label
                {
                    Text = date.Day.ToString(),
                    FontSize = isToday ? 13 : 13,
                    FontAttributes = isToday ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = dateFg,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start
                }
            };
            Grid.SetRow(dateCell, 1); Grid.SetColumn(dateCell, d);
            grid.Children.Add(dateCell);

            // ── Preview slot rows ─────────────────────────────────
            for (int s = 0; s < 4; s++)
            {
                var slotTime = PreviewSlots[s];
                bool doctorWorks = inMonth && DoctorWorksAt(date, slotTime);

                var appt = filtered.FirstOrDefault(a =>
                    a.AppointmentDate.Date == date.Date &&
                    a.StartTime <= slotTime && a.EndTime > slotTime);

                View cell;
                if (!inMonth || !doctorWorks)
                {
                    cell = new BoxView { Color = Color.FromArgb("#F8FAFC") };
                }
                else if (appt != null)
                {
                    var (bg, fg) = StatusColors(appt.Status);
                    cell = new Frame
                    {
                        BackgroundColor = bg, HasShadow = false,
                        BorderColor = Colors.Transparent, Padding = new Thickness(4, 1),
                        Margin = new Thickness(1, 0),
                        Content = new Label
                        {
                            Text = appt.PatientName?.Split(' ').FirstOrDefault() ?? "RDV",
                            FontSize = 8, TextColor = fg,
                            LineBreakMode = LineBreakMode.TailTruncation,
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                }
                else
                {
                    cell = new Frame
                    {
                        BackgroundColor = Colors.White, HasShadow = false,
                        BorderColor = Color.FromArgb("#F3F4F6"), Padding = new Thickness(4, 1),
                        Content = new Label
                        {
                            Text = $"{(int)slotTime.TotalHours}h{(slotTime.Minutes > 0 ? slotTime.Minutes.ToString() : "")} Libre",
                            FontSize = 9, TextColor = Color.FromArgb("#9CA3AF"),
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                }
                Grid.SetRow(cell, s + 2); Grid.SetColumn(cell, d);
                grid.Children.Add(cell);
            }

            // ── Footer: "+N RDV" button ───────────────────────────
            int totalSlots = inMonth ? GetTotalSlots(date) : 0;
            View footer;
            if (totalSlots > 0)
            {
                int booked = _monthAppts.Count(a => a.AppointmentDate.Date == date.Date && a.Status != "Cancelled");
                footer = new Frame
                {
                    BackgroundColor = Color.FromArgb("#1E3A5F"),
                    CornerRadius = 4, HasShadow = false,
                    BorderColor = Colors.Transparent, Padding = new Thickness(2, 6),
                    Margin = new Thickness(2, 3),
                    Content = new Label
                    {
                        Text = $"+{totalSlots} RDV",
                        FontSize = 10, FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
                footer.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        var dateStr = date.ToString("dddd dd MMMM yyyy");
                        var dayAppts = _monthAppts.Where(a => a.AppointmentDate.Date == date.Date && a.Status != "Cancelled").OrderBy(a => a.StartTime).ToList();
                        var msg = dayAppts.Any()
                            ? string.Join("\n", dayAppts.Select(a => $"• {a.StartTime:hh\\:mm} — {a.PatientName} ({a.Status})"))
                            : "Aucun rendez-vous pour cette journée.";
                        await DisplayAlert(dateStr, msg, "Fermer");
                    })
                });
            }
            else
            {
                footer = new BoxView { Color = Colors.White };
            }
            Grid.SetRow(footer, 6); Grid.SetColumn(footer, d);
            grid.Children.Add(footer);
        }

        return grid;
    }

    // ── Helpers ───────────────────────────────────────────────

    private List<AppointmentDto> GetFilteredAppts()
    {
        var q = _monthAppts.AsEnumerable();
        q = _statusFilter switch
        {
            "Déjà pris confirmé" => q.Where(a => a.Status == "Confirmed"),
            "Déjà pris non confirmé" => q.Where(a => a.Status == "Scheduled"),
            "Contrôle" => q.Where(a => a.Status == "Completed"),
            _ => q
        };
        if (_typeFilter == "Présentiel") q = q.Where(a => a.ConsultationType == "InPerson");
        else if (_typeFilter == "Téléconsultation") q = q.Where(a => a.ConsultationType == "Video");
        return q.ToList();
    }

    private bool DoctorWorksAt(DateTime date, TimeSpan time)
    {
        if (!_schedule.Any())
            return date.DayOfWeek != DayOfWeek.Sunday && date.DayOfWeek != DayOfWeek.Saturday
                   && time >= new TimeSpan(9, 0, 0) && time < new TimeSpan(17, 0, 0);
        return _schedule.Any(s => s.DayOfWeek == (int)date.DayOfWeek && s.IsAvailable
                                  && s.StartTime <= time && s.EndTime > time);
    }

    private int GetTotalSlots(DateTime date)
    {
        if (!_schedule.Any())
            return date.DayOfWeek is DayOfWeek.Sunday ? 0
                 : date.DayOfWeek is DayOfWeek.Saturday ? 16
                 : 32;
        var dayScheds = _schedule.Where(s => s.DayOfWeek == (int)date.DayOfWeek && s.IsAvailable);
        return dayScheds.Sum(s => (int)((s.EndTime - s.StartTime).TotalMinutes / 15));
    }

    private static (Color Bg, Color Fg) StatusColors(string status) => status switch
    {
        "Confirmed" => (Color.FromArgb("#D1FAE5"), Color.FromArgb("#065F46")),
        "Scheduled" => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#1E40AF")),
        "Completed" => (Color.FromArgb("#EDE9FE"), Color.FromArgb("#5B21B6")),
        "Cancelled" => (Color.FromArgb("#FEE2E2"), Color.FromArgb("#991B1B")),
        _ => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#6B7280"))
    };

    private static Frame CalNavBtn(string text)
    {
        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 6,
            Padding = new Thickness(14, 7), HasShadow = false,
            BorderColor = Color.FromArgb("#D1D5DB"),
            Content = new Label
            {
                Text = text, FontSize = 13, TextColor = Color.FromArgb("#374151"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            }
        };
    }
}

// ===== DOCTOR APPOINTMENTS LIST PAGE =====
public class DoctorAppointmentsListPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _doctorId;
    private DateTime _selectedDate = DateTime.Today;
    private string _statusFilter = "Tous";

    private Label _headerDateLabel = new();
    private Label _navDateLabel = new();
    private VerticalStackLayout _listStack = new();
    private ActivityIndicator _spinner = new();

    public DoctorAppointmentsListPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAppointmentsAsync();
    }

    private void BuildLayout()
    {
        // ── Teal header bar ────────────────────────────────────
        _headerDateLabel = new Label
        {
            Text = _selectedDate.ToString("dddd, dd MMMM yyyy"),
            FontSize = 11, TextColor = Colors.White.WithAlpha(0.85f)
        };

        var backBtn = new Label
        {
            Text = "←", FontSize = 22, TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center, Padding = new Thickness(0, 0, 6, 0)
        };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PopAsync())
        });

        var headerGrid = new Grid
        {
            BackgroundColor = Color.FromArgb("#0D9488"),
            Padding = new Thickness(16, 48, 16, 14),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };
        headerGrid.Children.Add(backBtn);

        var titleStack = new VerticalStackLayout
        {
            Spacing = 2, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "📅 Liste des rendez-vous", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                _headerDateLabel
            }
        };
        Grid.SetColumn(titleStack, 1); headerGrid.Children.Add(titleStack);

        var calIcon = new Label
        {
            Text = "📋", FontSize = 20, TextColor = Colors.White.WithAlpha(0.9f),
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(calIcon, 2); headerGrid.Children.Add(calIcon);

        // ── Day navigation bar ─────────────────────────────────
        _navDateLabel = new Label
        {
            Text = _selectedDate.ToString("dd MMMM yyyy"),
            FontSize = 13, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center
        };

        var prevBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#F3F4F6"), CornerRadius = 8,
            Padding = new Thickness(12, 7), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "◀", FontSize = 13, TextColor = Color.FromArgb("#374151"), HorizontalOptions = LayoutOptions.Center }
        };
        prevBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                _selectedDate = _selectedDate.AddDays(-1);
                UpdateDateLabels();
                await LoadAppointmentsAsync();
            })
        });

        var nextBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#F3F4F6"), CornerRadius = 8,
            Padding = new Thickness(12, 7), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "▶", FontSize = 13, TextColor = Color.FromArgb("#374151"), HorizontalOptions = LayoutOptions.Center }
        };
        nextBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                _selectedDate = _selectedDate.AddDays(1);
                UpdateDateLabels();
                await LoadAppointmentsAsync();
            })
        });

        var todayBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#CCFBF1"), CornerRadius = 8,
            Padding = new Thickness(10, 7), HasShadow = false, BorderColor = Color.FromArgb("#5EEAD4"),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "Auj.", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center }
        };
        todayBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                _selectedDate = DateTime.Today;
                UpdateDateLabels();
                await LoadAppointmentsAsync();
            })
        });

        var navBarGrid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 10, 16, 10),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        navBarGrid.Children.Add(prevBtn);
        Grid.SetColumn(_navDateLabel, 1); navBarGrid.Children.Add(_navDateLabel);
        Grid.SetColumn(todayBtn, 2); navBarGrid.Children.Add(todayBtn);
        Grid.SetColumn(nextBtn, 3); navBarGrid.Children.Add(nextBtn);

        // ── Status filter chips ─────────────────────────────────
        var filterScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 0, 16, 10)
        };
        var chipRow = new HorizontalStackLayout { Spacing = 8 };
        foreach (var filter in new[] { "Tous", "Scheduled", "Confirmed", "Completed", "Cancelled" })
        {
            var f = filter;
            var chip = BuildFilterChip(f, f == _statusFilter);
            chip.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => { _statusFilter = f; await LoadAppointmentsAsync(); })
            });
            chipRow.Children.Add(chip);
        }
        filterScroll.Content = chipRow;

        // ── List area ──────────────────────────────────────────
        _spinner = new ActivityIndicator
        {
            IsRunning = false, IsVisible = false,
            Color = Color.FromArgb("#0D9488"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 32)
        };

        _listStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16, 12, 16, 28) };

        var mainScroll = new ScrollView
        {
            Content = new VerticalStackLayout { Spacing = 0, Children = { _spinner, _listStack } }
        };

        // ── Page grid ─────────────────────────────────────────
        var page = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        page.Children.Add(headerGrid);

        Grid.SetRow(navBarGrid, 1); page.Children.Add(navBarGrid);

        var sep1 = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") };
        Grid.SetRow(sep1, 2); page.Children.Add(sep1);

        Grid.SetRow(filterScroll, 3); page.Children.Add(filterScroll);
        Grid.SetRow(mainScroll, 4); page.Children.Add(mainScroll);

        Content = page;
    }

    private void UpdateDateLabels()
    {
        _headerDateLabel.Text = _selectedDate.ToString("dddd, dd MMMM yyyy");
        _navDateLabel.Text = _selectedDate.ToString("dd MMMM yyyy");
    }

    private async Task LoadAppointmentsAsync()
    {
        _spinner.IsRunning = true;
        _spinner.IsVisible = true;
        _listStack.Children.Clear();

        List<AppointmentDto> allAppts = new();
        try
        {
            var weekStart = _selectedDate.AddDays(-(int)_selectedDate.DayOfWeek + 1);
            allAppts = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, weekStart);
        }
        catch { }

        var dayAppts = allAppts
            .Where(a => a.AppointmentDate.Date == _selectedDate.Date)
            .OrderBy(a => a.StartTime)
            .ToList();

        if (_statusFilter != "Tous")
            dayAppts = dayAppts.Where(a => a.Status == _statusFilter).ToList();

        _spinner.IsRunning = false;
        _spinner.IsVisible = false;

        if (!dayAppts.Any())
        {
            _listStack.Children.Add(new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 14, Padding = new Thickness(24, 60),
                Children =
                {
                    new Label { Text = "👩‍💻", FontSize = 72, HorizontalOptions = LayoutOptions.Center },
                    new Label
                    {
                        Text = "Aucune donnée disponible",
                        FontSize = 16, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#9CA3AF"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = _statusFilter == "Tous"
                            ? $"Aucun rendez-vous pour le {_selectedDate:dd MMM yyyy}."
                            : $"Aucun rendez-vous « {ToStatusLabel(_statusFilter)} » pour ce jour.",
                        FontSize = 13, TextColor = Color.FromArgb("#CBD5E1"),
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            });
            return;
        }

        _listStack.Children.Add(new Label
        {
            Text = $"{dayAppts.Count} rendez-vous — {_selectedDate:dddd dd MMMM yyyy}",
            FontSize = 12, TextColor = Color.FromArgb("#6B7280"), Margin = new Thickness(0, 0, 0, 4)
        });

        foreach (var appt in dayAppts)
            _listStack.Children.Add(BuildAppointmentCard(appt));
    }

    private View BuildAppointmentCard(AppointmentDto appt)
    {
        var captured = appt;
        var (statusBg, statusFg) = appt.Status switch
        {
            "Confirmed" => ("#D1FAE5", "#065F46"),
            "Scheduled" => ("#DBEAFE", "#1E40AF"),
            "Completed" => ("#EDE9FE", "#5B21B6"),
            "Cancelled" => ("#FEE2E2", "#991B1B"),
            _ => ("#F3F4F6", "#6B7280")
        };

        var initials = string.Join("", (appt.PatientName ?? "?")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => char.ToUpper(w[0]).ToString()));

        var avatar = new Frame
        {
            WidthRequest = 48, HeightRequest = 48, CornerRadius = 24,
            BackgroundColor = Color.FromArgb("#CCFBF1"), BorderColor = Color.FromArgb("#5EEAD4"),
            HasShadow = false, Padding = 0,
            Content = new Label
            {
                Text = initials, FontSize = 16, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D9488"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            }
        };

        var timeCol = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, Spacing = 2,
            Children =
            {
                new Label { Text = appt.StartTime.ToString(@"hh\:mm"), FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488") },
                new Label { Text = appt.EndTime.ToString(@"hh\:mm"), FontSize = 11, TextColor = Color.FromArgb("#9CA3AF") }
            }
        };

        var statusBadge = new Frame
        {
            BackgroundColor = Color.FromArgb(statusBg), CornerRadius = 6,
            Padding = new Thickness(8, 3), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Start,
            Content = new Label { Text = ToStatusLabel(appt.Status), FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(statusFg) }
        };

        var infoCol = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, Spacing = 3,
            Children =
            {
                new Label { Text = appt.PatientName ?? "Patient", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), LineBreakMode = LineBreakMode.TailTruncation },
                new Label { Text = string.IsNullOrEmpty(appt.Reason) ? "Consultation" : appt.Reason, FontSize = 12, TextColor = Color.FromArgb("#6B7280"), LineBreakMode = LineBreakMode.TailTruncation },
                new Label { Text = appt.ConsultationType == "Video" ? "📹 Téléconsultation" : "🏥 Présentiel", FontSize = 11, TextColor = Color.FromArgb("#0D9488") }
            }
        };

        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = 60 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };
        topRow.Children.Add(avatar);
        Grid.SetColumn(timeCol, 1); topRow.Children.Add(timeCol);
        Grid.SetColumn(infoCol, 2); topRow.Children.Add(infoCol);
        Grid.SetColumn(statusBadge, 3); topRow.Children.Add(statusBadge);

        var actions = new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };

        if (appt.Status == "Scheduled")
        {
            var btn = ActionChip("✓ Confirmer", "#D1FAE5", "#065F46");
            btn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => { await _api.UpdateAppointmentStatusAsync(captured.Id, "Confirmed"); await LoadAppointmentsAsync(); })
            });
            actions.Children.Add(btn);
        }

        if (appt.Status is "Scheduled" or "Confirmed")
        {
            var completeBtn = ActionChip("✓ Terminer", "#EDE9FE", "#5B21B6");
            completeBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => { await _api.UpdateAppointmentStatusAsync(captured.Id, "Completed"); await LoadAppointmentsAsync(); })
            });
            actions.Children.Add(completeBtn);

            var cancelBtn = ActionChip("✕ Annuler", "#FEE2E2", "#991B1B");
            cancelBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    bool ok = await DisplayAlert("Annuler", $"Annuler le RDV de {captured.PatientName} ?", "Oui", "Non");
                    if (ok) { await _api.UpdateAppointmentStatusAsync(captured.Id, "Cancelled"); await LoadAppointmentsAsync(); }
                })
            });
            actions.Children.Add(cancelBtn);

            if (captured.ConsultationType == "Video" && captured.PatientUserId.HasValue)
            {
                var callBtn = ActionChip("📹 Appel vidéo", "#EFF6FF", "#1D4ED8");
                callBtn.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        var sr = ServiceHelper.GetService<SignalRService>();
                        await Navigation.PushAsync(new VideoCallPage(sr, captured.PatientUserId!.Value, captured.PatientName ?? "Patient", true));
                    })
                });
                actions.Children.Add(callBtn);
            }
        }

        var cardContent = new VerticalStackLayout { Spacing = 0, Children = { topRow } };
        if (actions.Children.Count > 0) cardContent.Children.Add(actions);

        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 12,
            Padding = new Thickness(14, 12), HasShadow = true,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Content = cardContent
        };
    }

    private static Frame BuildFilterChip(string label, bool active)
    {
        var (bg, fg, border) = label switch
        {
            "Tous"      => active ? ("#0D9488", "white", "#0D9488") : ("#F3F4F6", "#374151", "#E5E7EB"),
            "Scheduled" => ("#DBEAFE", "#1E40AF", "#BFDBFE"),
            "Confirmed" => ("#D1FAE5", "#065F46", "#A7F3D0"),
            "Completed" => ("#EDE9FE", "#5B21B6", "#DDD6FE"),
            "Cancelled" => ("#FEE2E2", "#991B1B", "#FECACA"),
            _           => ("#F3F4F6", "#6B7280", "#E5E7EB")
        };
        return new Frame
        {
            BackgroundColor = Color.FromArgb(bg), CornerRadius = 20,
            Padding = new Thickness(14, 6), HasShadow = false,
            BorderColor = Color.FromArgb(border),
            Content = new Label { Text = ToStatusLabel(label), FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(fg), VerticalOptions = LayoutOptions.Center }
        };
    }

    private static Frame ActionChip(string text, string bg, string fg) => new Frame
    {
        BackgroundColor = Color.FromArgb(bg), CornerRadius = 8,
        Padding = new Thickness(12, 5), HasShadow = false, BorderColor = Colors.Transparent,
        Content = new Label { Text = text, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(fg) }
    };

    private static string ToStatusLabel(string status) => status switch
    {
        "Scheduled" => "En attente",
        "Confirmed" => "Confirmé",
        "Completed" => "Terminé",
        "Cancelled" => "Annulé",
        "NoShow"    => "Absent",
        "Tous"      => "Tous",
        _           => status
    };
}

// ===== DOCTOR PATIENTS PAGE =====
public class DoctorPatientsPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _doctorId;

    private List<PatientRow> _allPatients = new();
    private List<PatientRow> _filtered    = new();
    private bool   _isGridView  = true;
    private string _searchText  = "";
    private string _motifFilter = "Tous";

    private VerticalStackLayout _bodyStack = new();
    private Label               _countLabel = new();
    private ActivityIndicator   _spinner    = new();

    private record PatientRow(Guid? UserId, string Name, string LastReason, DateTime LastVisit, string ConsultationType, int VisitCount);

    public DoctorPatientsPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.White;
        BuildLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPatientsAsync();
    }

    private void BuildLayout()
    {
        // ── Header bar ─────────────────────────────────────────
        var backBtn = new Label { Text = "←", FontSize = 22, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        _countLabel = new Label { Text = "0", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        var badge = new Frame
        {
            BackgroundColor = Color.FromArgb("#0D9488"), CornerRadius = 12,
            Padding = new Thickness(8, 3), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center, Content = _countLabel
        };

        var titleRow = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        titleRow.Children.Add(new Label { Text = "👥", FontSize = 22, VerticalOptions = LayoutOptions.Center });
        titleRow.Children.Add(new Label { Text = "Patients", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E3A5F"), VerticalOptions = LayoutOptions.Center });
        titleRow.Children.Add(badge);

        var newBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#0D9488"), CornerRadius = 8,
            Padding = new Thickness(12, 7), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "+ Nouveau", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
        };
        newBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await DisplayAlert("Nouveau patient", "Fonctionnalité disponible prochainement.", "OK"))
        });

        var gridToggleLbl = new Label { Text = "⊞ Grid", FontSize = 12, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center };
        var gridToggle = new Frame
        {
            BackgroundColor = Color.FromArgb("#F3F4F6"), CornerRadius = 8,
            Padding = new Thickness(10, 7), HasShadow = false, BorderColor = Color.FromArgb("#E5E7EB"),
            VerticalOptions = LayoutOptions.Center, Content = gridToggleLbl
        };
        gridToggle.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { _isGridView = !_isGridView; gridToggleLbl.Text = _isGridView ? "⊞ Grid" : "≡ Liste"; ApplyFilter(); })
        });

        var headerGrid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 48, 16, 12),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        headerGrid.Children.Add(backBtn);
        Grid.SetColumn(titleRow, 1); headerGrid.Children.Add(titleRow);
        Grid.SetColumn(newBtn, 2);   headerGrid.Children.Add(newBtn);
        Grid.SetColumn(gridToggle, 3); headerGrid.Children.Add(gridToggle);

        // ── Search + filter ────────────────────────────────────
        var searchEntry = new Entry { Placeholder = "Recherche..", FontSize = 13, BackgroundColor = Colors.Transparent };
        searchEntry.TextChanged += (s, e) => { _searchText = e.NewTextValue ?? ""; ApplyFilter(); };

        var searchFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#F9FAFB"), CornerRadius = 10,
            Padding = new Thickness(12, 2), HasShadow = false,
            BorderColor = Color.FromArgb("#E5E7EB"), Content = searchEntry,
            Margin = new Thickness(16, 10, 16, 0)
        };

        var motifPicker = new Picker { Title = "Motif de la consultation", FontSize = 13, BackgroundColor = Colors.Transparent };
        foreach (var m in new[] { "Tous", "Consultation générale", "Suivi", "Urgence", "Contrôle", "Téléconsultation" })
            motifPicker.Items.Add(m);
        motifPicker.SelectedIndex = 0;
        motifPicker.SelectedIndexChanged += (s, e) =>
        {
            _motifFilter = motifPicker.SelectedIndex <= 0 ? "Tous" : motifPicker.Items[motifPicker.SelectedIndex];
            ApplyFilter();
        };

        var motifFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#F9FAFB"), CornerRadius = 10,
            Padding = new Thickness(12, 2), HasShadow = false,
            BorderColor = Color.FromArgb("#E5E7EB"), Content = motifPicker,
            Margin = new Thickness(16, 8, 16, 10)
        };

        // ── Body ───────────────────────────────────────────────
        _spinner   = new ActivityIndicator { IsRunning = false, IsVisible = false, Color = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 40) };
        _bodyStack = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16, 4, 16, 28) };

        var mainScroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    searchFrame,
                    motifFrame,
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") },
                    _spinner,
                    _bodyStack
                }
            }
        };

        var outer = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = 1 },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        outer.Children.Add(headerGrid);
        var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") };
        Grid.SetRow(sep, 1); outer.Children.Add(sep);
        Grid.SetRow(mainScroll, 2); outer.Children.Add(mainScroll);

        Content = outer;
    }

    private async Task LoadPatientsAsync()
    {
        _spinner.IsRunning = true; _spinner.IsVisible = true;
        _bodyStack.Children.Clear();

        var today = DateTime.Today;
        var tasks = Enumerable.Range(0, 8)
            .Select(w => _api.GetDoctorWeekAppointmentsAsync(_doctorId, today.AddDays(-(w * 7) - (int)today.DayOfWeek + 1)));

        List<AppointmentDto> all = new();
        try { var results = await Task.WhenAll(tasks); all = results.SelectMany(r => r).ToList(); }
        catch { }

        _allPatients = all
            .Where(a => a.PatientName != null)
            .GroupBy(a => a.PatientUserId?.ToString() ?? a.PatientName!)
            .Select(g =>
            {
                var last = g.OrderByDescending(x => x.AppointmentDate).First();
                return new PatientRow(last.PatientUserId, last.PatientName ?? "", last.Reason, last.AppointmentDate, last.ConsultationType, g.Count());
            })
            .OrderByDescending(p => p.LastVisit)
            .ToList();

        _spinner.IsRunning = false; _spinner.IsVisible = false;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filtered = _allPatients
            .Where(p => string.IsNullOrEmpty(_searchText) || p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .Where(p => _motifFilter == "Tous"
                        || p.LastReason.Contains(_motifFilter, StringComparison.OrdinalIgnoreCase)
                        || (_motifFilter == "Téléconsultation" && p.ConsultationType == "Video"))
            .ToList();

        _countLabel.Text = _filtered.Count.ToString();
        MainThread.BeginInvokeOnMainThread(RenderPatients);
    }

    private void RenderPatients()
    {
        _bodyStack.Children.Clear();

        if (!_filtered.Any())
        {
            _bodyStack.Children.Add(new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center, Spacing = 14, Padding = new Thickness(0, 60),
                Children =
                {
                    new Label { Text = "👩‍💻", FontSize = 72, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Aucune donnée disponible", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#9CA3AF"), HorizontalOptions = LayoutOptions.Center }
                }
            });
            return;
        }

        if (_isGridView)
        {
            for (int i = 0; i < _filtered.Count; i += 2)
            {
                var pairGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    ColumnSpacing = 10
                };
                pairGrid.Children.Add(BuildGridCard(_filtered[i]));
                if (i + 1 < _filtered.Count)
                {
                    var right = BuildGridCard(_filtered[i + 1]);
                    Grid.SetColumn(right, 1); pairGrid.Children.Add(right);
                }
                else
                {
                    var placeholder = new BoxView { Color = Colors.Transparent };
                    Grid.SetColumn(placeholder, 1); pairGrid.Children.Add(placeholder);
                }
                _bodyStack.Children.Add(pairGrid);
            }
        }
        else
        {
            foreach (var p in _filtered)
                _bodyStack.Children.Add(BuildListCard(p));
        }
    }

    private View BuildGridCard(PatientRow p)
    {
        var initials = GetInitials(p.Name);
        var card = new Frame
        {
            CornerRadius = 12, BackgroundColor = Colors.White,
            HasShadow = false, BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = new Thickness(12, 16)
        };
        var content = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center, Spacing = 8,
            Children =
            {
                new Frame
                {
                    WidthRequest = 54, HeightRequest = 54, CornerRadius = 27,
                    BackgroundColor = Color.FromArgb("#CCFBF1"), BorderColor = Color.FromArgb("#5EEAD4"),
                    HasShadow = false, Padding = 0, HorizontalOptions = LayoutOptions.Center,
                    Content = new Label { Text = initials, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                },
                new Label { Text = p.Name, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation },
                new Label { Text = p.LastVisit.ToString("dd MMM yyyy"), FontSize = 10, TextColor = Color.FromArgb("#9CA3AF"), HorizontalOptions = LayoutOptions.Center },
                new Frame
                {
                    BackgroundColor = Color.FromArgb("#F0FDF4"), CornerRadius = 10,
                    Padding = new Thickness(8, 3), HasShadow = false, BorderColor = Colors.Transparent,
                    HorizontalOptions = LayoutOptions.Center,
                    Content = new Label { Text = $"{p.VisitCount} visite{(p.VisitCount > 1 ? "s" : "")}", FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#059669") }
                }
            }
        };
        card.Content = content;
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await DisplayAlert(p.Name, $"Dernière visite : {p.LastVisit:dd MMM yyyy}\nMotif : {(string.IsNullOrEmpty(p.LastReason) ? "—" : p.LastReason)}\nConsultations : {p.VisitCount}", "Fermer"))
        });
        return card;
    }

    private View BuildListCard(PatientRow p)
    {
        var initials = GetInitials(p.Name);
        var avatar = new Frame
        {
            WidthRequest = 44, HeightRequest = 44, CornerRadius = 22,
            BackgroundColor = Color.FromArgb("#CCFBF1"), BorderColor = Color.FromArgb("#5EEAD4"),
            HasShadow = false, Padding = 0,
            Content = new Label { Text = initials, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        var info = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, Spacing = 2,
            Children =
            {
                new Label { Text = p.Name, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), LineBreakMode = LineBreakMode.TailTruncation },
                new Label { Text = $"Dernière visite : {p.LastVisit:dd MMM yyyy}", FontSize = 11, TextColor = Color.FromArgb("#6B7280") },
                new Label { Text = string.IsNullOrEmpty(p.LastReason) ? (p.ConsultationType == "Video" ? "📹 Téléconsultation" : "🏥 Consultation") : p.LastReason, FontSize = 11, TextColor = Color.FromArgb("#0D9488"), LineBreakMode = LineBreakMode.TailTruncation }
            }
        };
        var visitBadge = new Frame
        {
            BackgroundColor = Color.FromArgb("#F0FDF4"), CornerRadius = 10,
            Padding = new Thickness(8, 4), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = $"{p.VisitCount}x", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#059669") }
        };
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };
        row.Children.Add(avatar);
        Grid.SetColumn(info, 1); row.Children.Add(info);
        Grid.SetColumn(visitBadge, 2); row.Children.Add(visitBadge);

        var card = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 10,
            Padding = new Thickness(12, 10), HasShadow = false,
            BorderColor = Color.FromArgb("#E5E7EB"), Content = row
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await DisplayAlert(p.Name, $"Dernière visite : {p.LastVisit:dd MMM yyyy}\nMotif : {(string.IsNullOrEmpty(p.LastReason) ? "—" : p.LastReason)}\nConsultations : {p.VisitCount}", "Fermer"))
        });
        return card;
    }

    private static string GetInitials(string name) =>
        string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpper(w[0]).ToString()));
}

// ===== DOCTOR WORKFLOW PAGE =====
public class DoctorWorkflowPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _doctorId;

    private VerticalStackLayout _rdvStack  = new();
    private VerticalStackLayout _waitStack = new();
    private VerticalStackLayout _examStack = new();
    private VerticalStackLayout _doneStack = new();

    private const double ColW = 300.0;

    public DoctorWorkflowPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.White;
        BuildLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private void BuildLayout()
    {
        var fullName   = Preferences.Get("FullName", "Médecin");
        var firstName  = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? fullName;

        // ── Header bar ─────────────────────────────────────────
        var backBtn = new Label
        {
            Text = "←", FontSize = 22, TextColor = Color.FromArgb("#0D9488"),
            VerticalOptions = LayoutOptions.Center, Padding = new Thickness(0, 0, 6, 0)
        };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PopAsync())
        });

        var calBtn = new Label
        {
            Text = "📅", FontSize = 20, TextColor = Color.FromArgb("#0D9488"),
            VerticalOptions = LayoutOptions.Center, Padding = new Thickness(4, 0)
        };
        calBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorCalendarPage(_api)))
        });

        var ordoBtn = new Label
        {
            Text = "📋", FontSize = 20, TextColor = Color.FromArgb("#0D9488"),
            VerticalOptions = LayoutOptions.Center, Padding = new Thickness(4, 0)
        };
        ordoBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorOrdonnancesPage(_api)))
        });

        var headerGrid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(20, 48, 20, 14),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 6
        };
        headerGrid.Children.Add(backBtn);

        var greetLabel = new Label
        {
            VerticalOptions = LayoutOptions.Center,
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = "Bonjour Dr. ", FontSize = 15, TextColor = Color.FromArgb("#374151") },
                    new Span { Text = firstName + ", ", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E3A5F") },
                    new Span { Text = "Voici ce qui est prévu pour aujourd'hui :", FontSize = 13, TextColor = Color.FromArgb("#6B7280") }
                }
            }
        };
        Grid.SetColumn(greetLabel, 1); headerGrid.Children.Add(greetLabel);
        Grid.SetColumn(ordoBtn, 2); headerGrid.Children.Add(ordoBtn);
        Grid.SetColumn(calBtn, 3); headerGrid.Children.Add(calBtn);

        // ── 4-column workflow area ─────────────────────────────
        _rdvStack  = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(12, 12, 12, 28) };
        _waitStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(12, 12, 12, 28) };
        _examStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(12, 12, 12, 28) };
        _doneStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(12, 12, 12, 28) };

        var colsRow = new HorizontalStackLayout { Spacing = 0 };
        colsRow.Children.Add(MakeColumn("🗓️", "Rendez-vous",         _rdvStack,  "#0D9488"));
        colsRow.Children.Add(ColDivider());
        colsRow.Children.Add(MakeColumn("💺", "Salle d'attente",      _waitStack, "#0D9488"));
        colsRow.Children.Add(ColDivider());
        colsRow.Children.Add(MakeColumn("🩺", "En examen",            _examStack, "#0D9488"));
        colsRow.Children.Add(ColDivider());
        colsRow.Children.Add(MakeColumn("✅", "Rendez-vous finalisé", _doneStack, "#10B981"));

        var bodyScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Both,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = colsRow
        };

        // ── Page grid ──────────────────────────────────────────
        var pageGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = 1 },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        pageGrid.Children.Add(headerGrid);

        var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") };
        Grid.SetRow(sep, 1); pageGrid.Children.Add(sep);

        Grid.SetRow(bodyScroll, 2); pageGrid.Children.Add(bodyScroll);

        Content = pageGrid;
    }

    private static VerticalStackLayout MakeColumn(string icon, string title, VerticalStackLayout listStack, string accentHex)
    {
        var headerGrid = new Grid
        {
            WidthRequest = ColW,
            Padding = new Thickness(14, 13, 14, 10),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8,
            BackgroundColor = Colors.White
        };
        headerGrid.Children.Add(new Label { Text = icon, FontSize = 22, VerticalOptions = LayoutOptions.Center });
        var titleLbl = new Label
        {
            Text = title, FontSize = 14, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E3A5F"), VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(titleLbl, 1); headerGrid.Children.Add(titleLbl);

        var accentLine = new BoxView { HeightRequest = 2, WidthRequest = ColW, Color = Color.FromArgb(accentHex) };

        listStack.WidthRequest = ColW;

        return new VerticalStackLayout
        {
            WidthRequest = ColW, Spacing = 0,
            BackgroundColor = Color.FromArgb("#FAFAFA"),
            Children = { headerGrid, accentLine, listStack }
        };
    }

    private static BoxView ColDivider() => new BoxView
    {
        WidthRequest = 1, Color = Color.FromArgb("#E5E7EB"),
        VerticalOptions = LayoutOptions.Fill
    };

    private async Task LoadAsync()
    {
        var today     = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
        var now       = DateTime.Now.TimeOfDay;

        List<AppointmentDto> all = new();
        try { all = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, weekStart); } catch { }

        var todayAppts = all.Where(a => a.AppointmentDate.Date == today).OrderBy(a => a.StartTime).ToList();

        var rdv  = todayAppts.Where(a => a.Status == "Scheduled").ToList();
        var wait = todayAppts.Where(a => a.Status == "Confirmed" && a.StartTime > now).ToList();
        var exam = todayAppts.Where(a => a.Status == "Confirmed" && a.StartTime <= now).ToList();
        var done = todayAppts.Where(a => a.Status == "Completed").ToList();

        FillStack(_rdvStack,  rdv,  "🗓️", "Aucun rendez-vous aujourd'hui",         "Scheduled");
        FillStack(_waitStack, wait, "💺", "Aucun patient dans la salle d'attente", "Waiting");
        FillStack(_examStack, exam, "🩺", "Aucun patient dans la salle d'examen",  "InProgress");
        FillStack(_doneStack, done, "✅", "Aucun rendez-vous finalisé",              "Completed");
    }

    private void FillStack(VerticalStackLayout stack, List<AppointmentDto> appts,
                           string emptyIcon, string emptyMsg, string colType)
    {
        stack.Children.Clear();

        if (!appts.Any())
        {
            stack.Children.Add(new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10, Padding = new Thickness(16, 52),
                Children =
                {
                    new Label { Text = emptyIcon, FontSize = 52, TextColor = Color.FromArgb("#D1D5DB"), HorizontalOptions = LayoutOptions.Center },
                    new Label
                    {
                        Text = emptyMsg, FontSize = 12, TextColor = Color.FromArgb("#9CA3AF"),
                        HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            });
            return;
        }

        foreach (var appt in appts)
            stack.Children.Add(BuildCard(appt, colType));
    }

    private View BuildCard(AppointmentDto appt, string colType)
    {
        var captured = appt;

        var initials = string.Join("", (appt.PatientName ?? "?")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => char.ToUpper(w[0]).ToString()));

        var avatar = new Frame
        {
            WidthRequest = 38, HeightRequest = 38, CornerRadius = 19,
            BackgroundColor = Color.FromArgb("#CCFBF1"), BorderColor = Color.FromArgb("#5EEAD4"),
            HasShadow = false, Padding = 0,
            Content = new Label
            {
                Text = initials, FontSize = 13, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D9488"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            }
        };

        var infoCol = new VerticalStackLayout
        {
            Spacing = 2, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = appt.PatientName ?? "Patient", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), LineBreakMode = LineBreakMode.TailTruncation },
                new Label { Text = $"{appt.StartTime:hh\\:mm} – {appt.EndTime:hh\\:mm}", FontSize = 11, TextColor = Color.FromArgb("#6B7280") },
                new Label { Text = appt.ConsultationType == "Video" ? "📹 Téléconsultation" : "🏥 Présentiel", FontSize = 10, TextColor = Color.FromArgb("#0D9488") }
            }
        };

        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        topRow.Children.Add(avatar);
        Grid.SetColumn(infoCol, 1); topRow.Children.Add(infoCol);

        var cardContent = new VerticalStackLayout { Spacing = 0, Children = { topRow } };

        if (colType == "Scheduled")
        {
            var btn = new Frame
            {
                BackgroundColor = Color.FromArgb("#CCFBF1"), CornerRadius = 6,
                Padding = new Thickness(10, 4), HasShadow = false, BorderColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, 8, 0, 0),
                Content = new Label { Text = "Confirmer →", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D9488") }
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => { await _api.UpdateAppointmentStatusAsync(captured.Id, "Confirmed"); await LoadAsync(); })
            });
            cardContent.Children.Add(btn);
        }
        else if (colType == "InProgress")
        {
            var btn = new Frame
            {
                BackgroundColor = Color.FromArgb("#EDE9FE"), CornerRadius = 6,
                Padding = new Thickness(10, 4), HasShadow = false, BorderColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, 8, 0, 0),
                Content = new Label { Text = "Terminer →", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#5B21B6") }
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => { await _api.UpdateAppointmentStatusAsync(captured.Id, "Completed"); await LoadAsync(); })
            });
            cardContent.Children.Add(btn);
        }

        return new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 10,
            Padding = new Thickness(12, 10), HasShadow = false,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Content = cardContent
        };
    }
}

// ===== DOCTOR ORDONNANCES PAGE =====
public class DoctorOrdonnancesPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _doctorId;

    private List<(string PatientName, PrescriptionDto Prescription)> _allPrescriptions = new();
    private List<(string PatientName, PrescriptionDto Prescription)> _filtered = new();
    public  List<(Guid? UserId, string Name, List<AppointmentDto> Appointments)> RecentPatients { get; private set; } = new();

    private string    _searchText = "";
    private DateTime? _dateFilter = null;

    private VerticalStackLayout _bodyStack  = new();
    private ActivityIndicator   _spinner    = new();
    private Label               _countLabel = new();

    public DoctorOrdonnancesPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.White;
        BuildLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private void BuildLayout()
    {
        // ── Header ─────────────────────────────────────────────
        var backBtn = new Label { Text = "←", FontSize = 22, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        _countLabel = new Label { Text = "0", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };
        var badge = new Frame
        {
            BackgroundColor = Color.FromArgb("#0D9488"), CornerRadius = 12,
            Padding = new Thickness(8, 3), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center, Content = _countLabel
        };

        var titleRow = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        titleRow.Children.Add(new Label { Text = "📋", FontSize = 22, VerticalOptions = LayoutOptions.Center });
        titleRow.Children.Add(new Label { Text = "Ordonnances", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E3A5F"), VerticalOptions = LayoutOptions.Center });
        titleRow.Children.Add(badge);

        var newBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#0D9488"), CornerRadius = 8,
            Padding = new Thickness(12, 7), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "+ Nouvelle", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
        };
        newBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Navigation.PushAsync(new DoctorAddPrescriptionPage(_api, this)))
        });

        var toggleLbl = new Label { Text = "Liste ⊞", FontSize = 12, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center };
        var toggle = new Frame { BackgroundColor = Color.FromArgb("#F3F4F6"), CornerRadius = 8, Padding = new Thickness(10, 7), HasShadow = false, BorderColor = Color.FromArgb("#E5E7EB"), VerticalOptions = LayoutOptions.Center, Content = toggleLbl };

        var headerGrid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 48, 16, 12),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        headerGrid.Children.Add(backBtn);
        Grid.SetColumn(titleRow, 1); headerGrid.Children.Add(titleRow);
        Grid.SetColumn(newBtn, 2);   headerGrid.Children.Add(newBtn);
        Grid.SetColumn(toggle, 3);   headerGrid.Children.Add(toggle);

        // ── Filters ────────────────────────────────────────────
        var patientEntry = new Entry { Placeholder = "Patient (nom/prénom, téléphone...)", FontSize = 13, BackgroundColor = Colors.Transparent };
        patientEntry.TextChanged += (s, e) => { _searchText = e.NewTextValue ?? ""; ApplyFilter(); };

        var patientFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#F9FAFB"), CornerRadius = 10,
            Padding = new Thickness(12, 2), HasShadow = false,
            BorderColor = Color.FromArgb("#E5E7EB"), Content = patientEntry,
            Margin = new Thickness(16, 10, 16, 6)
        };

        var datePicker = new DatePicker { Format = "dd/MM/yyyy", BackgroundColor = Colors.Transparent, Date = DateTime.Today };
        datePicker.DateSelected += (s, e) => { _dateFilter = e.NewDate; ApplyFilter(); };

        var clearDate = new Label { Text = "✕ Effacer", FontSize = 11, TextColor = Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center };
        clearDate.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { _dateFilter = null; datePicker.Date = DateTime.Today; ApplyFilter(); })
        });

        var dateRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Padding = new Thickness(12, 4)
        };
        dateRow.Children.Add(datePicker);
        Grid.SetColumn(clearDate, 1); dateRow.Children.Add(clearDate);

        var dateFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#F9FAFB"), CornerRadius = 10,
            Padding = 0, HasShadow = false, BorderColor = Color.FromArgb("#E5E7EB"),
            Content = dateRow, Margin = new Thickness(16, 0, 16, 6)
        };

        var motifSection = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 10,
            Padding = new Thickness(14, 12), HasShadow = false,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Margin = new Thickness(16, 0, 16, 10),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Afficher les ordonnances par motif", FontSize = 12, TextColor = Color.FromArgb("#6B7280") },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            new Frame
                            {
                                WidthRequest = 16, HeightRequest = 16, CornerRadius = 3,
                                BackgroundColor = Color.FromArgb("#0D9488"), BorderColor = Color.FromArgb("#0D9488"),
                                HasShadow = false, Padding = 0,
                                Content = new Label { Text = "✓", FontSize = 9, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                            },
                            new Label { Text = "Tous", FontSize = 13, TextColor = Color.FromArgb("#374151"), VerticalOptions = LayoutOptions.Center }
                        }
                    }
                }
            }
        };

        // ── Body ───────────────────────────────────────────────
        _spinner   = new ActivityIndicator { IsRunning = false, IsVisible = false, Color = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 40) };
        _bodyStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16, 8, 16, 28) };

        var mainScroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    patientFrame, dateFrame, motifSection,
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") },
                    _spinner, _bodyStack
                }
            }
        };

        var outer = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = 1 },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        outer.Children.Add(headerGrid);
        var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") };
        Grid.SetRow(sep, 1); outer.Children.Add(sep);
        Grid.SetRow(mainScroll, 2); outer.Children.Add(mainScroll);

        Content = outer;
    }

    public async Task LoadAsync()
    {
        _spinner.IsRunning = true; _spinner.IsVisible = true;
        _bodyStack.Children.Clear();

        var today = DateTime.Today;
        List<AppointmentDto> allAppts = new();
        try
        {
            var tasks = Enumerable.Range(0, 8)
                .Select(w => _api.GetDoctorWeekAppointmentsAsync(_doctorId, today.AddDays(-(w * 7) - (int)today.DayOfWeek + 1)));
            var results = await Task.WhenAll(tasks);
            allAppts = results.SelectMany(r => r).ToList();
        }
        catch { }

        RecentPatients = allAppts
            .Where(a => a.PatientName != null && a.PatientUserId.HasValue)
            .GroupBy(a => a.PatientUserId!.Value)
            .Select(g =>
            {
                var appts = g.OrderByDescending(x => x.AppointmentDate).ToList();
                return (UserId: (Guid?)g.Key, Name: appts.First().PatientName ?? "", Appointments: appts);
            })
            .ToList();

        _allPrescriptions = new();
        try
        {
            var presTasks = RecentPatients
                .Where(p => p.UserId.HasValue)
                .Select(async p =>
                {
                    var list = await _api.GetPatientPrescriptionsAsync(p.UserId!.Value);
                    return list.Select(pr => (p.Name, Prescription: pr));
                });
            var presResults = await Task.WhenAll(presTasks);
            _allPrescriptions = presResults
                .SelectMany(r => r)
                .Where(r => !string.IsNullOrEmpty(r.Prescription.Prescription))
                .OrderByDescending(r => r.Prescription.AppointmentDate)
                .ToList();
        }
        catch { }

        _spinner.IsRunning = false; _spinner.IsVisible = false;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filtered = _allPrescriptions
            .Where(p => string.IsNullOrEmpty(_searchText) || p.PatientName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .Where(p => _dateFilter == null || p.Prescription.AppointmentDate.Date == _dateFilter.Value.Date)
            .ToList();

        _countLabel.Text = _filtered.Count.ToString();
        MainThread.BeginInvokeOnMainThread(RenderPrescriptions);
    }

    private void RenderPrescriptions()
    {
        _bodyStack.Children.Clear();

        if (!_filtered.Any())
        {
            _bodyStack.Children.Add(new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center, Spacing = 14, Padding = new Thickness(0, 60),
                Children =
                {
                    new Label { Text = "👩‍💻", FontSize = 72, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Aucune donnée disponible", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#9CA3AF"), HorizontalOptions = LayoutOptions.Center }
                }
            });
            return;
        }

        _bodyStack.Children.Add(new Label
        {
            Text = $"{_filtered.Count} ordonnance{(_filtered.Count > 1 ? "s" : "")}",
            FontSize = 12, TextColor = Color.FromArgb("#6B7280"), Margin = new Thickness(0, 0, 0, 4)
        });

        foreach (var (patientName, prescription) in _filtered)
            _bodyStack.Children.Add(BuildCard(patientName, prescription));
    }

    private View BuildCard(string patientName, PrescriptionDto dto)
    {
        var initials = string.Join("", patientName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => char.ToUpper(w[0]).ToString()));

        var avatar = new Frame
        {
            WidthRequest = 46, HeightRequest = 46, CornerRadius = 23,
            BackgroundColor = Color.FromArgb("#EFF6FF"), BorderColor = Color.FromArgb("#BFDBFE"),
            HasShadow = false, Padding = 0,
            Content = new Label { Text = initials, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E40AF"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        var preview = dto.Prescription.Length > 100
            ? dto.Prescription[..100] + "…"
            : dto.Prescription;

        var infoCol = new VerticalStackLayout
        {
            Spacing = 3, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = patientName, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827") },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label { Text = $"📅 {dto.AppointmentDate:dd MMM yyyy}", FontSize = 11, TextColor = Color.FromArgb("#6B7280") },
                        new Label { Text = dto.TypeIcon, FontSize = 13 }
                    }
                },
                new Frame
                {
                    BackgroundColor = Color.FromArgb("#F8FAFC"), CornerRadius = 8,
                    Padding = new Thickness(10, 6), HasShadow = false, BorderColor = Color.FromArgb("#E2E8F0"),
                    Margin = new Thickness(0, 4, 0, 0),
                    Content = new Label { Text = preview, FontSize = 12, TextColor = Color.FromArgb("#374151"), MaxLines = 3, LineBreakMode = LineBreakMode.TailTruncation }
                }
            }
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        row.Children.Add(avatar);
        Grid.SetColumn(infoCol, 1); row.Children.Add(infoCol);

        var card = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 12,
            Padding = new Thickness(14, 12), HasShadow = true,
            BorderColor = Color.FromArgb("#E5E7EB"), Content = row
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await DisplayAlert($"📋 {patientName}", dto.Prescription, "Fermer"))
        });
        return card;
    }
}

public class DoctorAddPrescriptionPage : ContentPage
{
    private readonly ApiService _api;
    private readonly DoctorOrdonnancesPage _parent;
    private readonly Guid _doctorId;

    private DoctorDetailDto? _doctorProfile;
    private List<(Guid UserId, string Name, List<AppointmentDto> Appointments)> _allPatients = new();

    private (Guid UserId, string Name, List<AppointmentDto> Appointments)? _selectedPatient;
    private AppointmentDto? _selectedAppt;

    private Entry               _searchEntry   = new();
    private VerticalStackLayout _suggestionList = new();
    private Frame               _suggestionFrame = new();
    private Frame               _selectedBadge  = new();
    private Label               _selectedBadgeLbl = new();
    private VerticalStackLayout _apptCards      = new();
    private Frame               _apptSection    = new();
    private Editor              _prescEditor    = new();
    private ActivityIndicator   _spinner        = new();
    private Label               _spinnerLbl     = new();

    public DoctorAddPrescriptionPage(ApiService api, DoctorOrdonnancesPage parent)
    {
        _api      = api;
        _parent   = parent;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private void BuildLayout()
    {
        var backBtn = new Label { Text = "← Annuler", FontSize = 14, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        var pdfBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#F0FDF4"), CornerRadius = 8,
            Padding = new Thickness(12, 7), HasShadow = false, BorderColor = Color.FromArgb("#6EE7B7"),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "📄 PDF", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#059669") }
        };
        pdfBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await PreviewPdfAsync()) });

        var saveBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#0D9488"), CornerRadius = 8,
            Padding = new Thickness(14, 7), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "Enregistrer", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
        };
        saveBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await SaveAsync()) });

        var hdr = new Grid
        {
            BackgroundColor = Colors.White, Padding = new Thickness(16, 48, 16, 14),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };
        hdr.Children.Add(backBtn);
        var titleLbl = new Label { Text = "📋 Nouvelle ordonnance", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E3A5F"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(titleLbl, 1); hdr.Children.Add(titleLbl);
        Grid.SetColumn(pdfBtn,   2); hdr.Children.Add(pdfBtn);
        Grid.SetColumn(saveBtn,  3); hdr.Children.Add(saveBtn);

        _spinner    = new ActivityIndicator { IsRunning = false, IsVisible = false, Color = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 16) };
        _spinnerLbl = new Label { Text = "Chargement des patients…", FontSize = 12, TextColor = Color.FromArgb("#6B7280"), IsVisible = false, HorizontalOptions = LayoutOptions.Center };

        _searchEntry = new Entry { Placeholder = "🔍 Nom du patient…", FontSize = 13, BackgroundColor = Colors.Transparent };
        _searchEntry.TextChanged += OnSearchChanged;

        _suggestionList  = new VerticalStackLayout { Spacing = 0 };
        _suggestionFrame = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 10,
            Padding = new Thickness(0), HasShadow = true, IsVisible = false,
            BorderColor = Color.FromArgb("#D1D5DB"), Margin = new Thickness(0, 2, 0, 0),
            Content = _suggestionList
        };

        _selectedBadgeLbl = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#064E3B"), VerticalOptions = LayoutOptions.Center };
        var clearPat = new Label { Text = "✕", FontSize = 16, TextColor = Color.FromArgb("#6B7280"), VerticalOptions = LayoutOptions.Center };
        clearPat.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(ClearPatient) });
        var badgeRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
        badgeRow.Children.Add(_selectedBadgeLbl);
        Grid.SetColumn(clearPat, 1); badgeRow.Children.Add(clearPat);

        _selectedBadge = new Frame
        {
            BackgroundColor = Color.FromArgb("#ECFDF5"), CornerRadius = 10,
            Padding = new Thickness(14, 10), HasShadow = false,
            BorderColor = Color.FromArgb("#6EE7B7"), IsVisible = false, Content = badgeRow
        };

        _apptCards   = new VerticalStackLayout { Spacing = 8 };
        _apptSection = new Frame
        {
            BackgroundColor = Color.FromArgb("#FAFAFA"), CornerRadius = 10,
            Padding = new Thickness(12), HasShadow = false,
            BorderColor = Color.FromArgb("#E5E7EB"), IsVisible = false, Content = _apptCards
        };

        _prescEditor = new Editor
        {
            Placeholder = "Contenu de l'ordonnance…\n\nEx:\nParacétamol 1g — 3×/jour pendant 5 jours\nIbuprofène 400mg — 2×/jour pendant 3 jours",
            FontSize = 13, BackgroundColor = Colors.White, HeightRequest = 200,
            AutoSize = EditorAutoSizeOption.Disabled
        };

        Frame Frm(View v) => new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 10, Padding = new Thickness(12, 4),
            HasShadow = false, BorderColor = Color.FromArgb("#E5E7EB"),
            Margin = new Thickness(0, 0, 0, 4), Content = v
        };
        Label Sec(string t) => new Label
        {
            Text = t, FontSize = 12, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#374151"), Margin = new Thickness(0, 10, 0, 5)
        };

        var scroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 10, 16, 40), Spacing = 0,
                Children =
                {
                    _spinner, _spinnerLbl,
                    Sec("Patient"),
                    Frm(_searchEntry),
                    _suggestionFrame,
                    _selectedBadge,
                    Sec("Rendez-vous"),
                    _apptSection,
                    Sec("Contenu de l'ordonnance"),
                    new Frame
                    {
                        BackgroundColor = Colors.White, CornerRadius = 10,
                        Padding = new Thickness(12, 8), HasShadow = false,
                        BorderColor = Color.FromArgb("#E5E7EB"), Content = _prescEditor
                    }
                }
            }
        };

        var outer = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = 1 },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        outer.Children.Add(hdr);
        var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") };
        Grid.SetRow(sep, 1); outer.Children.Add(sep);
        Grid.SetRow(scroll, 2); outer.Children.Add(scroll);
        Content = outer;
    }

    private async Task LoadAsync()
    {
        _spinner.IsRunning = _spinner.IsVisible = _spinnerLbl.IsVisible = true;
        try { _doctorProfile = await _api.GetDoctorByIdAsync(_doctorId); } catch { }

        var today = DateTime.Today;
        List<AppointmentDto> all = new();
        try
        {
            var tasks = Enumerable.Range(0, 8)
                .Select(w => _api.GetDoctorWeekAppointmentsAsync(_doctorId,
                    today.AddDays(-(w * 7) - (int)today.DayOfWeek + 1)));
            var results = await Task.WhenAll(tasks);
            all = results.SelectMany(r => r).ToList();
        }
        catch { }

        _allPatients = all
            .Where(a => a.PatientName != null && a.PatientUserId.HasValue)
            .GroupBy(a => a.PatientUserId!.Value)
            .Select(g =>
            {
                var appts = g.OrderByDescending(x => x.AppointmentDate).ToList();
                return (UserId: g.Key, Name: appts.First().PatientName ?? "", Appointments: appts);
            })
            .OrderBy(p => p.Name)
            .ToList();

        _spinner.IsRunning = _spinner.IsVisible = _spinnerLbl.IsVisible = false;
        RebuildSuggestions(_searchEntry.Text ?? "");
    }

    private void OnSearchChanged(object? s, TextChangedEventArgs e) => RebuildSuggestions(e.NewTextValue ?? "");

    private void RebuildSuggestions(string q)
    {
        var filtered = string.IsNullOrWhiteSpace(q)
            ? _allPatients.ToList()
            : _allPatients.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        _suggestionList.Children.Clear();

        if (!filtered.Any() || (_selectedPatient != null && string.IsNullOrWhiteSpace(q)))
        {
            _suggestionFrame.IsVisible = false;
            return;
        }

        foreach (var patient in filtered.Take(7))
        {
            var p = patient;
            var row = new Grid
            {
                Padding = new Thickness(14, 11), BackgroundColor = Colors.White,
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            var info = new VerticalStackLayout
            {
                Spacing = 1,
                Children =
                {
                    new Label { Text = p.Name, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E3A5F") },
                    new Label { Text = $"{p.Appointments.Count} rendez-vous", FontSize = 11, TextColor = Color.FromArgb("#6B7280") }
                }
            };
            row.Children.Add(info);
            var arrow = new Label { Text = "›", FontSize = 20, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(arrow, 1); row.Children.Add(arrow);
            row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectPatient(p)) });
            _suggestionList.Children.Add(row);
            _suggestionList.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6") });
        }

        if (filtered.Count > 7)
            _suggestionList.Children.Add(new Label
            {
                Text = $"  +{filtered.Count - 7} autre(s)… Affinez la recherche.",
                FontSize = 11, TextColor = Color.FromArgb("#9CA3AF"),
                Padding = new Thickness(14, 8)
            });

        _suggestionFrame.IsVisible = true;
    }

    private void SelectPatient((Guid UserId, string Name, List<AppointmentDto> Appointments) p)
    {
        _selectedPatient = p;
        _selectedAppt    = null;
        _searchEntry.Text = "";
        _suggestionFrame.IsVisible = false;
        _selectedBadgeLbl.Text   = $"{p.Name}  ({p.Appointments.Count} rendez-vous)";
        _selectedBadge.IsVisible = true;
        BuildApptCards(p.Appointments);
    }

    private void ClearPatient()
    {
        _selectedPatient         = null;
        _selectedAppt            = null;
        _selectedBadge.IsVisible = false;
        _apptSection.IsVisible   = false;
        _apptCards.Children.Clear();
    }

    private void BuildApptCards(List<AppointmentDto> appts)
    {
        _apptCards.Children.Clear();
        if (!appts.Any())
        {
            _apptCards.Children.Add(new Label { Text = "Aucun rendez-vous trouvé.", FontSize = 12, TextColor = Color.FromArgb("#9CA3AF") });
            _apptSection.IsVisible = true;
            return;
        }
        foreach (var a in appts.Take(10))
            _apptCards.Children.Add(MakeApptCard(a));
        _apptSection.IsVisible = true;
    }

    private Frame MakeApptCard(AppointmentDto a)
    {
        var isSelected = _selectedAppt?.Id == a.Id;
        var dot   = new BoxView { WidthRequest = 8, HeightRequest = 8, CornerRadius = 4, BackgroundColor = a.StatusColor, VerticalOptions = LayoutOptions.Center };
        var check = new Label { Text = isSelected ? "✓" : "○", FontSize = 18, TextColor = isSelected ? Color.FromArgb("#0D9488") : Color.FromArgb("#D1D5DB"), VerticalOptions = LayoutOptions.Center };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };
        grid.Children.Add(dot);
        var info = new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label { Text = $"📅 {a.AppointmentDate:dd/MM/yyyy}  🕐 {a.StartTime:hh\\:mm}", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E3A5F") },
                new Label { Text = $"{a.TypeIcon} {a.ConsultationType}  ·  {a.Reason}", FontSize = 11, TextColor = Color.FromArgb("#6B7280") },
                new Label { Text = a.Status, FontSize = 11, TextColor = a.StatusColor, FontAttributes = FontAttributes.Bold }
            }
        };
        Grid.SetColumn(info,  1); grid.Children.Add(info);
        Grid.SetColumn(check, 2); grid.Children.Add(check);

        var card = new Frame
        {
            BackgroundColor = isSelected ? Color.FromArgb("#ECFDF5") : Colors.White,
            CornerRadius = 10, Padding = new Thickness(12, 10), HasShadow = false,
            BorderColor = isSelected ? Color.FromArgb("#0D9488") : Color.FromArgb("#E5E7EB"),
            Content = grid
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                _selectedAppt = a;
                BuildApptCards(_selectedPatient!.Value.Appointments);
            })
        });
        return card;
    }

    private async Task PreviewPdfAsync()
    {
        if (_selectedPatient == null) { await DisplayAlert("Patient requis", "Sélectionnez d'abord un patient.", "OK"); return; }
        if (_selectedAppt    == null) { await DisplayAlert("RDV requis",     "Sélectionnez d'abord un rendez-vous.", "OK"); return; }
        var html = PrescriptionPreviewPage.BuildDoctorHtml(
            _doctorProfile, _selectedPatient.Value.Name,
            _selectedAppt, _prescEditor.Text?.Trim() ?? "(Contenu non saisi)");
        await Navigation.PushAsync(new PrescriptionPreviewPage(html,
            $"Ordonnance_{_selectedPatient.Value.Name.Replace(" ", "_")}_{DateTime.Today:yyyyMMdd}"));
    }

    private async Task SaveAsync()
    {
        if (_selectedPatient == null) { await DisplayAlert("Champ requis", "Veuillez sélectionner un patient.", "OK"); return; }
        if (_selectedAppt    == null) { await DisplayAlert("Champ requis", "Veuillez sélectionner un rendez-vous.", "OK"); return; }
        if (string.IsNullOrWhiteSpace(_prescEditor.Text)) { await DisplayAlert("Champ requis", "Veuillez saisir le contenu de l'ordonnance.", "OK"); return; }

        var ok = await _api.WritePrescriptionAsync(_selectedAppt.Id, _prescEditor.Text.Trim());
        if (ok)
        {
            await DisplayAlert("✓ Succès", "Ordonnance enregistrée avec succès.", "OK");
            await Navigation.PopAsync();
            await _parent.LoadAsync();
        }
        else
            await DisplayAlert("Erreur", "Impossible d'enregistrer l'ordonnance.", "Réessayer");
    }
}

// ═══════════════════════════════════════════════════════
// PRESCRIPTION PREVIEW & PRINT PAGE
// ═══════════════════════════════════════════════════════
public class PrescriptionPreviewPage : ContentPage
{
    private readonly string _html;
    private readonly string _fileName;

    public PrescriptionPreviewPage(string html, string fileName)
    {
        _html = html; _fileName = fileName;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.White;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var back = new Label { Text = "← Retour", FontSize = 14, TextColor = Color.FromArgb("#0D9488"), VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        var shareBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#0D9488"), CornerRadius = 8,
            Padding = new Thickness(14, 8), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "🖨 Imprimer / Partager", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
        };
        shareBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await ShareAsync()) });

        var hdr = new Grid
        {
            BackgroundColor = Colors.White, Padding = new Thickness(16, 48, 16, 14),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };
        hdr.Children.Add(back);
        var t = new Label { Text = "Aperçu de l'ordonnance", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E3A5F"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(t,        1); hdr.Children.Add(t);
        Grid.SetColumn(shareBtn, 2); hdr.Children.Add(shareBtn);

        var wv = new WebView { Source = new HtmlWebViewSource { Html = _html }, VerticalOptions = LayoutOptions.Fill };
        var root = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = 1 },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        root.Children.Add(hdr);
        var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB") };
        Grid.SetRow(sep, 1); root.Children.Add(sep);
        Grid.SetRow(wv,  2); root.Children.Add(wv);
        Content = root;
    }

    private async Task ShareAsync()
    {
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"{_fileName}.html");
            await File.WriteAllTextAsync(path, _html);
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Ordonnance médicale — MediCare+",
                File  = new ShareFile(path, "text/html")
            });
        }
        catch (Exception ex) { await DisplayAlert("Erreur", ex.Message, "OK"); }
    }

    // ── Static HTML builders ──────────────────────────────────────────

    public static string BuildDoctorHtml(DoctorDetailDto? doc, string patientName, AppointmentDto appt, string text)
    {
        var name     = doc != null ? $"Dr. {doc.FirstName} {doc.LastName}" : "Médecin";
        var spec     = doc?.SpecialtyName ?? "";
        var hosp     = doc?.HospitalName ?? "";
        var addr     = $"{doc?.Address ?? ""}{(doc?.City != null ? ", " + doc.City : "")}".Trim().TrimStart(',').Trim();
        var phone    = doc?.PhoneNumber ?? "";
        var initials = (string.IsNullOrEmpty(doc?.FirstName) ? "M" : doc.FirstName[0].ToString()) +
                       (string.IsNullOrEmpty(doc?.LastName)  ? "" : doc.LastName[0].ToString());
        var safe     = text.Replace("<", "&lt;").Replace(">", "&gt;");
        var date     = DateTime.Today.ToString("dd/MM/yyyy");
        var apptDt   = $"{appt.AppointmentDate:dd/MM/yyyy} à {appt.StartTime:hh\\:mm}";
        var avatar   = !string.IsNullOrEmpty(doc?.ProfileImageUrl)
            ? $"<img src='{doc!.ProfileImageUrl}' class='av' onerror='this.style.display=\"none\"'>"
            : $"<div class='av-ph'>{initials}</div>";
        return BuildHtmlTemplate(avatar, name, spec, hosp, addr, phone, patientName, date, apptDt, safe, initials);
    }

    public static string BuildPatientHtml(PrescriptionDto dto)
    {
        var safe = dto.Prescription.Replace("<", "&lt;").Replace(">", "&gt;");
        var date = dto.AppointmentDate.ToString("dd/MM/yyyy");
        return BuildHtmlTemplate("<div class='av-ph'>Rx</div>",
            dto.DoctorName ?? "Médecin", dto.DoctorSpecialty ?? "",
            "", "", "", "Patient", date, date, safe, "Rx");
    }

    private static string BuildHtmlTemplate(
        string avatar, string docName, string spec,
        string hosp, string addr, string phone,
        string patient, string date, string apptInfo,
        string safeText, string initials)
    {
        var hospLine  = string.IsNullOrEmpty(hosp)  ? "" : $"<p>🏥 {hosp}</p>";
        var addrLine  = string.IsNullOrEmpty(addr)  ? "" : $"<p>📍 {addr}</p>";
        var phoneLine = string.IsNullOrEmpty(phone) ? "" : $"<p>📞 {phone}</p>";
        return $@"<!DOCTYPE html><html lang='fr'><head>
<meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Ordonnance</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0}}
body{{font-family:'Segoe UI',Arial,sans-serif;background:#fff;color:#1a1a1a;font-size:14px}}
.lh{{background:linear-gradient(135deg,#0D9488,#059669);color:#fff;padding:20px 22px;display:flex;align-items:center;gap:14px}}
.av{{width:64px;height:64px;border-radius:50%;border:3px solid rgba(255,255,255,.5);object-fit:cover;flex-shrink:0}}
.av-ph{{width:64px;height:64px;border-radius:50%;border:3px solid rgba(255,255,255,.5);background:rgba(255,255,255,.2);display:flex;align-items:center;justify-content:center;font-size:18px;font-weight:700;color:#fff;flex-shrink:0}}
.di h1{{font-size:18px;font-weight:700}}.di p{{font-size:12px;opacity:.88;margin-top:3px}}
.bar{{height:4px;background:linear-gradient(90deg,#0D9488,#34D399)}}
.bd{{padding:20px}}
.ir{{display:flex;gap:10px;margin-bottom:16px;flex-wrap:wrap}}
.ib{{flex:1;min-width:80px;background:#f0fdf4;border:1px solid #d1fae5;border-radius:8px;padding:10px}}
.lb{{font-size:10px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:3px}}
.vl{{font-size:13px;font-weight:600;color:#0f2540}}
.rh{{display:flex;align-items:center;gap:8px;margin:18px 0 10px}}
.rs{{font-size:34px;color:#0D9488;font-style:italic;font-weight:700;line-height:1}}
.rt{{font-size:15px;font-weight:700;color:#064E3B;text-transform:uppercase;letter-spacing:1px}}
.rb{{background:#f9fafb;border:1.5px solid #d1fae5;border-radius:10px;padding:16px;white-space:pre-wrap;font-size:14px;line-height:1.8;color:#065f46;min-height:80px}}
.ft{{margin-top:32px;display:flex;justify-content:flex-end}}
.sc{{text-align:center}}
.sc-c{{width:80px;height:80px;border:3px dashed #0D9488;border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 6px;color:#0D9488;font-size:11px;font-weight:600;text-align:center;padding:6px;line-height:1.3}}
.sc p{{font-size:11px;color:#374151}}.sc .dt{{font-size:10px;color:#9ca3af;margin-top:2px}}
@media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
</style>
</head><body>
<div class='lh'>
  {avatar}
  <div class='di'><h1>{docName}</h1><p>{spec}</p>{hospLine}{addrLine}{phoneLine}</div>
</div>
<div class='bar'></div>
<div class='bd'>
  <div class='ir'>
    <div class='ib'><div class='lb'>Patient</div><div class='vl'>{patient}</div></div>
    <div class='ib'><div class='lb'>Date</div><div class='vl'>{date}</div></div>
    <div class='ib'><div class='lb'>Rendez-vous</div><div class='vl'>{apptInfo}</div></div>
  </div>
  <div class='rh'><span class='rs'>&#x211E;</span><span class='rt'>Ordonnance médicale</span></div>
  <div class='rb'>{safeText}</div>
  <div class='ft'>
    <div class='sc'>
      <div class='sc-c'>{initials}</div>
      <p>Signature du médecin</p><p class='dt'>Le {date}</p>
    </div>
  </div>
</div>
</body></html>";
    }
}

// ═══════════════════════════════════════════════════════
// NEARBY PHARMACIES PAGE
// ═══════════════════════════════════════════════════════
public class NearbyPharmaciesPage : ContentPage
{
    private static readonly HttpClient _pharmHttp = new() { Timeout = TimeSpan.FromSeconds(20) };

    public NearbyPharmaciesPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0FDF4");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var spinner = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#059669"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var lbl     = new Label { Text = "Localisation en cours…", FontSize = 13, TextColor = Color.FromArgb("#059669"), HorizontalOptions = LayoutOptions.Center };
        Content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 12, Padding = new Thickness(20), Children = { spinner, lbl } };

        var back = new Label { Text = "←", FontSize = 22, TextColor = Color.FromArgb("#059669"), VerticalOptions = LayoutOptions.Center };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        var hdr = new Grid
        {
            BackgroundColor = Colors.White, Padding = new Thickness(16, 48, 16, 14),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        hdr.Children.Add(back);
        var ht = new Label { Text = "💊 Pharmacies proches", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#064E3B"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(ht, 1); hdr.Children.Add(ht);

        try
        {
            lbl.Text = "Obtention de votre position GPS…";
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(12)
            });
            if (location == null) { await DisplayAlert("GPS", "Impossible d'obtenir votre position. Activez le GPS.", "OK"); await Navigation.PopAsync(); return; }

            lbl.Text = "Recherche des pharmacies…";
            var lat = location.Latitude;
            var lon = location.Longitude;
            var ic  = System.Globalization.CultureInfo.InvariantCulture;

            _pharmHttp.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MediCarePlus/1.0");
            var query = $"[out:json];node[amenity=pharmacy](around:3000,{lat.ToString("F6", ic)},{lon.ToString("F6", ic)});out;";
            var url   = "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query);
            var json  = await _pharmHttp.GetStringAsync(url);
            var doc   = System.Text.Json.JsonDocument.Parse(json);

            var pharmacies = new List<(string Name, double Lat, double Lon, string? Phone, string? Hours)>();
            foreach (var el in doc.RootElement.GetProperty("elements").EnumerateArray())
            {
                if (!el.TryGetProperty("lat", out var eLat) || !el.TryGetProperty("lon", out var eLon)) continue;
                string name = "Pharmacie"; string? phone = null, hours = null;
                if (el.TryGetProperty("tags", out var tags))
                {
                    if (tags.TryGetProperty("name",          out var n))  name  = n.GetString()  ?? name;
                    if (tags.TryGetProperty("phone",         out var ph)) phone = ph.GetString();
                    if (tags.TryGetProperty("opening_hours", out var oh)) hours = oh.GetString();
                }
                pharmacies.Add((name, eLat.GetDouble(), eLon.GetDouble(), phone, hours));
            }

            double Dist(double pLat, double pLon) => Math.Sqrt(Math.Pow(pLat - lat, 2) + Math.Pow(pLon - lon, 2));
            pharmacies.Sort((a, b) => Dist(a.Lat, a.Lon).CompareTo(Dist(b.Lat, b.Lon)));

            var mapWv = new WebView
            {
                Source = new HtmlWebViewSource { Html = BuildMapHtml(lat, lon, pharmacies) },
                HeightRequest = 270
            };

            var listStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16, 12, 16, 32) };
            listStack.Children.Add(new Label
            {
                Text = pharmacies.Any()
                    ? $"{pharmacies.Count} pharmacie(s) dans un rayon de 3 km"
                    : "Aucune pharmacie trouvée dans un rayon de 3 km",
                FontSize = 13, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#064E3B"), Margin = new Thickness(0, 0, 0, 4)
            });

            foreach (var ph in pharmacies.Take(15))
            {
                var distM    = Dist(ph.Lat, ph.Lon) * 111000;
                var distText = distM < 1000 ? $"{distM:F0} m" : $"{distM / 1000:F1} km";
                var children = new VerticalStackLayout { Spacing = 3 };
                children.Children.Add(new Label { Text = $"💊 {ph.Name}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#064E3B") });
                children.Children.Add(new Label { Text = $"📍 Environ {distText}", FontSize = 12, TextColor = Color.FromArgb("#059669") });
                if (ph.Phone != null) children.Children.Add(new Label { Text = $"📞 {ph.Phone}", FontSize = 12, TextColor = Color.FromArgb("#6B7280") });
                if (ph.Hours != null) children.Children.Add(new Label { Text = $"🕐 {ph.Hours}", FontSize = 11, TextColor = Color.FromArgb("#9CA3AF") });
                listStack.Children.Add(new Frame
                {
                    BackgroundColor = Colors.White, CornerRadius = 12,
                    Padding = new Thickness(14, 12), HasShadow = false,
                    BorderColor = Color.FromArgb("#D1FAE5"), Content = children
                });
            }

            var scroll = new ScrollView { Content = listStack };
            var root = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = 1 },
                    new RowDefinition { Height = 270 },
                    new RowDefinition { Height = GridLength.Star }
                }
            };
            root.Children.Add(hdr);
            var sep = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#D1FAE5") };
            Grid.SetRow(sep,    1); root.Children.Add(sep);
            Grid.SetRow(mapWv,  2); root.Children.Add(mapWv);
            Grid.SetRow(scroll, 3); root.Children.Add(scroll);
            Content = root;
        }
        catch (FeatureNotSupportedException) { await DisplayAlert("GPS", "Géolocalisation non supportée sur cet appareil.", "OK"); await Navigation.PopAsync(); }
        catch (PermissionException)          { await DisplayAlert("GPS", "Permission de localisation refusée. Vérifiez les paramètres.", "OK"); await Navigation.PopAsync(); }
        catch (Exception ex)                 { await DisplayAlert("Erreur", ex.Message, "OK"); await Navigation.PopAsync(); }
    }

    private static string BuildMapHtml(double userLat, double userLon,
        List<(string Name, double Lat, double Lon, string? Phone, string? Hours)> pharmacies)
    {
        var ic      = System.Globalization.CultureInfo.InvariantCulture;
        var latStr  = userLat.ToString("F6", ic);
        var lonStr  = userLon.ToString("F6", ic);
        var sb = new System.Text.StringBuilder();
        foreach (var p in pharmacies.Take(20))
        {
            var safeName = p.Name.Replace("'", "&#39;");
            var popup    = string.IsNullOrEmpty(p.Phone) ? $"<b>{safeName}</b>" : $"<b>{safeName}</b><br>📞 {p.Phone}";
            sb.AppendLine($"L.marker([{p.Lat.ToString("F6",ic)},{p.Lon.ToString("F6",ic)}],{{icon:pi}}).addTo(map).bindPopup('{popup}');");
        }
        var js = sb.ToString();
        return $@"<!DOCTYPE html><html><head>
<meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>html,body,#map{{height:100%;margin:0;padding:0}}</style>
</head><body><div id='map'></div>
<script>
var map=L.map('map').setView([{latStr},{lonStr}],15);
L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png',{{attribution:'© OSM',maxZoom:19}}).addTo(map);
var ui=L.divIcon({{html:'<div style=""background:#0D9488;width:14px;height:14px;border-radius:50%;border:2px solid white""></div>',iconSize:[14,14],iconAnchor:[7,7]}});
var pi=L.divIcon({{html:'<div style=""font-size:20px"">💊</div>',iconSize:[24,24],iconAnchor:[12,12]}});
L.marker([{latStr},{lonStr}],{{icon:ui}}).addTo(map).bindPopup('<b>Vous êtes ici</b>').openPopup();
{js}
</script></body></html>";
    }
}

// ===== ADMIN PAGES =====
public class AdminDashboardPage : ContentPage
{
    private readonly ApiService _api;

    public AdminDashboardPage(ApiService api)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#EEF2FF");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStats();
    }

    private async Task LoadStats()
    {
        var loading = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#4F46E5"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Content = new VerticalStackLayout { BackgroundColor = Color.FromArgb("#EEF2FF"), VerticalOptions = LayoutOptions.Center, Children = { loading } };

        DashboardStatsDto stats;
        try { stats = await _api.GetAdminDashboardAsync(); }
        catch { stats = new DashboardStatsDto(); }

        var scroll = new ScrollView { BackgroundColor = Color.FromArgb("#EEF2FF") };
        var stack = new VerticalStackLayout { Padding = new Thickness(24, 52, 24, 40), Spacing = 22, BackgroundColor = Color.FromArgb("#EEF2FF") };

        // ── Header bar ────────────────────────────────────────────────────────
        var logoutFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#FEF2F2"), CornerRadius = 10,
            Padding = new Thickness(12, 6), HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "Déconnexion", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#DC2626") }
        };
        logoutFrame.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                Preferences.Clear();
                Application.Current!.MainPage = new NavigationPage(new LoginPage(ServiceHelper.GetService<ApiService>()));
            })
        });

        var headerCard = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 24, Padding = new Thickness(20, 18),
            HasShadow = false, BorderColor = Color.FromArgb("#E8EDF5"),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    new VerticalStackLayout
                    {
                        Spacing = 2,
                        Children =
                        {
                            new Label { Text = "Admin Panel", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") },
                            new Label { Text = "Hospital Management System", FontSize = 13, TextColor = Color.FromArgb("#64748B") }
                        }
                    }
                }
            }
        };
        var hdrGrid = (Grid)headerCard.Content;
        Grid.SetColumn(logoutFrame, 1);
        hdrGrid.Children.Add(logoutFrame);
        stack.Children.Add(headerCard);

        // ── Section: Overview ─────────────────────────────────────────────────
        stack.Children.Add(new Label { Text = "Overview", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A1A2E"), Margin = new Thickness(4, 0, 0, 0) });

        var g1 = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        var s1 = StatCard("👨‍⚕️", stats.TotalDoctors.ToString(), "Doctors", "#EFF6FF", "#2563EB");
        var s2 = StatCard("🧑", stats.TotalPatients.ToString(), "Patients", "#F0FDF4", "#16A34A");
        var s3 = StatCard("👩‍⚕️", stats.TotalNurses.ToString(), "Nurses", "#FAF5FF", "#7C3AED");
        Grid.SetColumn(s1, 0); Grid.SetColumn(s2, 1); Grid.SetColumn(s3, 2);
        g1.Children.Add(s1); g1.Children.Add(s2); g1.Children.Add(s3);
        stack.Children.Add(g1);

        var g2 = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        var s4 = StatCard("📅", stats.TodayAppointments.ToString(), "Today's Appts", "#FFF7ED", "#EA580C");
        var s5 = StatCard("⏳", stats.PendingAppointments.ToString(), "Pending", "#FFFBEB", "#D97706");
        Grid.SetColumn(s4, 0); Grid.SetColumn(s5, 1);
        g2.Children.Add(s4); g2.Children.Add(s5);
        stack.Children.Add(g2);

        var g3 = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        var s6 = StatCard("✅", stats.CompletedToday.ToString(), "Completed Today", "#F0FDF4", "#059669");
        var s7 = StatCard("📊", stats.TotalAppointments.ToString(), "Total Appts", "#F8FAFC", "#475569");
        Grid.SetColumn(s6, 0); Grid.SetColumn(s7, 1);
        g3.Children.Add(s6); g3.Children.Add(s7);
        stack.Children.Add(g3);

        // ── Section: Actions ──────────────────────────────────────────────────
        stack.Children.Add(new Label { Text = "Quick Actions", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A1A2E"), Margin = new Thickness(4, 4, 0, 0) });

        var actGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            ColumnSpacing = 12,
            RowSpacing = 12
        };

        var addDocCard = ActionCard("👨‍⚕️", "Add Doctor", "#EFF6FF", "#2563EB");
        addDocCard.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new CreateDoctorPage(_api))) });
        var addNurseCard = ActionCard("👩‍⚕️", "Add Nurse", "#FAF5FF", "#7C3AED");
        addNurseCard.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new CreateNursePage(_api))) });
        var usersCard = ActionCard("👥", "Manage Users", "#F0FDF4", "#16A34A");
        usersCard.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new ManageUsersPage(_api))) });
        var apptsCard = ActionCard("🗓️", "Appointments", "#FFF7ED", "#EA580C");
        apptsCard.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new AdminAppointmentsPage(_api))) });

        Grid.SetRow(addDocCard, 0); Grid.SetColumn(addDocCard, 0);
        Grid.SetRow(addNurseCard, 0); Grid.SetColumn(addNurseCard, 1);
        Grid.SetRow(usersCard, 1); Grid.SetColumn(usersCard, 0);
        Grid.SetRow(apptsCard, 1); Grid.SetColumn(apptsCard, 1);
        actGrid.Children.Add(addDocCard); actGrid.Children.Add(addNurseCard);
        actGrid.Children.Add(usersCard); actGrid.Children.Add(apptsCard);
        stack.Children.Add(actGrid);

        stack.Children.Add(new BoxView { HeightRequest = 80, Color = Colors.Transparent });
        scroll.Content = stack;
        Content = scroll;
    }

    private static Frame StatCard(string emoji, string value, string label, string bg, string accent)
    {
        return new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 18, Padding = new Thickness(14, 14),
            HasShadow = false, BorderColor = Color.FromArgb("#E8EDF5"),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Color.FromArgb(bg), CornerRadius = 10,
                        WidthRequest = 36, HeightRequest = 36, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                        Content = new Label { Text = emoji, FontSize = 16, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    },
                    new Label { Text = value, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(accent) },
                    new Label { Text = label, FontSize = 10, TextColor = Color.FromArgb("#8896AB") }
                }
            }
        };
    }

    private Frame ActionCard(string emoji, string title, string bg, string accent)
    {
        return new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = new Thickness(16),
            HasShadow = false,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Color.FromArgb(bg),
                        CornerRadius = 12,
                        Padding = new Thickness(10),
                        HasShadow = false,
                        BorderColor = Colors.Transparent,
                        WidthRequest = 52, HeightRequest = 52,
                        HorizontalOptions = LayoutOptions.Center,
                        Content = new Label { Text = emoji, FontSize = 26, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    },
                    new Label { Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }
                }
            }
        };
    }
}

public class CreateDoctorPage : ContentPage
{
    private readonly ApiService _api;
    private Entry _fnEntry = new(), _lnEntry = new(), _emailEntry = new(),
                  _passEntry = new(), _licEntry = new(), _phoneEntry = new(),
                  _feeEntry = new(), _expEntry = new();

    public CreateDoctorPage(ApiService api)
    {
        _api = api;
        Title = "Add Doctor";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildUI();
    }

    private void BuildUI()
    {
        _passEntry.IsPassword = true;
        var fields = new (string Label, Entry Field)[]
        {
            ("First Name", _fnEntry), ("Last Name", _lnEntry),
            ("Email", _emailEntry), ("Password", _passEntry),
            ("License Number", _licEntry), ("Phone", _phoneEntry),
            ("Fee ($)", _feeEntry), ("Experience (years)", _expEntry)
        };

        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        stack.Children.Add(new Label { Text = "👨‍⚕️ New Doctor Account", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") });

        foreach (var (lbl, field) in fields)
        {
            field.Placeholder = lbl;
            field.BackgroundColor = Colors.White;
            field.TextColor = Color.FromArgb("#1E293B");
            field.PlaceholderColor = Color.FromArgb("#94A3B8");
            field.FontSize = 14;
            field.HeightRequest = 48;
            stack.Children.Add(new Frame
            {
                BorderColor = Color.FromArgb("#E2E8F0"),
                CornerRadius = 12,
                Padding = new Thickness(4, 0),
                HasShadow = false,
                Content = field
            });
        }

        var createBtn = new Button
        {
            Text = "Create Doctor Account",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 52
        };
        createBtn.Clicked += CreateDoctor_Clicked;
        stack.Children.Add(createBtn);

        Content = new ScrollView { Content = stack };
    }

    private async void CreateDoctor_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_fnEntry.Text) || string.IsNullOrEmpty(_emailEntry.Text))
        {
            await DisplayAlert("Error", "Please fill required fields", "OK");
            return;
        }
        var result = await _api.CreateDoctorAsync(new CreateDoctorRequest
        {
            FirstName = _fnEntry.Text,
            LastName = _lnEntry.Text,
            Email = _emailEntry.Text,
            Password = _passEntry.Text,
            LicenseNumber = _licEntry.Text,
            PhoneNumber = _phoneEntry.Text,
            SpecialtyId = Guid.Parse("11111111-0000-0000-0000-000000000006"),
            ConsultationFee = decimal.TryParse(_feeEntry.Text, out var f) ? f : 0,
            YearsOfExperience = int.TryParse(_expEntry.Text, out var x) ? x : 0
        });
        if (result.Success) { await DisplayAlert("✅", "Doctor created!", "OK"); await Navigation.PopAsync(); }
        else await DisplayAlert("Error", result.Message ?? "Failed", "OK");
    }
}

public class ManageUsersPage : ContentPage
{
    private readonly ApiService _api;
    public ManageUsersPage(ApiService api) { _api = api; Title = "Manage Users"; BackgroundColor = Color.FromArgb("#F8FAFC"); }

    protected override async void OnAppearing() { base.OnAppearing(); await LoadUsers(); }

    private async Task LoadUsers()
    {
        var loading = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#2563EB"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { loading } };
        List<UserListDto> users;
        try { users = await _api.GetUsersAsync(); } catch { users = new(); }

        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 10 };
        foreach (var user in users)
        {
            var toggleBtn = new Button
            {
                Text = user.IsActive ? "Disable" : "Enable",
                BackgroundColor = user.IsActive ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#D1FAE5"),
                TextColor = user.IsActive ? Color.FromArgb("#DC2626") : Color.FromArgb("#059669"),
                CornerRadius = 8,
                HeightRequest = 36,
                FontSize = 12,
                WidthRequest = 80
            };
            var uid = user.Id;
            toggleBtn.Clicked += async (s, e) => { await _api.ToggleUserAsync(uid); await LoadUsers(); };

            var infoStack = new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    new Label { Text = $"{user.RoleIcon} {user.Email}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                    new Label { Text = user.Role, FontSize = 12, TextColor = Color.FromArgb("#2563EB") },
                    new Label { Text = user.StatusDisplay, FontSize = 11, TextColor = user.StatusColor }
                }
            };

            var cardGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            cardGrid.Children.Add(infoStack);
            Grid.SetColumn(toggleBtn, 1);
            cardGrid.Children.Add(toggleBtn);

            stack.Children.Add(new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 14,
                Padding = new Thickness(16),
                HasShadow = true,
                BorderColor = Color.FromArgb("#E2E8F0"),
                Content = cardGrid
            });
        }
        Content = new ScrollView { Content = stack };
    }
}

public class ChatContactsPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService? _signalR;

    public ChatContactsPage(ApiService api, SignalRService? signalR)
    {
        _api = api;
        _signalR = signalR;
        Title = "Messages";
        BackgroundColor = Color.FromArgb("#F8FAFC");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadContacts();
    }

    private async Task LoadContacts()
    {
        var loading = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#2563EB"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { loading } };

        List<ChatContactDto> contacts;
        try { contacts = await _api.GetChatContactsAsync(); }
        catch { contacts = new(); }

        var stack = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#F8FAFC") };

        // Header
        stack.Children.Add(new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E2E8F0"),
            CornerRadius = 0,
            HasShadow = false,
            Padding = new Thickness(20, 16),
            Content = new Label { Text = "Messages", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") }
        });

        if (!contacts.Any())
        {
            var goToApptsBtn = new Frame
            {
                BackgroundColor = Color.FromArgb("#4F46E5"),
                CornerRadius = 14,
                Padding = new Thickness(24, 14),
                HasShadow = false,
                BorderColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = "🗓️  Go to My Appointments",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center
                }
            };
            goToApptsBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                    await Navigation.PushAsync(new PatientAppointmentsPage(_api, _signalR)))
            });

            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 16,
                Padding = new Thickness(32),
                Children =
                {
                    new Label { Text = "💬", FontSize = 64, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "No conversations yet", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B"), HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "To start chatting, go to your appointments and tap \"💬 Chat with Doctor\"", FontSize = 14, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center },
                    goToApptsBtn
                }
            };
            return;
        }

        var listStack = new VerticalStackLayout { Spacing = 1, BackgroundColor = Color.FromArgb("#E2E8F0") };
        foreach (var contact in contacts)
        {
            var initial = (contact.Email.Length > 0 ? contact.Email[0].ToString().ToUpper() : "?");
            var avatarFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#DBEAFE"),
                CornerRadius = 24,
                WidthRequest = 48, HeightRequest = 48,
                Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                Content = new Label { Text = initial, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1D4ED8"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };

            var nameLabel = new Label { Text = contact.DisplayName ?? contact.Email, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") };
            var lastMsgLabel = new Label { Text = contact.LastMessage, FontSize = 13, TextColor = Color.FromArgb("#64748B"), LineBreakMode = LineBreakMode.TailTruncation };
            var timeLabel = new Label { Text = contact.TimeDisplay, FontSize = 11, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.End };

            var rightStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 0 };
            rightStack.Children.Add(timeLabel);
            if (contact.HasUnread)
                rightStack.Children.Add(new Frame
                {
                    BackgroundColor = Color.FromArgb("#2563EB"),
                    CornerRadius = 10,
                    Padding = new Thickness(6, 2),
                    HasShadow = false, BorderColor = Colors.Transparent,
                    HorizontalOptions = LayoutOptions.End,
                    Content = new Label { Text = contact.UnreadCount.ToString(), FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }
                });

            var rowGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12,
                Padding = new Thickness(16, 14)
            };
            rowGrid.Children.Add(avatarFrame);
            var infoStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 3 };
            infoStack.Children.Add(nameLabel);
            infoStack.Children.Add(lastMsgLabel);
            Grid.SetColumn(infoStack, 1);
            Grid.SetColumn(rightStack, 2);
            rowGrid.Children.Add(infoStack);
            rowGrid.Children.Add(rightStack);

            var contactCard = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 0,
                Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                Content = rowGrid
            };
            var capturedContact = contact;
            contactCard.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                    await Navigation.PushAsync(new ChatPage(_api, _signalR, capturedContact.UserId, capturedContact.DisplayName ?? capturedContact.Email)))
            });
            listStack.Children.Add(contactCard);
        }

        stack.Children.Add(new ScrollView { Content = listStack });
        Content = stack;
    }
}

public class ChatPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;
    private readonly Guid _otherUserId;
    private readonly string _otherName;
    private VerticalStackLayout _messagesStack = new();
    private Entry _messageEntry = new();
    private ScrollView _scrollView = new();

    public ChatPage(ApiService api, SignalRService signalR, Guid otherUserId, string otherName)
    {
        _api = api;
        _signalR = signalR;
        _otherUserId = otherUserId;
        _otherName = otherName;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#ECE5DD");
        BuildUI();
    }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#EEF2FF");
        var initial = _otherName.Length > 0 ? _otherName[0].ToString().ToUpper() : "?";

        // ── Header ───────────────────────────────────────────────────
        var backBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#6366F1"), CornerRadius = 12,
            WidthRequest = 38, HeightRequest = 38, Padding = 0,
            HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "←", FontSize = 20, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        var avatarFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#7C3AED"), CornerRadius = 22,
            WidthRequest = 44, HeightRequest = 44, Padding = 0, HasShadow = false, BorderColor = Color.FromArgb("#6366F1"),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = initial, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        var nameStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, Spacing = 2,
            Children =
            {
                new Label { Text = _otherName, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new HorizontalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        new BoxView { WidthRequest = 7, HeightRequest = 7, CornerRadius = 4, BackgroundColor = Color.FromArgb("#4ADE80"), VerticalOptions = LayoutOptions.Center },
                        new Label { Text = "Online", FontSize = 12, TextColor = Color.FromArgb("#C7D2FE") }
                    }
                }
            }
        };

        Frame MakeHeaderBtn(string symbol, System.Action action)
        {
            var f = new Frame
            {
                BackgroundColor = Color.FromArgb("#6366F1"), CornerRadius = 12,
                WidthRequest = 38, HeightRequest = 38, Padding = 0,
                HasShadow = false, BorderColor = Colors.Transparent,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label { Text = symbol, FontSize = 16, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            f.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(action) });
            return f;
        }

        // Use text symbols instead of emoji to avoid OS color rendering
        var voiceBtn = MakeHeaderBtn("✆", async () => await Navigation.PushAsync(new VideoCallPage(_signalR, _otherUserId, _otherName, false)));
        var videoBtn = MakeHeaderBtn("▶", async () => await Navigation.PushAsync(new VideoCallPage(_signalR, _otherUserId, _otherName, true)));

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10,
            Padding = new Thickness(16, 48, 16, 14),
            BackgroundColor = Color.FromArgb("#4F46E5")
        };
        headerGrid.Children.Add(backBtn);
        Grid.SetColumn(avatarFrame, 1); headerGrid.Children.Add(avatarFrame);
        Grid.SetColumn(nameStack, 2); headerGrid.Children.Add(nameStack);
        Grid.SetColumn(voiceBtn, 3); headerGrid.Children.Add(voiceBtn);
        Grid.SetColumn(videoBtn, 4); headerGrid.Children.Add(videoBtn);

        // ── Messages area ────────────────────────────────────────────
        _messagesStack = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(16, 16) };
        _scrollView = new ScrollView { Content = _messagesStack, BackgroundColor = Color.FromArgb("#EEF2FF") };

        // ── Input bar ────────────────────────────────────────────────
        _messageEntry = new Entry
        {
            Placeholder = "Type a message...",
            PlaceholderColor = Color.FromArgb("#8896AB"),
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#1A1A2E"),
            FontSize = 15,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center
        };
        _messageEntry.Completed += async (s, e) => await SendTextAsync();

        var attachBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#EEF2FF"), CornerRadius = 12,
            WidthRequest = 44, HeightRequest = 44, Padding = 0,
            HasShadow = false, BorderColor = Color.FromArgb("#E8EDF5"),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "+", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4F46E5"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        attachBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await ShowAttachmentMenuAsync()) });

        var msgInputFrame = new Frame
        {
            BackgroundColor = Colors.White, CornerRadius = 24, Padding = new Thickness(16, 0),
            HasShadow = false, BorderColor = Color.FromArgb("#E8EDF5"), HeightRequest = 50,
            HorizontalOptions = LayoutOptions.Fill,
            Content = _messageEntry
        };

        var sendBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#4F46E5"), CornerRadius = 24,
            WidthRequest = 50, HeightRequest = 50, Padding = 0,
            HasShadow = false, BorderColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label { Text = "➤", FontSize = 20, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        sendBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await SendTextAsync()) });

        var inputRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10,
            Padding = new Thickness(16, 10, 16, 16),
            BackgroundColor = Colors.White
        };
        inputRow.Children.Add(attachBtn);
        Grid.SetColumn(msgInputFrame, 1); inputRow.Children.Add(msgInputFrame);
        Grid.SetColumn(sendBtn, 2); inputRow.Children.Add(sendBtn);

        // ── Layout ───────────────────────────────────────────────────
        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };
        mainGrid.Children.Add(headerGrid);
        Grid.SetRow(_scrollView, 1); mainGrid.Children.Add(_scrollView);
        Grid.SetRow(inputRow, 2); mainGrid.Children.Add(inputRow);
        Content = mainGrid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _signalR.MessageReceived += OnMessageReceived;
        _signalR.MessageSent += OnMessageSentBack;
        await LoadHistory();
        await _signalR.MarkMessagesReadAsync(_otherUserId.ToString());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _signalR.MessageReceived -= OnMessageReceived;
        _signalR.MessageSent -= OnMessageSentBack;
    }

    private async Task LoadHistory()
    {
        _messagesStack.Children.Clear();
        try
        {
            var messages = await _api.GetChatMessagesAsync(_otherUserId);
            foreach (var msg in messages)
                _messagesStack.Children.Add(BuildBubble(msg.Message, msg.IsMine, msg.SentAt));
        }
        catch { }
        await ScrollToBottomAsync();
    }

    private void OnMessageReceived(IncomingChatMessageDto msg)
    {
        if (!string.Equals(msg.SenderId, _otherUserId.ToString(), StringComparison.OrdinalIgnoreCase)) return;
        AddBubbleAndScroll(BuildBubble(msg.Message, false, msg.SentAt));
    }

    private void OnMessageSentBack(IncomingChatMessageDto msg)
    {
        // Message is already shown optimistically — no duplicate needed
    }

    private void AddBubbleAndScroll(View bubble)
    {
        _messagesStack.Children.Add(bubble);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(80); // wait for layout pass
            await ScrollToBottomAsync();
        });
    }

    private async Task SendTextAsync()
    {
        var text = _messageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _messageEntry.Text = "";

        // Show immediately (optimistic — like WhatsApp)
        AddBubbleAndScroll(BuildBubble(text, true, DateTime.Now));

        try { await _signalR.SendMessageAsync(_otherUserId.ToString(), text); }
        catch (Exception ex) { await DisplayAlert("Send Error", ex.Message, "OK"); }
    }

    private async Task ShowAttachmentMenuAsync()
    {
        var choice = await DisplayActionSheet("Send Attachment", "Cancel", null,
            "📷 Photo from Gallery", "📸 Take Photo", "📄 Send File");
        if (choice == "📷 Photo from Gallery") await SendImageAsync(false);
        else if (choice == "📸 Take Photo") await SendImageAsync(true);
        else if (choice == "📄 Send File") await SendDocumentAsync();
    }

    private async Task SendImageAsync(bool takeNew)
    {
        try
        {
            FileResult? photo = takeNew
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;
            using var stream = await photo.OpenReadAsync();
            using var ms = new System.IO.MemoryStream();
            await stream.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var ext = System.IO.Path.GetExtension(photo.FileName).ToLowerInvariant();
            var mime = ext == ".png" ? "image/png" : "image/jpeg";
            var payload = $"[IMG:{mime}]{base64}";
            AddBubbleAndScroll(BuildBubble(payload, true, DateTime.Now));
            await _signalR.SendMessageAsync(_otherUserId.ToString(), payload);
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private async Task SendDocumentAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync();
            if (file == null) return;
            using var stream = await file.OpenReadAsync();
            using var ms = new System.IO.MemoryStream();
            await stream.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var payload = $"[FILE:{file.FileName}]{base64}";
            AddBubbleAndScroll(BuildBubble(payload, true, DateTime.Now));
            await _signalR.SendMessageAsync(_otherUserId.ToString(), payload);
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    // ── Bubble builders ──────────────────────────────────────────────
    private View BuildBubble(string message, bool isMine, DateTime sentAt)
    {
        if (message.StartsWith("[IMG:")) return BuildImageBubble(message, isMine, sentAt);
        if (message.StartsWith("[FILE:")) return BuildFileBubble(message, isMine, sentAt);
        return BuildTextBubble(message, isMine, sentAt);
    }

    private View BuildTextBubble(string message, bool isMine, DateTime sentAt)
    {
        var bubbleBg    = isMine ? Color.FromArgb("#4F46E5") : Colors.White;
        var textColor   = isMine ? Colors.White : Color.FromArgb("#1A1A2E");
        var timeColor   = isMine ? Color.FromArgb("#C7D2FE") : Color.FromArgb("#8896AB");
        var borderColor = isMine ? Colors.Transparent : Color.FromArgb("#E8EDF5");

        return new Frame
        {
            BackgroundColor = bubbleBg,
            CornerRadius = 18, Padding = new Thickness(14, 10),
            HasShadow = false, BorderColor = borderColor,
            MaximumWidthRequest = 280,
            HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start,
            Margin = new Thickness(isMine ? 56 : 0, 2, isMine ? 0 : 56, 2),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = message, FontSize = 14, TextColor = textColor, LineBreakMode = LineBreakMode.WordWrap },
                    new HorizontalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.End, Spacing = 4,
                        Children =
                        {
                            new Label { Text = sentAt.ToLocalTime().ToString("HH:mm"), FontSize = 10, TextColor = timeColor },
                            isMine ? new Label { Text = "✓✓", FontSize = 10, TextColor = Color.FromArgb("#C7D2FE") } : (View)new BoxView { WidthRequest = 0 }
                        }
                    }
                }
            }
        };
    }

    private View BuildImageBubble(string message, bool isMine, DateTime sentAt)
    {
        try
        {
            var end = message.IndexOf(']');
            var base64 = message[(end + 1)..];
            var imgSource = ImageSource.FromStream(() => new System.IO.MemoryStream(Convert.FromBase64String(base64)));
            return new Frame
            {
                BackgroundColor = isMine ? Color.FromArgb("#4F46E5") : Colors.White,
                CornerRadius = 18, Padding = new Thickness(4),
                HasShadow = false, BorderColor = isMine ? Colors.Transparent : Color.FromArgb("#E8EDF5"),
                MaximumWidthRequest = 240,
                HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start,
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Image { Source = imgSource, WidthRequest = 230, HeightRequest = 190, Aspect = Aspect.AspectFill },
                        new Label { Text = sentAt.ToLocalTime().ToString("HH:mm"), FontSize = 10, TextColor = Color.FromArgb("#667781"), HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 4, 2) }
                    }
                }
            };
        }
        catch { return BuildTextBubble("📷 Image", isMine, sentAt); }
    }

    private View BuildFileBubble(string message, bool isMine, DateTime sentAt)
    {
        var end = message.IndexOf(']');
        var fileName = end > 6 ? message[6..end] : "File";
        return new Frame
        {
            BackgroundColor = isMine ? Color.FromArgb("#4F46E5") : Colors.White,
            CornerRadius = 18, Padding = new Thickness(14, 12),
            HasShadow = false, BorderColor = isMine ? Colors.Transparent : Color.FromArgb("#E8EDF5"),
            MaximumWidthRequest = 260,
            HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start,
            Content = new HorizontalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Color.FromArgb("#EEF2FF"), CornerRadius = 8,
                        WidthRequest = 40, HeightRequest = 40, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                        Content = new Label { Text = "📄", FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    },
                    new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center, Spacing = 2,
                        Children =
                        {
                            new Label { Text = fileName, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111B21"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 },
                            new Label { Text = sentAt.ToLocalTime().ToString("HH:mm"), FontSize = 10, TextColor = Color.FromArgb("#667781") }
                        }
                    }
                }
            }
        };
    }

    private async Task ScrollToBottomAsync()
    {
        if (_messagesStack.Children.LastOrDefault() is View last)
            await _scrollView.ScrollToAsync(last, ScrollToPosition.End, false);
    }
}

// ===== VIDEO / VOICE CALL PAGE =====
public class VideoCallPage : ContentPage
{
    private readonly SignalRService _signalR;
    private readonly Guid _otherUserId;
    private readonly string _otherName;
    private readonly bool _isVideo;
    private Label _statusLabel = new();
    private bool _isMuted = false;
    private bool _isSpeakerOn = true;
    private Frame _muteBtn = new();
    private Frame _speakerBtn = new();

    public VideoCallPage(SignalRService signalR, Guid otherUserId, string otherName, bool isVideo, bool isIncoming = false, string? sessionId = null)
    {
        _signalR = signalR;
        _otherUserId = otherUserId;
        _otherName = otherName;
        _isVideo = isVideo;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#0D1117");

        if (isVideo)
            BuildJitsiUI();
        else
        {
            BuildVoiceUI(isIncoming, sessionId);
            if (!isIncoming) _ = InitiateCallAsync();
        }
    }

    // ── Real video via Jitsi Meet ──────────────────────────────
    private void BuildJitsiUI()
    {
        var myId = Preferences.Get("UserId", "");
        var otherId = _otherUserId.ToString();
        // Deterministic room: sort both IDs so both parties get the same room name
        var ids = new[] { myId, otherId };
        Array.Sort(ids, StringComparer.OrdinalIgnoreCase);
        var roomId = $"MediCarePlus-{ids[0][..8]}-{ids[1][..8]}";
        var myName = Uri.EscapeDataString(Preferences.Get("FullName", "Utilisateur"));
        var url = $"https://meet.jit.si/{roomId}" +
                  $"#userInfo.displayName={myName}" +
                  $"&config.startWithVideoMuted=false" +
                  $"&config.startWithAudioMuted=false" +
                  $"&config.prejoinPageEnabled=false" +
                  $"&config.disableDeepLinking=true";

        var webView = new WebView
        {
            Source = new UrlWebViewSource { Url = url },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var endBar = new Grid
        {
            BackgroundColor = Color.FromArgb("#0D1117"),
            Padding = new Thickness(0, 12),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star), new(GridLength.Auto), new(GridLength.Star)
            }
        };
        var endBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#EF4444"), CornerRadius = 30,
            WidthRequest = 60, HeightRequest = 60, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label { Text = "📵", FontSize = 26, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        endBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        Grid.SetColumn(endBtn, 1);
        endBar.Children.Add(endBtn);

        var layout = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new(GridLength.Star),
                new(GridLength.Auto)
            }
        };
        layout.Children.Add(webView);
        Grid.SetRow(endBar, 1);
        layout.Children.Add(endBar);

        Content = layout;
        _ = InitiateCallAsync();
    }

    // ── Voice call UI ──────────────────────────────────────────
    private void BuildVoiceUI(bool isIncoming, string? sessionId)
    {
        var initial = _otherName.Length > 0 ? _otherName[0].ToString().ToUpper() : "?";

        _statusLabel = new Label
        {
            Text = isIncoming ? "Appel vocal entrant..." : "Appel en cours...",
            FontSize = 15, TextColor = Color.FromArgb("#94A3B8"),
            HorizontalOptions = LayoutOptions.Center
        };

        var avatar = new Frame
        {
            BackgroundColor = Color.FromArgb("#1D4ED8"), CornerRadius = 56,
            WidthRequest = 112, HeightRequest = 112, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label { Text = initial, FontSize = 48, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        _muteBtn = CallControlBtn("🎤", "#1E293B", () =>
        {
            _isMuted = !_isMuted;
            ((Label)_muteBtn.Content).Text = _isMuted ? "🔇" : "🎤";
            _muteBtn.BackgroundColor = _isMuted ? Color.FromArgb("#EF4444") : Color.FromArgb("#1E293B");
        });
        _speakerBtn = CallControlBtn("🔊", "#1E293B", () =>
        {
            _isSpeakerOn = !_isSpeakerOn;
            ((Label)_speakerBtn.Content).Text = _isSpeakerOn ? "🔊" : "🔈";
        });

        var endBtn = new Frame
        {
            BackgroundColor = Color.FromArgb("#EF4444"), CornerRadius = 34,
            WidthRequest = 68, HeightRequest = 68, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label { Text = "📵", FontSize = 28, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        endBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });

        View bottomControls;
        if (isIncoming)
        {
            var acceptBtn = new Frame
            {
                BackgroundColor = Color.FromArgb("#22C55E"), CornerRadius = 34,
                WidthRequest = 68, HeightRequest = 68, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label { Text = "📞", FontSize = 28, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            acceptBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => _statusLabel.Text = "Appel en cours...")
            });
            bottomControls = new HorizontalStackLayout { Spacing = 60, HorizontalOptions = LayoutOptions.Center, Children = { endBtn, acceptBtn } };
        }
        else
        {
            bottomControls = new HorizontalStackLayout { Spacing = 24, HorizontalOptions = LayoutOptions.Center, Children = { _muteBtn, endBtn, _speakerBtn } };
        }

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new(new GridLength(1, GridUnitType.Star)),
                new(GridLength.Auto),
                new(new GridLength(1, GridUnitType.Star))
            }
        };
        grid.Children.Add(new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 16,
            Children =
            {
                avatar,
                new Label { Text = _otherName, FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "📞 Appel vocal", FontSize = 14, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center },
                _statusLabel
            }
        });
        Grid.SetRow(bottomControls, 2);
        grid.Children.Add(bottomControls);
        Content = grid;
    }

    private static Frame CallControlBtn(string emoji, string bgHex, Action onTap)
    {
        var btn = new Frame
        {
            BackgroundColor = Color.FromArgb(bgHex), CornerRadius = 30,
            WidthRequest = 60, HeightRequest = 60, Padding = 0, HasShadow = false, BorderColor = Colors.Transparent,
            Content = new Label { Text = emoji, FontSize = 24, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        btn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(onTap) });
        return btn;
    }

    private async Task InitiateCallAsync()
    {
        try { await _signalR.InitiateCallAsync(_otherUserId.ToString(), _isVideo); }
        catch { }
    }
}

public class NotificationsPage : ContentPage
{
    public NotificationsPage()
    {
        Title = "Notifications";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 12,
            Children =
            {
                new Label { Text = "🔔", FontSize = 60, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "Notifications", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B"), HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "No notifications yet", FontSize = 14, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center }
            }
        };
    }
}

public class AdminAppointmentsPage : ContentPage
{
    private readonly ApiService _api;

    public AdminAppointmentsPage(ApiService api)
    {
        _api = api;
        Title = "All Appointments";
        BackgroundColor = Color.FromArgb("#F1F5F9");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAppointments();
    }

    private async Task LoadAppointments()
    {
        var loading = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#2563EB"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { loading } };

        List<AppointmentDto> appointments;
        try { appointments = await _api.GetAllAppointmentsAsync(); }
        catch { appointments = new(); }

        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 10 };
        stack.Children.Add(new Label
        {
            Text = $"{appointments.Count} total appointments",
            FontSize = 13,
            TextColor = Color.FromArgb("#64748B"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (!appointments.Any())
        {
            stack.Children.Add(new Label
            {
                Text = "No appointments found",
                FontSize = 16,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40)
            });
        }

        foreach (var appt in appointments)
        {
            var statusBg = appt.Status switch
            {
                "Confirmed" => "#D1FAE5", "Completed" => "#EDE9FE",
                "Cancelled" => "#FEE2E2", _ => "#DBEAFE"
            };
            var statusFg = appt.Status switch
            {
                "Confirmed" => "#065F46", "Completed" => "#5B21B6",
                "Cancelled" => "#991B1B", _ => "#1D4ED8"
            };

            var statusBadge = new Frame
            {
                BackgroundColor = Color.FromArgb(statusBg),
                CornerRadius = 8, Padding = new Thickness(8, 3),
                HasShadow = false, BorderColor = Colors.Transparent,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label { Text = appt.Status, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(statusFg) }
            };

            var infoStack = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = appt.DoctorName ?? "Doctor", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") },
                    new Label { Text = $"Patient: {appt.PatientName ?? "Unknown"}", FontSize = 13, TextColor = Color.FromArgb("#374151") },
                    new Label { Text = $"{appt.DateDisplay}  ·  {appt.TimeDisplay}", FontSize = 12, TextColor = Color.FromArgb("#64748B") },
                    new Label { Text = appt.ConsultationType == "Video" ? "📹 Video" : "🏥 In Person", FontSize = 12, TextColor = Color.FromArgb("#2563EB") }
                }
            };

            var cardGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            cardGrid.Children.Add(infoStack);
            Grid.SetColumn(statusBadge, 1);
            cardGrid.Children.Add(statusBadge);

            stack.Children.Add(new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 14,
                Padding = new Thickness(16),
                HasShadow = false,
                BorderColor = Color.FromArgb("#E2E8F0"),
                Content = cardGrid
            });
        }

        Content = new ScrollView { Content = stack, BackgroundColor = Color.FromArgb("#F1F5F9") };
    }
}

public class CreateNursePage : ContentPage
{
    private readonly ApiService _api;
    private Entry _fnEntry = new(), _lnEntry = new(), _emailEntry = new(),
                  _passEntry = new(), _licEntry = new(), _phoneEntry = new(),
                  _deptEntry = new();

    public CreateNursePage(ApiService api)
    {
        _api = api;
        Title = "Add Nurse";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildUI();
    }

    private void BuildUI()
    {
        _passEntry.IsPassword = true;
        var fields = new (string Label, Entry Field)[]
        {
            ("First Name", _fnEntry), ("Last Name", _lnEntry),
            ("Email", _emailEntry), ("Password", _passEntry),
            ("License Number", _licEntry), ("Phone", _phoneEntry),
            ("Department", _deptEntry)
        };

        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        stack.Children.Add(new Label { Text = "👩‍⚕️ New Nurse Account", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") });

        foreach (var (lbl, field) in fields)
        {
            field.Placeholder = lbl;
            field.BackgroundColor = Colors.White;
            field.TextColor = Color.FromArgb("#1E293B");
            field.PlaceholderColor = Color.FromArgb("#94A3B8");
            field.FontSize = 14;
            field.HeightRequest = 48;
            stack.Children.Add(new Frame
            {
                BorderColor = Color.FromArgb("#E2E8F0"),
                CornerRadius = 12,
                Padding = new Thickness(4, 0),
                HasShadow = false,
                Content = field
            });
        }

        var createBtn = new Button
        {
            Text = "Create Nurse Account",
            BackgroundColor = Color.FromArgb("#7C3AED"),
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 52
        };
        createBtn.Clicked += CreateNurse_Clicked;
        stack.Children.Add(createBtn);

        Content = new ScrollView { Content = stack };
    }

    private async void CreateNurse_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_fnEntry.Text) || string.IsNullOrEmpty(_emailEntry.Text))
        {
            await DisplayAlert("Error", "Please fill required fields", "OK");
            return;
        }
        var result = await _api.CreateNurseAsync(new CreateNurseRequest
        {
            FirstName = _fnEntry.Text,
            LastName = _lnEntry.Text,
            Email = _emailEntry.Text,
            Password = _passEntry.Text,
            LicenseNumber = _licEntry.Text,
            PhoneNumber = _phoneEntry.Text,
            Department = _deptEntry.Text
        });
        if (result.Success) { await DisplayAlert("✅", "Nurse created!", "OK"); await Navigation.PopAsync(); }
        else await DisplayAlert("Error", result.Message ?? "Failed", "OK");
    }
}