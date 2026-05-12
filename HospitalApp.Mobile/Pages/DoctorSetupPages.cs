// ============================================================
// DoctorSetupPages.cs — Two-step doctor onboarding flow
// ============================================================
using HospitalApp.Mobile.Models;
using HospitalApp.Mobile.Services;

namespace HospitalApp.Mobile.Pages;

// ════════════════════════════════════════════════════════════
// STEP 1 — Profile info + photos
// ════════════════════════════════════════════════════════════
public class DoctorProfileSetupPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Guid _doctorId;

    // Form fields
    private readonly Entry _phoneEntry   = MakeEntry("📞  Téléphone", Keyboard.Telephone);
    private readonly Entry _addressEntry = MakeEntry("📍  Saisissez votre adresse…");
    private Picker _cityPicker = new();
    private Label  _mapBtnLbl  = new();
    private Frame  _mapFrame   = new();
    private WebView _mapWebView = new();
    private double _lat, _lng;
    private static readonly HttpClient _http = new() { DefaultRequestHeaders = { { "User-Agent", "MediCarePlus/1.0" } } };
    private readonly Entry _hospitalEntry  = MakeEntry("🏥  Nom de l'établissement");
    private readonly Entry _sectionEntry   = MakeEntry("🏷️  Service / Section");
    private readonly Entry _feeEntry       = MakeEntry("💰  Tarif de consultation (MAD)", Keyboard.Numeric);
    private readonly Entry _expEntry       = MakeEntry("🎓  Années d'expérience", Keyboard.Numeric);
    private readonly Editor _bioEditor     = new Editor
    {
        Placeholder = "✍️  Biographie professionnelle…",
        HeightRequest = 90,
        BackgroundColor = Colors.White,
        FontSize = 14,
        TextColor = Color.FromArgb("#1A1A2E")
    };

    // Profile image
    private string? _profileImagePath;
    private string? _profileImageName;
    private readonly Frame _profileImageFrame;
    private readonly Label _profileImageLabel;

    // Cabinet images
    private readonly List<string> _cabinetPaths = new();
    private readonly List<string> _cabinetNames = new();
    private readonly HorizontalStackLayout _cabinetRow;

    public DoctorProfileSetupPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F0F4FF");

        // ── Profile image picker ─────────────────────────────────────
        _profileImageLabel = new Label
        {
            Text = "👤",
            FontSize = 40,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        _profileImageFrame = new Frame
        {
            WidthRequest = 100, HeightRequest = 100, CornerRadius = 50,
            BackgroundColor = Color.FromArgb("#E0E7FF"),
            BorderColor = Color.FromArgb("#6366F1"),
            Padding = 0, HasShadow = false,
            Content = _profileImageLabel,
            HorizontalOptions = LayoutOptions.Center
        };
        _profileImageFrame.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await PickProfileImageAsync())
        });

        // ── Address / city widgets ────────────────────────────────────
        InitAddressMap();

        // ── Cabinet images row ──────────────────────────────────────
        _cabinetRow = new HorizontalStackLayout { Spacing = 10 };
        RebuildCabinetRow();

        // ── Scroll content ──────────────────────────────────────────
        var scroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 52, 24, 40),
                Spacing = 20,
                Children =
                {
                    // Header
                    new Label
                    {
                        Text = "Configuration du profil",
                        FontSize = 24, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#1A1A2E"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Étape 1 sur 2",
                        FontSize = 14, TextColor = Color.FromArgb("#6366F1"),
                        HorizontalOptions = LayoutOptions.Center
                    },

                    // Progress bar
                    MakeProgressBar(0.5),

                    // Profile photo
                    Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            SectionTitle("Photo de profil"),
                            _profileImageFrame,
                            new Label
                            {
                                Text = "Appuyez pour choisir une photo",
                                FontSize = 12,
                                TextColor = Color.FromArgb("#6366F1"),
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    }),

                    // Cabinet images
                    Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            SectionTitle("Photos du cabinet (optionnel)"),
                            new ScrollView
                            {
                                Orientation = ScrollOrientation.Horizontal,
                                Content = _cabinetRow,
                                HeightRequest = 90
                            }
                        }
                    }),

                    // Professional info
                    Card(new VerticalStackLayout
                    {
                        Spacing = 14,
                        Children =
                        {
                            SectionTitle("Informations professionnelles"),
                            _phoneEntry,
                            // ── Adresse + carte ──────────────────────────
                            new Label { Text = "📍  Adresse du cabinet", FontSize = 13, TextColor = Color.FromArgb("#64748B") },
                            _addressEntry,
                            _mapBtnLbl,
                            _mapFrame,
                            // ── Ville ────────────────────────────────────
                            new Label { Text = "🏙️  Ville", FontSize = 13, TextColor = Color.FromArgb("#64748B") },
                            new Frame
                            {
                                CornerRadius = 10, BorderColor = Color.FromArgb("#E2E8F0"),
                                Padding = new Thickness(8, 4), HasShadow = false, BackgroundColor = Colors.White,
                                Content = _cityPicker
                            },
                            _hospitalEntry,
                            _sectionEntry,
                            RowOf(_feeEntry, _expEntry),
                            new Label { Text = "Biographie", FontSize = 13, TextColor = Color.FromArgb("#64748B") },
                            new Frame
                            {
                                CornerRadius = 10, BorderColor = Color.FromArgb("#E2E8F0"),
                                Padding = 8, HasShadow = false, BackgroundColor = Colors.White,
                                Content = _bioEditor
                            }
                        }
                    }),

                    // Next button
                    MakeButton("Suivant →", "#6366F1", async () => await GoNextAsync())
                }
            }
        };

        Content = scroll;
    }

    private void RebuildCabinetRow()
    {
        _cabinetRow.Children.Clear();

        foreach (var (path, name) in _cabinetPaths.Zip(_cabinetNames))
        {
            var thumb = new Frame
            {
                WidthRequest = 80, HeightRequest = 80,
                CornerRadius = 10, Padding = 0, HasShadow = false,
                BackgroundColor = Color.FromArgb("#C7D2FE"),
                BorderColor = Color.FromArgb("#6366F1"),
                Content = new Label
                {
                    Text = "🖼️", FontSize = 28,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            _cabinetRow.Children.Add(thumb);
        }

        // Add button
        var addBtn = new Frame
        {
            WidthRequest = 80, HeightRequest = 80, CornerRadius = 10,
            BackgroundColor = Color.FromArgb("#EEF2FF"),
            BorderColor = Color.FromArgb("#C7D2FE"),
            Padding = 0, HasShadow = false,
            Content = new Label
            {
                Text = "+", FontSize = 32,
                TextColor = Color.FromArgb("#6366F1"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        addBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await PickCabinetImageAsync())
        });
        if (_cabinetPaths.Count < 5) _cabinetRow.Children.Add(addBtn);
    }

    private async Task PickProfileImageAsync()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync();
            if (result == null) return;
            _profileImagePath = result.FullPath;
            _profileImageName = result.FileName;
            _profileImageLabel.Text = "✅";
            _profileImageFrame.BackgroundColor = Color.FromArgb("#D1FAE5");
            _profileImageFrame.BorderColor = Color.FromArgb("#10B981");
        }
        catch { }
    }

    private async Task PickCabinetImageAsync()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync();
            if (result == null) return;
            _cabinetPaths.Add(result.FullPath);
            _cabinetNames.Add(result.FileName);
            RebuildCabinetRow();
        }
        catch { }
    }

    private async Task GoNextAsync()
    {
        if (string.IsNullOrWhiteSpace(_phoneEntry.Text))
        {
            await DisplayAlert("Champ requis", "Veuillez entrer votre numéro de téléphone.", "OK");
            return;
        }

        var profile = new UpdateDoctorProfileRequest
        {
            PhoneNumber     = _phoneEntry.Text.Trim(),
            Address         = _addressEntry.Text?.Trim(),
            City            = _cityPicker.SelectedItem?.ToString()?.Trim(),
            HospitalName    = _hospitalEntry.Text?.Trim(),
            HospitalSection = _sectionEntry.Text?.Trim(),
            Biography       = _bioEditor.Text?.Trim(),
            Latitude        = _lat != 0 ? _lat : null,
            Longitude       = _lng != 0 ? _lng : null,
        };

        if (decimal.TryParse(_feeEntry.Text, out var fee))   profile.ConsultationFee = fee;
        if (int.TryParse(_expEntry.Text, out var exp))       profile.YearsOfExperience = exp;

        var loading = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#6366F1"),
            HorizontalOptions = LayoutOptions.Center };
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center, Children = { loading,
                new Label { Text = "Enregistrement…", HorizontalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#6366F1") }
            }
        };

        await _api.UpdateDoctorProfileAsync(_doctorId, profile);

        // Upload profile image if picked
        if (_profileImagePath != null)
            await _api.UploadDoctorProfileImageAsync(_doctorId, _profileImagePath, _profileImageName!, "image/jpeg");

        // Upload cabinet images
        foreach (var (path, name) in _cabinetPaths.Zip(_cabinetNames))
            await _api.UploadCabinetImageAsync(_doctorId, path, name, "image/jpeg");

        await Navigation.PushAsync(new DoctorScheduleSetupPage(_api));
    }

    // ── Address / map helpers ─────────────────────────────────
    private void InitAddressMap()
    {
        // City picker — Moroccan cities
        _cityPicker = new Picker
        {
            Title = "Sélectionner une ville",
            FontSize = 14,
            TextColor = Color.FromArgb("#1A1A2E"),
            BackgroundColor = Colors.Transparent
        };
        foreach (var c in new[]
        {
            "Agadir","Al Hoceima","Béni Mellal","Berrechid","Casablanca","Dakhla","El Jadida",
            "Errachidia","Essaouira","Fès","Guelmim","Ifrane","Kénitra","Khémisset","Khénifra",
            "Laâyoune","Marrakech","Meknès","Mohammedia","Nador","Ouarzazate","Oujda","Rabat",
            "Salé","Safi","Settat","Sidi Kacem","Tanger","Taza","Tétouan","Tiznit"
        }.OrderBy(x => x)) _cityPicker.Items.Add(c);

        // Map WebView
        _mapWebView = new WebView { HeightRequest = 220 };
        _mapWebView.Navigating += (_, e) =>
        {
            if (!e.Url.StartsWith("maui://location/")) return;
            e.Cancel = true;
            var parts = e.Url["maui://location/".Length..].Split('/');
            if (parts.Length == 2
                && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                && double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
            {
                _lat = lat; _lng = lng;
                _ = ReverseGeocodeAsync(lat, lng);
            }
        };

        _mapFrame = new Frame
        {
            IsVisible = false, Padding = 0, CornerRadius = 10,
            BorderColor = Color.FromArgb("#C7D2FE"), HasShadow = false,
            Content = _mapWebView
        };

        _mapBtnLbl = new Label
        {
            Text = "📍 Voir sur la carte",
            FontSize = 13, TextColor = Color.FromArgb("#6366F1"),
            HorizontalOptions = LayoutOptions.Start,
            Padding = new Thickness(0, 2, 0, 4)
        };
        _mapBtnLbl.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await ShowMapPreviewAsync())
        });
    }

    private async Task ShowMapPreviewAsync()
    {
        var addr = _addressEntry.Text?.Trim() ?? "";
        var city = _cityPicker.SelectedItem?.ToString() ?? "";
        var query = string.Join(" ", new[] { addr, city, "Maroc" }.Where(s => !string.IsNullOrEmpty(s)));
        if (string.IsNullOrWhiteSpace(addr) && string.IsNullOrWhiteSpace(city))
        {
            await DisplayAlert("Champ vide", "Saisissez une adresse ou sélectionnez une ville.", "OK");
            return;
        }

        _mapBtnLbl.Text = "🔍 Recherche en cours…";
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1&countrycodes=ma";
            var json = await _http.GetStringAsync(url);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.GetArrayLength() == 0)
            {
                await DisplayAlert("Adresse introuvable", "Essayez une adresse plus précise.", "OK");
                return;
            }
            var first = doc.RootElement[0];
            _lat = double.Parse(first.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            _lng = double.Parse(first.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            _mapWebView.Source = new HtmlWebViewSource { Html = GetLeafletHtml(_lat, _lng, addr) };
            _mapFrame.IsVisible = true;
        }
        catch
        {
            await DisplayAlert("Erreur réseau", "Impossible d'afficher la carte. Vérifiez votre connexion.", "OK");
        }
        finally { _mapBtnLbl.Text = "📍 Voir sur la carte"; }
    }

    private async Task ReverseGeocodeAsync(double lat, double lng)
    {
        try
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={lat.ToString(ic)}&lon={lng.ToString(ic)}&format=json";
            var json = await _http.GetStringAsync(url);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("display_name", out var dn))
            {
                var addr = dn.GetString() ?? "";
                var shortAddr = addr.Split(',')[0].Trim();
                MainThread.BeginInvokeOnMainThread(() => _addressEntry.Text = shortAddr);
            }
        }
        catch { }
    }

    private static string GetLeafletHtml(double lat, double lng, string label)
    {
        var ic  = System.Globalization.CultureInfo.InvariantCulture;
        var sLat = lat.ToString(ic); var sLng = lng.ToString(ic);
        var safeLabel = label.Replace("'", "\\'").Replace("\n", " ");
        return $@"<!DOCTYPE html><html><head>
<meta charset='utf-8'/>
<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>body{{margin:0;padding:0}}#map{{width:100%;height:220px}}</style>
</head><body><div id='map'></div>
<script>
var map=L.map('map',{{zoomControl:true,attributionControl:false}}).setView([{sLat},{sLng}],16);
L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png').addTo(map);
var mk=L.marker([{sLat},{sLng}],{{draggable:true}}).addTo(map).bindPopup('{safeLabel}').openPopup();
function notify(lat,lng){{window.location.href='maui://location/'+lat+'/'+lng;}}
mk.on('dragend',function(){{var p=mk.getLatLng();notify(p.lat,p.lng);}});
map.on('click',function(e){{mk.setLatLng(e.latlng);notify(e.latlng.lat,e.latlng.lng);}});
</script></body></html>";
    }

    // ── Helpers ──────────────────────────────────────────────
    private static Entry MakeEntry(string placeholder, Keyboard? keyboard = null) => new Entry
    {
        Placeholder = placeholder,
        BackgroundColor = Colors.White,
        FontSize = 14,
        TextColor = Color.FromArgb("#1A1A2E"),
        Keyboard = keyboard ?? Keyboard.Default
    };

    private static View MakeProgressBar(double pct) => new Frame
    {
        HeightRequest = 6, CornerRadius = 3, Padding = 0, HasShadow = false,
        BackgroundColor = Color.FromArgb("#E0E7FF"), BorderColor = Colors.Transparent,
        Content = new BoxView
        {
            Color = Color.FromArgb("#6366F1"),
            WidthRequest = 320 * pct,
            HeightRequest = 6,
            HorizontalOptions = LayoutOptions.Start
        }
    };

    private static Frame Card(View content) => new Frame
    {
        CornerRadius = 16, BackgroundColor = Colors.White,
        BorderColor = Color.FromArgb("#E0E7FF"),
        Padding = new Thickness(16), HasShadow = false,
        Content = content
    };

    private static Label SectionTitle(string text) => new Label
    {
        Text = text, FontSize = 15, FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#1A1A2E")
    };

    private static Grid RowOf(View left, View right)
    {
        var g = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(10) },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        g.Children.Add(left);
        Grid.SetColumn(right, 2);
        g.Children.Add(right);
        return g;
    }

    private static Frame MakeButton(string text, string color, Func<Task> action)
    {
        var label = new Label
        {
            Text = text, FontSize = 17, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        var frame = new Frame
        {
            CornerRadius = 14, BackgroundColor = Color.FromArgb(color),
            Padding = new Thickness(0, 16), HasShadow = true,
            BorderColor = Colors.Transparent,
            Content = label
        };
        frame.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await action())
        });
        return frame;
    }
}

// ════════════════════════════════════════════════════════════
// STEP 2 — Slot duration + working hours + vacances
// ════════════════════════════════════════════════════════════
public class DoctorScheduleSetupPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Guid _doctorId;

    // Duration
    private int _slotDuration = 30;
    private Frame? _sel15, _sel30, _sel60;

    // Heures de travail
    private static readonly string[] DayNames = { "Dimanche", "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi" };
    private readonly bool[] _dayEnabled = { false, true, true, true, true, true, false };
    private readonly List<List<(TimePicker Start, TimePicker End, Picker Type)>> _daySlots;
    private readonly VerticalStackLayout _daysStack = new() { Spacing = 10 };

    // Vacances
    private readonly List<(Entry Label, DatePicker Start, DatePicker End)> _vacations = new();
    private readonly VerticalStackLayout _vacationsGrid = new() { Spacing = 10 };

    // Tab views
    private VerticalStackLayout? _hoursContent;
    private VerticalStackLayout? _vacancesContent;
    private BoxView? _hoursUnderline, _vacancesUnderline;
    private Label? _hoursTabLabel, _vacancesTabLabel;

    public DoctorScheduleSetupPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFF");

        _daySlots = Enumerable.Range(0, 7)
            .Select(_ => new List<(TimePicker Start, TimePicker End, Picker Type)>()).ToList();
        for (int d = 1; d <= 5; d++)
            _daySlots[d].Add(MakeSlotTuple(9, 0, 17, 0));

        _hoursContent = BuildHoursContent();
        _vacancesContent = BuildVacancesContent();
        _vacancesContent.IsVisible = false;

        BuildDaysStack();
        PreFillMoroccanHolidays();

        var scroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 48, 20, 40),
                Spacing = 16,
                Children =
                {
                    // Header
                    new Label
                    {
                        Text = "Configurer votre Agenda",
                        FontSize = 22, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#1A1A2E"),
                        HorizontalOptions = LayoutOptions.Center
                    },

                    // Duration card
                    Card(BuildDurationSection()),

                    // Tab bar
                    BuildTabBar(),

                    // Tab contents
                    _hoursContent,
                    _vacancesContent
                }
            }
        };

        Content = scroll;
    }

    // ── Duration ────────────────────────────────────────────

    private VerticalStackLayout BuildDurationSection()
    {
        _sel15 = DurationCard("15 min", 15);
        _sel30 = DurationCard("30 min", 30);
        _sel60 = DurationCard("60 min", 60);
        HighlightDuration();

        return new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "Durée du rendez-vous",
                    FontSize = 14, FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#374151")
                },
                new HorizontalStackLayout
                {
                    Spacing = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    Children = { _sel15, _sel30, _sel60 }
                }
            }
        };
    }

    private Frame DurationCard(string label, int minutes)
    {
        var iconLabel = new Label
        {
            Text = "🕐", FontSize = 26,
            HorizontalOptions = LayoutOptions.Center
        };
        var textLabel = new Label
        {
            Text = label, FontSize = 13, FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Color.FromArgb("#1A1A2E")
        };
        var frame = new Frame
        {
            WidthRequest = 88, HeightRequest = 88,
            CornerRadius = 16, Padding = 8, HasShadow = false,
            Content = new VerticalStackLayout
            {
                Spacing = 4, VerticalOptions = LayoutOptions.Center,
                Children = { iconLabel, textLabel }
            }
        };
        frame.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => { _slotDuration = minutes; HighlightDuration(); })
        });
        return frame;
    }

    private void HighlightDuration()
    {
        void Style(Frame f, bool sel)
        {
            f.BackgroundColor = sel ? Color.FromArgb("#0D9488") : Colors.White;
            f.BorderColor = sel ? Color.FromArgb("#0D9488") : Color.FromArgb("#E5E7EB");
            if (f.Content is VerticalStackLayout vsl)
                foreach (var child in vsl.Children)
                    if (child is Label lbl)
                        lbl.TextColor = sel ? Colors.White : Color.FromArgb("#1A1A2E");
        }
        Style(_sel15!, _slotDuration == 15);
        Style(_sel30!, _slotDuration == 30);
        Style(_sel60!, _slotDuration == 60);
    }

    // ── Tab bar ─────────────────────────────────────────────

    private View BuildTabBar()
    {
        _hoursUnderline = new BoxView { HeightRequest = 3, Color = Color.FromArgb("#0D9488"), IsVisible = true };
        _vacancesUnderline = new BoxView { HeightRequest = 3, Color = Color.FromArgb("#0D9488"), IsVisible = false };

        _hoursTabLabel = new Label
        {
            Text = "Heures de travail", FontSize = 15, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0D9488"), HorizontalOptions = LayoutOptions.Center
        };
        _vacancesTabLabel = new Label
        {
            Text = "Vacances", FontSize = 15,
            TextColor = Color.FromArgb("#6B7280"), HorizontalOptions = LayoutOptions.Center
        };

        var hoursTab = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Children = { _hoursTabLabel, _hoursUnderline }
        };
        hoursTab.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => SwitchTab(true))
        });

        var vacancesTab = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Children = { _vacancesTabLabel, _vacancesUnderline }
        };
        vacancesTab.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => SwitchTab(false))
        });

        var separator = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E5E7EB"), Margin = new Thickness(0, -1, 0, 0) };

        Grid.SetColumn(vacancesTab, 1);
        var tabGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            BackgroundColor = Colors.White
        };
        tabGrid.Children.Add(hoursTab);
        tabGrid.Children.Add(vacancesTab);

        return new VerticalStackLayout
        {
            Spacing = 0,
            Children = { tabGrid, separator }
        };
    }

    private void SwitchTab(bool showHours)
    {
        _hoursContent!.IsVisible = showHours;
        _vacancesContent!.IsVisible = !showHours;
        _hoursUnderline!.IsVisible = showHours;
        _vacancesUnderline!.IsVisible = !showHours;
        _hoursTabLabel!.TextColor = showHours ? Color.FromArgb("#0D9488") : Color.FromArgb("#6B7280");
        _hoursTabLabel!.FontAttributes = showHours ? FontAttributes.Bold : FontAttributes.None;
        _vacancesTabLabel!.TextColor = showHours ? Color.FromArgb("#6B7280") : Color.FromArgb("#0D9488");
        _vacancesTabLabel!.FontAttributes = showHours ? FontAttributes.None : FontAttributes.Bold;
    }

    // ── Heures de travail tab ───────────────────────────────

    private VerticalStackLayout BuildHoursContent()
    {
        return new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                _daysStack,
                MakeButton("Enregistrer les horaires", "#0D9488", async () => await SaveScheduleAsync())
            }
        };
    }

    private void BuildDaysStack()
    {
        _daysStack.Children.Clear();
        for (int d = 0; d < 7; d++)
            _daysStack.Children.Add(BuildDayRow(d));
    }

    private View BuildDayRow(int dayIndex)
    {
        var checkbox = new CheckBox { IsChecked = _dayEnabled[dayIndex], Color = Color.FromArgb("#0D9488") };
        var dayLabel = new Label
        {
            Text = DayNames[dayIndex], FontSize = 14, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1A1A2E"),
            VerticalOptions = LayoutOptions.Center
        };

        var slotsStack = new VerticalStackLayout { Spacing = 8, Margin = new Thickness(32, 4, 0, 0) };

        // Add-slot button (right side)
        var addBtn = new Label
        {
            Text = "⊕", FontSize = 22,
            TextColor = Color.FromArgb("#0D9488"),
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 2, 0, 0)
        };

        void RebuildSlots()
        {
            slotsStack.Children.Clear();
            if (!_dayEnabled[dayIndex]) { addBtn.IsVisible = false; return; }
            addBtn.IsVisible = true;

            foreach (var (start, end, type) in _daySlots[dayIndex])
            {
                var capturedTuple = (start, end, type);
                var removeBtn = new Label
                {
                    Text = "⊗", FontSize = 20,
                    TextColor = Color.FromArgb("#EF4444"),
                    VerticalOptions = LayoutOptions.Center
                };
                removeBtn.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        _daySlots[dayIndex].Remove(capturedTuple);
                        RebuildSlots();
                    })
                });

                var startFrame = new Frame
                {
                    CornerRadius = 8, Padding = new Thickness(4, 2),
                    BackgroundColor = Color.FromArgb("#F0FDF4"),
                    BorderColor = Color.FromArgb("#6EE7B7"),
                    HasShadow = false, Content = start,
                    WidthRequest = 90
                };
                var endFrame = new Frame
                {
                    CornerRadius = 8, Padding = new Thickness(4, 2),
                    BackgroundColor = Color.FromArgb("#F0FDF4"),
                    BorderColor = Color.FromArgb("#6EE7B7"),
                    HasShadow = false, Content = end,
                    WidthRequest = 90
                };

                var typeFrame = new Frame
                {
                    CornerRadius = 8, Padding = new Thickness(4, 2),
                    BackgroundColor = Colors.White,
                    BorderColor = Color.FromArgb("#E5E7EB"),
                    HasShadow = false, Content = type
                };

                var slotRow = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = new GridLength(8, GridUnitType.Absolute) },
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 4,
                    VerticalOptions = LayoutOptions.Center
                };
                slotRow.Children.Add(startFrame);
                var arrow = new Label { Text = "→", FontSize = 14, TextColor = Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center };
                Grid.SetColumn(arrow, 1); slotRow.Children.Add(arrow);
                Grid.SetColumn(endFrame, 2); slotRow.Children.Add(endFrame);
                Grid.SetColumn(typeFrame, 3); slotRow.Children.Add(typeFrame);
                Grid.SetColumn(removeBtn, 4); slotRow.Children.Add(removeBtn);

                slotsStack.Children.Add(slotRow);
            }
        }

        addBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                _daySlots[dayIndex].Add(MakeSlotTuple(9, 0, 17, 0));
                RebuildSlots();
            })
        });

        checkbox.CheckedChanged += (s, e) =>
        {
            _dayEnabled[dayIndex] = e.Value;
            if (e.Value && _daySlots[dayIndex].Count == 0)
                _daySlots[dayIndex].Add(MakeSlotTuple(9, 0, 17, 0));
            RebuildSlots();
        };

        RebuildSlots();

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 6
        };
        headerGrid.Children.Add(checkbox);
        Grid.SetColumn(dayLabel, 1); headerGrid.Children.Add(dayLabel);
        Grid.SetColumn(addBtn, 2); headerGrid.Children.Add(addBtn);

        return new Frame
        {
            CornerRadius = 12, Padding = new Thickness(10, 8),
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            HasShadow = false,
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children = { headerGrid, slotsStack }
            }
        };
    }

    private async Task SaveScheduleAsync()
    {
        var slots = new List<DoctorScheduleItemDto>();
        for (int d = 0; d < 7; d++)
        {
            if (!_dayEnabled[d]) continue;
            foreach (var (start, end, type) in _daySlots[d])
            {
                slots.Add(new DoctorScheduleItemDto
                {
                    DayOfWeek = d,
                    StartTime = start.Time,
                    EndTime = end.Time,
                    IsAvailable = true,
                    ConsultationType = type.SelectedItem?.ToString() ?? "Présentiel"
                });
            }
        }

        if (slots.Count == 0)
        {
            await DisplayAlert("Requis", "Veuillez activer au moins un jour de travail.", "OK");
            return;
        }

        await _api.UpdateDoctorProfileAsync(_doctorId, new UpdateDoctorProfileRequest
        {
            SlotDurationMinutes = _slotDuration
        });
        await _api.SaveDoctorScheduleAsync(_doctorId, slots);
        await DisplayAlert("Succès", "Horaires enregistrés.", "OK");
    }

    // ── Vacances tab ─────────────────────────────────────────

    private VerticalStackLayout BuildVacancesContent()
    {
        var addBtn = new Frame
        {
            CornerRadius = 10, Padding = new Thickness(0, 10),
            BackgroundColor = Color.FromArgb("#F0FDF4"),
            BorderColor = Color.FromArgb("#6EE7B7"), HasShadow = false,
            Content = new Label
            {
                Text = "⊕  Ajouter une période",
                FontSize = 14, TextColor = Color.FromArgb("#0D9488"),
                HorizontalOptions = LayoutOptions.Center
            }
        };
        addBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => AddVacationRow(DateTime.Today, DateTime.Today.AddDays(1), ""))
        });

        return new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                _vacationsGrid,
                addBtn,
                MakeButton("Enregistrer les vacances", "#0D9488", async () => await SaveVacationsAsync())
            }
        };
    }

    private void PreFillMoroccanHolidays()
    {
        var holidays = new (string Label, DateTime Start, DateTime End)[]
        {
            ("Jour de l'An",              new DateTime(2026, 1,  1),  new DateTime(2026, 1,  1)),
            ("Manifeste de l'Indépendance",new DateTime(2026, 1, 11), new DateTime(2026, 1, 11)),
            ("Aid Al Fitr",               new DateTime(2026, 3, 20),  new DateTime(2026, 3, 21)),
            ("Aid Al Adha",               new DateTime(2026, 5, 27),  new DateTime(2026, 5, 28)),
            ("Fête du Travail",           new DateTime(2026, 5,  1),  new DateTime(2026, 5,  1)),
            ("Nouvel An Hégirien",        new DateTime(2026, 6, 17),  new DateTime(2026, 6, 17)),
            ("Fête du Trône",             new DateTime(2026, 7, 30),  new DateTime(2026, 7, 30)),
            ("Anniversaire de la Révolution",new DateTime(2026, 8, 20),new DateTime(2026, 8, 20)),
            ("Fête de l'Indépendance",    new DateTime(2026, 11, 18), new DateTime(2026, 11, 18))
        };
        foreach (var (lbl, s, e) in holidays)
            AddVacationRow(s, e, lbl);
    }

    private void AddVacationRow(DateTime start, DateTime end, string label)
    {
        var labelEntry = new Entry
        {
            Text = label, Placeholder = "Libellé",
            FontSize = 13, TextColor = Color.FromArgb("#1A1A2E"),
            BackgroundColor = Colors.Transparent
        };
        var startPicker = new DatePicker
        {
            Date = start, Format = "dd/MM/yy",
            TextColor = Color.FromArgb("#1A1A2E"),
            FontSize = 12
        };
        var endPicker = new DatePicker
        {
            Date = end, Format = "dd/MM/yy",
            TextColor = Color.FromArgb("#1A1A2E"),
            FontSize = 12
        };
        var tuple = (labelEntry, startPicker, endPicker);
        _vacations.Add(tuple);

        var removeBtn = new Label
        {
            Text = "⊗", FontSize = 18,
            TextColor = Color.FromArgb("#EF4444"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };
        removeBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                _vacations.Remove(tuple);
                RebuildVacationsGrid();
            })
        });

        Grid.SetColumn(removeBtn, 1);
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        headerGrid.Children.Add(labelEntry);
        headerGrid.Children.Add(removeBtn);

        var card = new Frame
        {
            CornerRadius = 12, Padding = new Thickness(12, 10),
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            HasShadow = false,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    headerGrid,
                    new Frame
                    {
                        CornerRadius = 8, Padding = new Thickness(8, 4),
                        BackgroundColor = Color.FromArgb("#F9FAFB"),
                        BorderColor = Color.FromArgb("#E5E7EB"), HasShadow = false,
                        Content = startPicker
                    },
                    new Frame
                    {
                        CornerRadius = 8, Padding = new Thickness(8, 4),
                        BackgroundColor = Color.FromArgb("#F9FAFB"),
                        BorderColor = Color.FromArgb("#E5E7EB"), HasShadow = false,
                        Content = endPicker
                    }
                }
            }
        };

        _vacationsGrid.Children.Add(card);
    }

    private void RebuildVacationsGrid()
    {
        _vacationsGrid.Children.Clear();
        var snapshot = _vacations.ToList();
        _vacations.Clear();
        foreach (var (lbl, s, e) in snapshot)
            AddVacationRow(s.Date, e.Date, lbl.Text ?? "");
    }

    private async Task SaveVacationsAsync()
    {
        var dtos = _vacations.Select(v => new DoctorVacationDto
        {
            Label = v.Label.Text ?? "",
            StartDate = v.Start.Date,
            EndDate = v.End.Date
        }).ToList();

        await _api.SaveDoctorVacationsAsync(_doctorId, dtos);

        var signalR = ServiceHelper.GetService<SignalRService>();
        Application.Current!.MainPage = new NavigationPage(
            new DoctorDashboardPage(ServiceHelper.GetService<ApiService>(), signalR));
    }

    // ── Shared helpers ───────────────────────────────────────

    private static (TimePicker Start, TimePicker End, Picker Type) MakeSlotTuple(int sh, int sm, int eh, int em)
    {
        var typePicker = new Picker
        {
            TextColor = Color.FromArgb("#374151"),
            FontSize = 12,
            Title = "Type"
        };
        typePicker.Items.Add("Présentiel");
        typePicker.Items.Add("Téléconsultation");
        typePicker.SelectedIndex = 0;

        return (
            new TimePicker { Time = new TimeSpan(sh, sm, 0), TextColor = Color.FromArgb("#1A1A2E") },
            new TimePicker { Time = new TimeSpan(eh, em, 0), TextColor = Color.FromArgb("#1A1A2E") },
            typePicker
        );
    }

    private static Frame Card(View content) => new Frame
    {
        CornerRadius = 16, BackgroundColor = Colors.White,
        BorderColor = Color.FromArgb("#E5E7EB"),
        Padding = new Thickness(16), HasShadow = false,
        Content = content
    };

    private static Frame MakeButton(string text, string color, Func<Task> action)
    {
        var frame = new Frame
        {
            CornerRadius = 14, BackgroundColor = Color.FromArgb(color),
            Padding = new Thickness(0, 16), HasShadow = false,
            BorderColor = Colors.Transparent,
            Content = new Label
            {
                Text = text, FontSize = 16, FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        frame.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await action())
        });
        return frame;
    }
}

// ════════════════════════════════════════════════════════════
// POST-REGISTRATION — Welcome & Onboarding Checklist
// ════════════════════════════════════════════════════════════
public class DoctorOnboardingPage : ContentPage
{
    private readonly ApiService _api;

    public DoctorOnboardingPage(ApiService api, string firstName)
    {
        _api = api;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.White;

        var root = new VerticalStackLayout { Spacing = 0 };

        // Header
        root.Children.Add(new VerticalStackLayout
        {
            Padding = new Thickness(24, 56, 24, 20), BackgroundColor = Colors.White, Spacing = 8,
            Children =
            {
                new Label
                {
                    FormattedText = new FormattedString
                    {
                        Spans =
                        {
                            new Span { Text = "Bienvenue sur le logiciel médical MediCare+, ", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") },
                            new Span { Text = firstName, FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0EA5E9") },
                            new Span { Text = ".", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") }
                        }
                    },
                    LineBreakMode = LineBreakMode.WordWrap
                },
                new Label { Text = "Effectuez ces tâches à votre rythme pour commencer à utiliser et bénéficier de MediCare+.", FontSize = 14, TextColor = Color.FromArgb("#64748B"), LineBreakMode = LineBreakMode.WordWrap }
            }
        });

        // Progress bar 80%
        root.Children.Add(new VerticalStackLayout
        {
            Padding = new Thickness(24, 4, 24, 0), Spacing = 4,
            Children =
            {
                new ProgressBar { Progress = 0.8, ProgressColor = Color.FromArgb("#0EA5E9"), HeightRequest = 10 },
                new Label { Text = "80%", FontSize = 12, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.End }
            }
        });

        root.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0"), Margin = new Thickness(0, 24, 0, 0) });

        // "Que faire ensuite ?"
        root.Children.Add(new VerticalStackLayout
        {
            Padding = new Thickness(24, 24, 24, 24), Spacing = 10,
            Children =
            {
                new Label { Text = "Que faire ensuite ?", FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0EA5E9") },
                new Label
                {
                    FormattedText = new FormattedString
                    {
                        Spans =
                        {
                            new Span { Text = "Activer ", FontSize = 14, TextColor = Color.FromArgb("#374151") },
                            new Span { Text = "le logiciel médical MediCare+", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") },
                            new Span { Text = ", le paramètrage de votre compte prend environ 3 minutes.", FontSize = 14, TextColor = Color.FromArgb("#374151") }
                        }
                    },
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }
        });

        root.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0") });
        root.Children.Add(DoneItem("Créer un compte", "Vous y êtes arrivé ! Maintenant, lancez-vous."));
        root.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0") });
        root.Children.Add(DoneItem("Vérifier votre identité", "Et oui, chez MediCare+ on vous protège contre la fraude d'identité !"));
        root.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0") });

        var locBtn = new Button
        {
            Text = "Configurer la localisation",
            BackgroundColor = Color.FromArgb("#0EA5E9"), TextColor = Colors.White,
            FontSize = 13, CornerRadius = 8, Padding = new Thickness(14, 8),
            HorizontalOptions = LayoutOptions.Start
        };
        locBtn.Clicked += async (_, _) =>
            await Navigation.PushAsync(new DoctorLocationPage(ServiceHelper.GetService<ApiService>()));

        root.Children.Add(PendingItem("Configurer la localisation de votre cabinet", "Renseigner votre localisation et quelques informations sur votre activité.", locBtn));
        root.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0") });
        root.Children.Add(PendingItem("Configurer votre Agenda", "Indiquer vos disponibilités et horaires de travail.", null));
        root.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0") });

        // Footer
        root.Children.Add(new VerticalStackLayout
        {
            Padding = new Thickness(40, 40, 40, 56), Spacing = 14,
            Children =
            {
                new Label { Text = "🎯", FontSize = 44, HorizontalOptions = LayoutOptions.Center },
                new Label
                {
                    Text = "Publier votre page web et commencer à recevoir les patients !",
                    FontSize = 15, FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#0369A1"),
                    HorizontalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }
        });

        Content = new ScrollView { Content = root };
    }

    private static View DoneItem(string title, string subtitle)
    {
        var check = new Frame { WidthRequest = 30, HeightRequest = 30, CornerRadius = 15, BackgroundColor = Color.FromArgb("#0EA5E9"), BorderColor = Colors.Transparent, HasShadow = false, Padding = 0, VerticalOptions = LayoutOptions.Start, Content = new Label { Text = "✓", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
        var text = new VerticalStackLayout { Spacing = 4, Children = { new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151"), TextDecorations = TextDecorations.Strikethrough }, new Label { Text = subtitle, FontSize = 13, TextColor = Color.FromArgb("#0EA5E9"), LineBreakMode = LineBreakMode.WordWrap } } };
        var g = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 16, Padding = new Thickness(24, 16) };
        g.Children.Add(check);
        Grid.SetColumn(text, 1); g.Children.Add(text);
        return g;
    }

    private static View PendingItem(string title, string subtitle, View? actionBtn)
    {
        var check = new Frame { WidthRequest = 30, HeightRequest = 30, CornerRadius = 15, BackgroundColor = Color.FromArgb("#E2E8F0"), BorderColor = Colors.Transparent, HasShadow = false, Padding = 0, VerticalOptions = LayoutOptions.Start, Content = new Label { Text = "✓", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
        var textStack = new VerticalStackLayout { Spacing = 4 };
        textStack.Children.Add(new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B") });
        textStack.Children.Add(new Label { Text = subtitle, FontSize = 13, TextColor = Color.FromArgb("#64748B"), LineBreakMode = LineBreakMode.WordWrap });
        if (actionBtn != null) { textStack.Children.Add(new BoxView { HeightRequest = 8 }); textStack.Children.Add(actionBtn); }
        var g = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 16, Padding = new Thickness(24, 16) };
        g.Children.Add(check);
        Grid.SetColumn(textStack, 1); g.Children.Add(textStack);
        return g;
    }
}

// ════════════════════════════════════════════════════════════
// DOCTOR LOCATION — Géolocalisation + Arabic info + Cabinet photos
// ════════════════════════════════════════════════════════════
public class DoctorLocationPage : ContentPage
{
    private readonly ApiService _api;
    private readonly Guid _doctorId;
    private double _latitude  = 33.5731;
    private double _longitude = -7.5898;
    private readonly WebView _mapView;
    private readonly Entry _lastNameAr  = ArEntry("الاسم العائلي بالعربية *");
    private readonly Entry _firstNameAr = ArEntry("الاسم الشخصي بالعربية *");
    private readonly Entry _addressAr   = ArEntry("العنوان بالعربية *");
    private readonly List<string> _cabinetPaths = new();
    private readonly List<string> _cabinetNames = new();
    private readonly HorizontalStackLayout _cabinetRow;
    private readonly Label _errLabel = new Label { TextColor = Color.FromArgb("#DC2626"), FontSize = 13 };
    private readonly Frame _errFrame;
    private readonly Button _saveBtn;
    private readonly ActivityIndicator _spinner = new ActivityIndicator { Color = Color.FromArgb("#0369A1"), IsRunning = false, IsVisible = false, HorizontalOptions = LayoutOptions.Center };

    public DoctorLocationPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFF");

        _errFrame = new Frame { BackgroundColor = Color.FromArgb("#FEE2E2"), BorderColor = Color.FromArgb("#FECACA"), CornerRadius = 10, Padding = 12, IsVisible = false, Content = _errLabel };
        _saveBtn = new Button { Text = "Enregistrer", BackgroundColor = Color.FromArgb("#0369A1"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 15, CornerRadius = 10, Padding = new Thickness(20, 0) };
        _saveBtn.Clicked += OnSave;

        _mapView = new WebView { HeightRequest = 280, Source = new HtmlWebViewSource { Html = MapHtml() } };
        _mapView.Navigating += OnMapNavigating;

        var addBtn = MakeAddCabBtn();
        _cabinetRow = new HorizontalStackLayout { Spacing = 10 };
        _cabinetRow.Children.Add(addBtn);

        var gpsBtn = new Button { Text = "Ma position actuelle", BackgroundColor = Color.FromArgb("#E0F2FE"), TextColor = Color.FromArgb("#0369A1"), FontSize = 13, CornerRadius = 8, FontAttributes = FontAttributes.Bold };
        gpsBtn.Clicked += OnGetLocation;

        var root = new VerticalStackLayout { Spacing = 0 };

        root.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#0369A1"), Padding = new Thickness(24, 52, 24, 28),
            Children = { new VerticalStackLayout { Spacing = 6, Children = {
                new Label { Text = "Localisation du cabinet", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = "Indiquez votre position pour que les patients puissent vous trouver.", FontSize = 13, TextColor = Colors.White.WithAlpha(0.85f), LineBreakMode = LineBreakMode.WordWrap }
            }}}
        });

        var form = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(20, 24, 20, 40) };

        // Arabic names (RTL side-by-side)
        var arRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(new GridLength(10)), new(GridLength.Star) } };
        arRow.Children.Add(ArField("الاسم العائلي بالعربية *", _lastNameAr));
        var arLeft = ArField("الاسم الشخصي بالعربية *", _firstNameAr);
        Grid.SetColumn(arLeft, 2); arRow.Children.Add(arLeft);
        form.Children.Add(arRow);

        form.Children.Add(ArField("العنوان بالعربية *", _addressAr));

        // Map
        form.Children.Add(new Label { Text = "Géolocalisation *", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B"), Margin = new Thickness(0, 4, 0, 0) });
        form.Children.Add(gpsBtn);
        form.Children.Add(new Frame { BorderColor = Color.FromArgb("#CBD5E1"), CornerRadius = 16, Padding = 0, HasShadow = false, Content = _mapView, HeightRequest = 280 });

        // Cabinet images
        form.Children.Add(new Label { Text = "Les images du cabinet", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E1B4B"), Margin = new Thickness(0, 4, 0, 0) });
        form.Children.Add(new Frame
        {
            BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 14, Padding = new Thickness(16, 12),
            HasShadow = false, BackgroundColor = Colors.White,
            Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = _cabinetRow }
        });

        form.Children.Add(_errFrame);
        form.Children.Add(_spinner);

        var backBtn = new Button { Text = "Retour", BackgroundColor = Color.FromArgb("#E2E8F0"), TextColor = Color.FromArgb("#374151"), FontSize = 15, CornerRadius = 10, Padding = new Thickness(20, 0) };
        backBtn.Clicked += async (_, _) => await Navigation.PopAsync();
        var btnRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(new GridLength(12)), new(GridLength.Star) } };
        btnRow.Children.Add(backBtn);
        Grid.SetColumn(_saveBtn, 2); btnRow.Children.Add(_saveBtn);
        form.Children.Add(btnRow);

        root.Children.Add(new Frame { CornerRadius = 0, BackgroundColor = Color.FromArgb("#F8FAFF"), HasShadow = false, BorderColor = Colors.Transparent, Padding = 0, Content = new ScrollView { Content = form } });
        Content = root;
    }

    private void OnMapNavigating(object? s, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("location://")) return;
        e.Cancel = true;
        var parts = e.Url["location://".Length..].Split(',');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
        { _latitude = lat; _longitude = lng; }
    }

    private async void OnGetLocation(object? s, EventArgs e)
    {
        try
        {
            var loc = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            if (loc == null) return;
            _latitude = loc.Latitude; _longitude = loc.Longitude;
            var lat = loc.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var lng = loc.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            await _mapView.EvaluateJavaScriptAsync($"move({lat},{lng})");
        }
        catch { }
    }

    private async void OnSave(object? s, EventArgs e)
    {
        _errFrame.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_lastNameAr.Text) || string.IsNullOrWhiteSpace(_firstNameAr.Text) || string.IsNullOrWhiteSpace(_addressAr.Text))
        { _errLabel.Text = "Veuillez remplir tous les champs obligatoires."; _errFrame.IsVisible = true; return; }

        _saveBtn.IsEnabled = false; _spinner.IsRunning = _spinner.IsVisible = true;
        try
        {
            await _api.UpdateDoctorProfileAsync(_doctorId, new UpdateDoctorProfileRequest
            {
                LastNameAr = _lastNameAr.Text.Trim(), FirstNameAr = _firstNameAr.Text.Trim(),
                AddressAr = _addressAr.Text.Trim(), Latitude = _latitude, Longitude = _longitude
            });
            foreach (var (path, name) in _cabinetPaths.Zip(_cabinetNames))
                await _api.UploadCabinetImageAsync(_doctorId, path, name, "image/jpeg");
            await Navigation.PopAsync();
        }
        catch (Exception ex) { _errLabel.Text = $"Erreur: {ex.Message}"; _errFrame.IsVisible = true; }
        finally { _saveBtn.IsEnabled = true; _spinner.IsRunning = _spinner.IsVisible = false; }
    }

    private Frame MakeAddCabBtn()
    {
        var frame = new Frame { WidthRequest = 88, HeightRequest = 88, CornerRadius = 14, BackgroundColor = Color.FromArgb("#F1F5F9"), BorderColor = Color.FromArgb("#CBD5E1"), HasShadow = false, Padding = 0, Content = new VerticalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Spacing = 4, Children = { new Label { Text = "+", FontSize = 28, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center }, new Label { Text = "Ajouter", FontSize = 10, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center } } } };
        frame.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await PickCabinetImageAsync()) });
        return frame;
    }

    private async Task PickCabinetImageAsync()
    {
        if (_cabinetPaths.Count >= 5) return;
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result == null) return;
            _cabinetPaths.Add(result.FullPath);
            _cabinetNames.Add(result.FileName);
            var thumb = new Frame { WidthRequest = 88, HeightRequest = 88, CornerRadius = 14, Padding = 0, HasShadow = false, BorderColor = Color.FromArgb("#CBD5E1"), Content = new Image { Source = ImageSource.FromFile(result.FullPath), Aspect = Aspect.AspectFill } };
            _cabinetRow.Children.Insert(_cabinetRow.Children.Count - 1, thumb);
        }
        catch { }
    }

    private static View ArField(string label, Entry entry) => new VerticalStackLayout { Spacing = 5, Children = { new Label { Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151"), HorizontalTextAlignment = TextAlignment.End }, new Frame { BorderColor = Color.FromArgb("#E2E8F0"), CornerRadius = 12, Padding = new Thickness(12, 0), HasShadow = false, BackgroundColor = Colors.White, Content = entry } } };
    private static Entry ArEntry(string placeholder) => new Entry { Placeholder = placeholder, BackgroundColor = Colors.Transparent, FontSize = 14, TextColor = Color.FromArgb("#1E1B4B"), PlaceholderColor = Color.FromArgb("#9CA3AF"), HorizontalTextAlignment = TextAlignment.End };

    private static string MapHtml() => @"<!DOCTYPE html>
<html><head>
<meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>html,body,#map{height:100%;margin:0;padding:0;}</style>
</head><body>
<div id='map'></div>
<script>
var map=L.map('map').setView([33.5731,-7.5898],13);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'OSM'}).addTo(map);
var marker=L.marker([33.5731,-7.5898],{draggable:true}).addTo(map);
function report(lat,lng){window.location.href='location://'+lat.toFixed(6)+','+lng.toFixed(6);}
marker.on('dragend',function(){var p=marker.getLatLng();report(p.lat,p.lng);});
map.on('click',function(e){marker.setLatLng(e.latlng);report(e.latlng.lat,e.latlng.lng);});
function move(lat,lng){map.setView([lat,lng],16);marker.setLatLng([lat,lng]);}
</script></body></html>";
}
