using HospitalApp.Mobile.Models;
using HospitalApp.Mobile.Services;

namespace HospitalApp.Mobile.Pages;

public class PatientDashboardPage : ContentPage
{
    private readonly ApiService _api;
    public PatientDashboardPage(ApiService api)
    {
        _api = api;
        Title = "Dashboard";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildUI();
    }

    private void BuildUI()
    {
        var scroll = new ScrollView();
        var stack = new VerticalStackLayout { Padding = new Thickness(20), Spacing = 16 };

        var headerFrame = new Frame
        {
            CornerRadius = 20,
            Padding = new Thickness(20),
            HasShadow = false,
            BorderColor = Colors.Transparent
        };
        headerFrame.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#1D4ED8"), Offset = 0 },
                new GradientStop { Color = Color.FromArgb("#3B82F6"), Offset = 1 }
            }
        };
        headerFrame.Content = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = $"Hello, {Preferences.Get("FullName", "Patient")} 👋", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = DateTime.Today.ToString("dddd, MMM dd yyyy"), FontSize = 13, TextColor = Color.FromArgb("#BFDBFE") }
            }
        };
        stack.Children.Add(headerFrame);

        stack.Children.Add(new Label { Text = "Quick Actions", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") });

        var actionsGrid = new Grid
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

        var bookBtn = CreateActionCard("📅", "Book Appointment", "#DBEAFE", "#1D4ED8");
        bookBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
                await Navigation.PushAsync(new SpecialtySelectionPage(_api)))
        });

        var apptsBtn = CreateActionCard("🗓️", "My Appointments", "#D1FAE5", "#065F46");
        apptsBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
                await Navigation.PushAsync(new PatientAppointmentsPage(_api)))
        });

        var chatBtn = CreateActionCard("💬", "Chat", "#EDE9FE", "#5B21B6");
        var profileBtn = CreateActionCard("👤", "Profile", "#FEF3C7", "#92400E");
        profileBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
                await Navigation.PushAsync(new PatientProfilePage()))
        });

        Grid.SetRow(bookBtn, 0); Grid.SetColumn(bookBtn, 0);
        Grid.SetRow(apptsBtn, 0); Grid.SetColumn(apptsBtn, 1);
        Grid.SetRow(chatBtn, 1); Grid.SetColumn(chatBtn, 0);
        Grid.SetRow(profileBtn, 1); Grid.SetColumn(profileBtn, 1);

        actionsGrid.Children.Add(bookBtn);
        actionsGrid.Children.Add(apptsBtn);
        actionsGrid.Children.Add(chatBtn);
        actionsGrid.Children.Add(profileBtn);
        stack.Children.Add(actionsGrid);

        scroll.Content = stack;
        Content = scroll;
    }

    private Frame CreateActionCard(string emoji, string title, string bg, string textColor)
    {
        return new Frame
        {
            BackgroundColor = Color.FromArgb(bg),
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
                    new Label { Text = emoji, FontSize = 32, HorizontalOptions = LayoutOptions.Center },
                    new Label
                    {
                        Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb(textColor),
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
    }
}

public class PatientAppointmentsPage : ContentPage
{
    private readonly ApiService _api;
    public PatientAppointmentsPage(ApiService api)
    {
        _api = api;
        Title = "My Appointments";
        BackgroundColor = Color.FromArgb("#F8FAFC");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAppointments();
    }

    private async Task LoadAppointments()
    {
        var loading = new ActivityIndicator
        {
            IsRunning = true,
            Color = Color.FromArgb("#2563EB"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Children = { loading }
        };

        var patientId = Preferences.Get("ProfileId", "");
        if (string.IsNullOrEmpty(patientId)) return;

        var appointments = await _api.GetPatientAppointmentsAsync(Guid.Parse(patientId));

        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 10 };
        stack.Children.Add(new Label
        {
            Text = $"{appointments.Count} appointments",
            FontSize = 14,
            TextColor = Color.FromArgb("#64748B"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (!appointments.Any())
        {
            stack.Children.Add(new Label
            {
                Text = "No appointments yet 📅",
                FontSize = 16,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 40)
            });
        }

        foreach (var appt in appointments)
        {
            var statusFrame = new Frame
            {
                BackgroundColor = appt.StatusBg,
                CornerRadius = 8,
                Padding = new Thickness(8, 4),
                HasShadow = false,
                BorderColor = Colors.Transparent,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = appt.Status,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = appt.StatusColor
                }
            };

            var infoStack = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = appt.DoctorName ?? "", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                    new Label { Text = appt.DoctorSpecialty ?? "", FontSize = 12, TextColor = Color.FromArgb("#2563EB") },
                    new Label { Text = appt.DateDisplay, FontSize = 12, TextColor = Color.FromArgb("#64748B") },
                    new Label { Text = appt.TimeDisplay, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") }
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
            Grid.SetColumn(statusFrame, 1);
            cardGrid.Children.Add(statusFrame);

            stack.Children.Add(new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 16,
                Padding = new Thickness(16),
                HasShadow = true,
                BorderColor = Color.FromArgb("#E2E8F0"),
                Content = cardGrid
            });
        }

        Content = new ScrollView { Content = stack };
    }
}

public class PatientProfilePage : ContentPage
{
    public PatientProfilePage()
    {
        Title = "My Profile";
        BackgroundColor = Color.FromArgb("#F8FAFC");

        var logoutBtn = new Button
        {
            Text = "Logout",
            BackgroundColor = Color.FromArgb("#EF4444"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 50,
            Margin = new Thickness(20, 0)
        };
        logoutBtn.Clicked += (s, e) =>
        {
            Preferences.Clear();
            Application.Current!.MainPage = new NavigationPage(
                new LoginPage(ServiceHelper.GetService<ApiService>()));
        };

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 16,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "👤", FontSize = 60, HorizontalOptions = LayoutOptions.Center },
                new Label
                {
                    Text = Preferences.Get("FullName", "Patient"),
                    FontSize = 22, FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1E293B"),
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = "Patient", FontSize = 14,
                    TextColor = Color.FromArgb("#64748B"),
                    HorizontalOptions = LayoutOptions.Center
                },
                logoutBtn
            }
        };
    }
}

public class SpecialtySelectionPage : ContentPage
{
    private readonly ApiService _api;

    public SpecialtySelectionPage(ApiService api)
    {
        _api = api;
        Title = "Choose Specialty";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildUI();
    }

    private void BuildUI()
    {
        var specialties = new List<(string Emoji, string Name, Guid Id, string Color)>
        {
            ("❤️", "Cardiology", Guid.Parse("11111111-0000-0000-0000-000000000001"), "#FEE2E2"),
            ("🧴", "Dermatology", Guid.Parse("11111111-0000-0000-0000-000000000002"), "#FEF3C7"),
            ("🧠", "Neurology", Guid.Parse("11111111-0000-0000-0000-000000000003"), "#EDE9FE"),
            ("🦴", "Orthopedics", Guid.Parse("11111111-0000-0000-0000-000000000004"), "#DBEAFE"),
            ("👶", "Pediatrics", Guid.Parse("11111111-0000-0000-0000-000000000005"), "#D1FAE5"),
            ("🩺", "General", Guid.Parse("11111111-0000-0000-0000-000000000006"), "#CCFBF1"),
            ("👁️", "Ophthalmology", Guid.Parse("11111111-0000-0000-0000-000000000007"), "#FFEDD5"),
            ("🌸", "Gynecology", Guid.Parse("11111111-0000-0000-0000-000000000008"), "#FCE7F3"),
            ("🧘", "Psychiatry", Guid.Parse("11111111-0000-0000-0000-000000000009"), "#F1F5F9"),
            ("🔬", "Radiology", Guid.Parse("11111111-0000-0000-0000-000000000010"), "#F5F5F4"),
        };

        var outerStack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        outerStack.Children.Add(new Label
        {
            Text = "Select a Specialty",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E293B")
        });

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
            card.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    var page = new DoctorListPage(_api) { SpecialtyId = specialtyId };
                    await Navigation.PushAsync(page);
                })
            });
            Grid.SetRow(card, i / 2);
            Grid.SetColumn(card, i % 2);
            grid.Children.Add(card);
        }

        outerStack.Children.Add(grid);
        Content = new ScrollView { Content = outerStack };
    }
}

public class DoctorListPage : ContentPage
{
    private readonly ApiService _api;
    public Guid? SpecialtyId { get; set; }

    public DoctorListPage(ApiService api)
    {
        _api = api;
        Title = "Choose Doctor";
        BackgroundColor = Color.FromArgb("#F8FAFC");
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
            Color = Color.FromArgb("#2563EB"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 40)
        };
        Content = new VerticalStackLayout { Children = { loading } };

        var doctors = await _api.GetDoctorsAsync(SpecialtyId);
        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };

        foreach (var doc in doctors)
        {
            var rightStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End,
                Spacing = 4,
                Children =
                {
                    new Label { Text = doc.FeeDisplay, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B"), HorizontalOptions = LayoutOptions.End },
                    new Label { Text = "Book →", FontSize = 12, TextColor = Color.FromArgb("#2563EB"), FontAttributes = FontAttributes.Bold }
                }
            };

            var leftStack = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = doc.FullName, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                    new Label { Text = doc.SpecialtyName, FontSize = 12, TextColor = Color.FromArgb("#2563EB"), FontAttributes = FontAttributes.Bold },
                    new Label { Text = doc.ExperienceDisplay, FontSize = 12, TextColor = Color.FromArgb("#64748B") },
                    new Label { Text = doc.RatingDisplay, FontSize = 12, TextColor = Color.FromArgb("#F59E0B") }
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
            cardGrid.Children.Add(leftStack);
            Grid.SetColumn(rightStack, 1);
            cardGrid.Children.Add(rightStack);

            var card = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 16,
                Padding = new Thickness(16),
                HasShadow = true,
                BorderColor = Color.FromArgb("#E2E8F0"),
                Content = cardGrid
            };

            var docId = doc.Id;
            card.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    var page = new BookAppointmentPage(_api) { DoctorId = docId };
                    await Navigation.PushAsync(page);
                })
            });
            stack.Children.Add(card);
        }

        if (!doctors.Any())
            stack.Children.Add(new Label
            {
                Text = "No doctors found",
                FontSize = 16,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40)
            });

        Content = new ScrollView { Content = stack };
    }
}

public class BookAppointmentPage : ContentPage
{
    private readonly ApiService _api;
    public Guid DoctorId { get; set; }
    private TimeSlotDto? _selectedSlot;
    private DateTime _selectedDate = DateTime.Today;
    private Label? _selectedTimeLabel;
    private Button? _bookButton;
    private VerticalStackLayout? _slotsStack;
    private Editor? _reasonEntry;
    private Switch? _videoSwitch;

    public BookAppointmentPage(ApiService api)
    {
        _api = api;
        Title = "Book Appointment";
        BackgroundColor = Color.FromArgb("#F8FAFC");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildUI();
        _ = LoadSlots(_selectedDate);
    }

    private void BuildUI()
    {
        var scroll = new ScrollView();
        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 14 };

        // Date picker
        var datePicker = new DatePicker
        {
            MinimumDate = DateTime.Today,
            TextColor = Color.FromArgb("#1E293B"),
            BackgroundColor = Color.FromArgb("#F9FAFB")
        };
        datePicker.DateSelected += (s, e) =>
        {
            _selectedDate = e.NewDate;
            _ = LoadSlots(_selectedDate);
        };
        stack.Children.Add(new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = new Thickness(16),
            HasShadow = true,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = "📅 Select Date", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                    datePicker
                }
            }
        });

        // Slots
        _slotsStack = new VerticalStackLayout { Spacing = 8 };
        _selectedTimeLabel = new Label
        {
            Text = "",
            FontSize = 13,
            TextColor = Color.FromArgb("#2563EB"),
            FontAttributes = FontAttributes.Bold,
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center
        };
        stack.Children.Add(new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = new Thickness(16),
            HasShadow = true,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = "⏰ Available Slots", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                    _slotsStack,
                    _selectedTimeLabel
                }
            }
        });

        // Video switch
        _videoSwitch = new Switch { OnColor = Color.FromArgb("#2563EB") };
        var switchGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        switchGrid.Children.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "📹 Video Consultation", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                new Label { Text = "Remote video call", FontSize = 12, TextColor = Color.FromArgb("#64748B") }
            }
        });
        Grid.SetColumn(_videoSwitch, 1);
        switchGrid.Children.Add(_videoSwitch);
        stack.Children.Add(new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = new Thickness(16),
            HasShadow = true,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = switchGrid
        });

        // Reason
        _reasonEntry = new Editor
        {
            Placeholder = "Describe your symptoms...",
            HeightRequest = 90,
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            TextColor = Color.FromArgb("#1E293B"),
            FontSize = 14,
            AutoSize = EditorAutoSizeOption.TextChanges
        };
        stack.Children.Add(new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = new Thickness(16),
            HasShadow = true,
            BorderColor = Color.FromArgb("#E2E8F0"),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = "📝 Reason for Visit", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                    _reasonEntry
                }
            }
        });

        // Book button
        _bookButton = new Button
        {
            Text = "Confirm Appointment",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 52,
            IsEnabled = false
        };
        _bookButton.Clicked += BookButton_Clicked;
        stack.Children.Add(_bookButton);
        stack.Children.Add(new BoxView { HeightRequest = 40, Color = Colors.Transparent });

        scroll.Content = stack;
        Content = scroll;
    }

    private async Task LoadSlots(DateTime date)
    {
        if (_slotsStack == null) return;
        _slotsStack.Children.Clear();
        _slotsStack.Children.Add(new ActivityIndicator
        {
            IsRunning = true,
            Color = Color.FromArgb("#2563EB"),
            HorizontalOptions = LayoutOptions.Center
        });

        var slots = await _api.GetAvailableSlotsAsync(DoctorId, date);
        _slotsStack.Children.Clear();

        if (!slots.Any())
        {
            _slotsStack.Children.Add(new Label
            {
                Text = "No slots available for this day",
                TextColor = Color.FromArgb("#94A3B8"),
                FontSize = 13,
                HorizontalOptions = LayoutOptions.Center
            });
            return;
        }

        var slotsGrid = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        for (int i = 0; i < (slots.Count + 2) / 3; i++)
            slotsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var slotFrame = new Frame
            {
                BackgroundColor = slot.IsAvailable ? Color.FromArgb("#F0FDF4") : Color.FromArgb("#F9FAFB"),
                BorderColor = slot.IsAvailable ? Color.FromArgb("#86EFAC") : Color.FromArgb("#E2E8F0"),
                CornerRadius = 12,
                Padding = new Thickness(8, 10),
                HasShadow = false,
                Content = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = slot.DisplayTime, FontSize = 11,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = slot.IsAvailable ? Color.FromArgb("#166534") : Color.FromArgb("#9CA3AF"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = slot.IsAvailable ? "Free" : "Booked",
                            FontSize = 10,
                            TextColor = slot.IsAvailable ? Color.FromArgb("#166534") : Color.FromArgb("#9CA3AF"),
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                }
            };

            if (slot.IsAvailable)
            {
                var capturedSlot = slot;
                slotFrame.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        _selectedSlot = capturedSlot;
                        if (_bookButton != null) _bookButton.IsEnabled = true;
                        if (_selectedTimeLabel != null)
                        {
                            _selectedTimeLabel.Text = $"✅ Selected: {capturedSlot.DisplayTime}";
                            _selectedTimeLabel.IsVisible = true;
                        }
                    })
                });
            }

            Grid.SetRow(slotFrame, i / 3);
            Grid.SetColumn(slotFrame, i % 3);
            slotsGrid.Children.Add(slotFrame);
        }
        _slotsStack.Children.Add(slotsGrid);
    }

    private async void BookButton_Clicked(object sender, EventArgs e)
    {
        if (_selectedSlot == null) { await DisplayAlert("", "Please select a time slot", "OK"); return; }
        var reason = _reasonEntry?.Text?.Trim();
        if (string.IsNullOrEmpty(reason)) { await DisplayAlert("", "Please describe your reason", "OK"); return; }

        if (_bookButton != null) { _bookButton.IsEnabled = false; _bookButton.Text = "Booking..."; }
        try
        {
            var consultType = _videoSwitch?.IsToggled == true ? "Video" : "InPerson";
            var result = await _api.BookAppointmentAsync(DoctorId, _selectedDate, _selectedSlot.StartTime, reason, consultType);
            if (result.Success)
            {
                await DisplayAlert("✅ Booked!", "Your appointment has been confirmed!", "Great!");
                await Navigation.PopToRootAsync();
            }
            else
                await DisplayAlert("Failed", result.Message ?? "Could not book", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        finally
        {
            if (_bookButton != null) { _bookButton.IsEnabled = true; _bookButton.Text = "Confirm Appointment"; }
        }
    }
}

// ===== DOCTOR PAGES =====
public class DoctorDashboardPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _doctorId;

    public DoctorDashboardPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        Title = "Dashboard";
        BackgroundColor = Color.FromArgb("#F8FAFC");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboard();
    }

    private async Task LoadDashboard()
    {
        var loading = new ActivityIndicator
        {
            IsRunning = true,
            Color = Color.FromArgb("#2563EB"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        Content = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { loading } };

        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
        List<AppointmentDto> weekAppts = new();
        try { weekAppts = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, weekStart); } catch { }

        var todayAppts = weekAppts.Where(a => a.AppointmentDate.Date == today && a.Status != "Cancelled").ToList();

        var scroll = new ScrollView();
        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 16 };

        // Header
        var headerFrame = new Frame
        {
            CornerRadius = 20,
            Padding = new Thickness(20),
            HasShadow = false,
            BorderColor = Colors.Transparent
        };
        headerFrame.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#1D4ED8"), Offset = 0 },
                new GradientStop { Color = Color.FromArgb("#3B82F6"), Offset = 1 }
            }
        };
        headerFrame.Content = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "Good Morning 👋", FontSize = 14, TextColor = Color.FromArgb("#BFDBFE") },
                new Label { Text = Preferences.Get("FullName", "Doctor"), FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = today.ToString("dddd, MMM dd"), FontSize = 12, TextColor = Color.FromArgb("#93C5FD") }
            }
        };
        stack.Children.Add(headerFrame);

        // Stats
        var statsGrid = new Grid
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

        var s1 = CreateStatCard("📅", todayAppts.Count.ToString(), "Today", "#2563EB", "#1D4ED8");
        var s2 = CreateStatCard("🗓️", weekAppts.Count(a => a.Status != "Cancelled").ToString(), "This Week", "#059669", "#10B981");
        var s3 = CreateStatCard("⏳", weekAppts.Count(a => a.Status == "Scheduled").ToString(), "Pending", "#D97706", "#F59E0B");
        var s4 = CreateStatCard("✅", weekAppts.Count(a => a.Status == "Completed").ToString(), "Completed", "#7C3AED", "#8B5CF6");

        Grid.SetRow(s1, 0); Grid.SetColumn(s1, 0);
        Grid.SetRow(s2, 0); Grid.SetColumn(s2, 1);
        Grid.SetRow(s3, 1); Grid.SetColumn(s3, 0);
        Grid.SetRow(s4, 1); Grid.SetColumn(s4, 1);
        statsGrid.Children.Add(s1); statsGrid.Children.Add(s2);
        statsGrid.Children.Add(s3); statsGrid.Children.Add(s4);
        stack.Children.Add(statsGrid);

        // Buttons
        var btnsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        var calBtn = new Button
        {
            Text = "📅 Calendar",
            BackgroundColor = Color.FromArgb("#DBEAFE"),
            TextColor = Color.FromArgb("#1D4ED8"),
            CornerRadius = 10,
            HeightRequest = 44,
            FontAttributes = FontAttributes.Bold
        };
        calBtn.Clicked += async (s, e) =>
            await Navigation.PushAsync(new DoctorCalendarPage(_api));

        var logoutBtn = new Button
        {
            Text = "🚪 Logout",
            BackgroundColor = Color.FromArgb("#FEE2E2"),
            TextColor = Color.FromArgb("#DC2626"),
            CornerRadius = 10,
            HeightRequest = 44,
            FontAttributes = FontAttributes.Bold
        };
        logoutBtn.Clicked += (s, e) =>
        {
            Preferences.Clear();
            Application.Current!.MainPage = new NavigationPage(
                new LoginPage(ServiceHelper.GetService<ApiService>()));
        };
        Grid.SetColumn(logoutBtn, 1);
        btnsGrid.Children.Add(calBtn);
        btnsGrid.Children.Add(logoutBtn);
        stack.Children.Add(btnsGrid);

        // Today schedule
        stack.Children.Add(new Label
        {
            Text = "Today's Schedule",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E293B")
        });

        if (!todayAppts.Any())
            stack.Children.Add(new Label
            {
                Text = "No appointments today 🎉",
                FontSize = 14,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.Center
            });

        foreach (var appt in todayAppts.OrderBy(a => a.StartTime))
        {
            var timeStack = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = appt.StartTime.ToString(@"hh\:mm"), FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2563EB"), HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "30min", FontSize = 10, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center }
                }
            };

            var infoStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Spacing = 3,
                Children =
                {
                    new Label { Text = appt.PatientName ?? "", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") },
                    new Label { Text = appt.Reason, FontSize = 12, TextColor = Color.FromArgb("#64748B"), LineBreakMode = LineBreakMode.TailTruncation },
                    new Label { Text = appt.ConsultationType == "Video" ? "📹 Video" : "🏥 In Person", FontSize = 11, TextColor = Color.FromArgb("#2563EB") }
                }
            };

            var apptGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = 60 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12
            };
            apptGrid.Children.Add(timeStack);
            Grid.SetColumn(infoStack, 1);
            apptGrid.Children.Add(infoStack);

            stack.Children.Add(new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 14,
                Padding = new Thickness(16),
                HasShadow = true,
                BorderColor = Color.FromArgb("#E2E8F0"),
                Content = apptGrid
            });
        }

        stack.Children.Add(new BoxView { HeightRequest = 80, Color = Colors.Transparent });
        scroll.Content = stack;
        Content = scroll;
    }

    private Frame CreateStatCard(string emoji, string value, string label, string c1, string c2)
    {
        var frame = new Frame
        {
            CornerRadius = 20,
            Padding = new Thickness(16),
            HasShadow = true,
            BorderColor = Colors.Transparent
        };
        frame.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb(c1), Offset = 0 },
                new GradientStop { Color = Color.FromArgb(c2), Offset = 1 }
            }
        };
        frame.Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = emoji, FontSize = 28 },
                new Label { Text = value, FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = label, FontSize = 12, TextColor = Colors.White, Opacity = 0.85 }
            }
        };
        return frame;
    }
}

public class DoctorCalendarPage : ContentPage
{
    private readonly ApiService _api;
    private Guid _doctorId;
    private DateTime _currentWeekStart;
    private List<AppointmentDto> _weekAppointments = new();
    private Grid _calendarGrid = new();
    private Label _weekLabel = new();
    private ActivityIndicator _loading = new();

    private static readonly TimeSpan[] TimeSlots = Enumerable.Range(0, 14)
        .Select(i => TimeSpan.FromMinutes(9 * 60 + i * 30)).ToArray();

    public DoctorCalendarPage(ApiService api)
    {
        _api = api;
        _doctorId = Guid.Parse(Preferences.Get("ProfileId", Guid.Empty.ToString()));
        _currentWeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        Title = "Weekly Calendar";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        BuildLayout();
    }

    private void BuildLayout()
    {
        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // Header
        var prevBtn = new Button { Text = "◀", BackgroundColor = Color.FromArgb("#F1F5F9"), TextColor = Color.FromArgb("#1E293B"), CornerRadius = 8, WidthRequest = 44, HeightRequest = 44 };
        prevBtn.Clicked += async (s, e) => { _currentWeekStart = _currentWeekStart.AddDays(-7); await LoadWeek(); };

        var nextBtn = new Button { Text = "▶", BackgroundColor = Color.FromArgb("#F1F5F9"), TextColor = Color.FromArgb("#1E293B"), CornerRadius = 8, WidthRequest = 44, HeightRequest = 44 };
        nextBtn.Clicked += async (s, e) => { _currentWeekStart = _currentWeekStart.AddDays(7); await LoadWeek(); };

        var todayBtn = new Button { Text = "Today", BackgroundColor = Color.FromArgb("#2563EB"), TextColor = Colors.White, CornerRadius = 8, HeightRequest = 44, FontSize = 12 };
        todayBtn.Clicked += async (s, e) => { _currentWeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); await LoadWeek(); };

        _weekLabel = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        _loading = new ActivityIndicator { Color = Color.FromArgb("#2563EB"), IsRunning = false, HorizontalOptions = LayoutOptions.Center };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Padding = new Thickness(12, 8),
            BackgroundColor = Colors.White
        };
        headerGrid.Children.Add(prevBtn);
        Grid.SetColumn(_weekLabel, 1); headerGrid.Children.Add(_weekLabel);
        Grid.SetColumn(nextBtn, 2); headerGrid.Children.Add(nextBtn);
        Grid.SetColumn(todayBtn, 3); headerGrid.Children.Add(todayBtn);

        var headerStack = new VerticalStackLayout { Children = { headerGrid, _loading } };
        Grid.SetRow(headerStack, 0);
        mainGrid.Children.Add(headerStack);

        _calendarGrid = new Grid { Margin = new Thickness(8) };
        var calScroll = new ScrollView { Orientation = ScrollOrientation.Both, Content = _calendarGrid };
        Grid.SetRow(calScroll, 1);
        mainGrid.Children.Add(calScroll);

        Content = mainGrid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadWeek();
    }

    private async Task LoadWeek()
    {
        _loading.IsRunning = true;
        try
        {
            _weekAppointments = await _api.GetDoctorWeekAppointmentsAsync(_doctorId, _currentWeekStart);
            _weekLabel.Text = $"{_currentWeekStart:MMM dd} - {_currentWeekStart.AddDays(6):MMM dd}";
            BuildCalendarGrid();
        }
        catch { }
        finally { _loading.IsRunning = false; }
    }

    private void BuildCalendarGrid()
    {
        _calendarGrid.Children.Clear();
        _calendarGrid.RowDefinitions.Clear();
        _calendarGrid.ColumnDefinitions.Clear();

        _calendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 55 });
        for (int i = 0; i < 5; i++)
            _calendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 100 });

        _calendarGrid.RowDefinitions.Add(new RowDefinition { Height = 44 });
        foreach (var _ in TimeSlots)
            _calendarGrid.RowDefinitions.Add(new RowDefinition { Height = 64 });

        var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri" };
        for (int col = 0; col < 5; col++)
        {
            var date = _currentWeekStart.AddDays(col);
            var isToday = date.Date == DateTime.Today;
            var hdr = new Frame
            {
                BackgroundColor = isToday ? Color.FromArgb("#2563EB") : Color.FromArgb("#F1F5F9"),
                CornerRadius = 8,
                Padding = 4,
                Margin = 2,
                Content = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = days[col], FontSize = 11, TextColor = isToday ? Colors.White : Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = date.Day.ToString(), FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = isToday ? Colors.White : Color.FromArgb("#1E293B"), HorizontalOptions = LayoutOptions.Center }
                    }
                }
            };
            _calendarGrid.Children.Add(hdr);
            Grid.SetColumn(hdr, col + 1);
            Grid.SetRow(hdr, 0);
        }

        for (int row = 0; row < TimeSlots.Length; row++)
        {
            var time = TimeSlots[row];
            var tl = new Label { Text = $"{time:hh\\:mm}", FontSize = 10, TextColor = Color.FromArgb("#94A3B8"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            _calendarGrid.Children.Add(tl);
            Grid.SetColumn(tl, 0);
            Grid.SetRow(tl, row + 1);

            for (int col = 0; col < 5; col++)
            {
                var date = _currentWeekStart.AddDays(col);
                var appt = _weekAppointments.FirstOrDefault(a => a.AppointmentDate.Date == date.Date && a.StartTime == time);
                Frame cell;
                if (appt != null)
                {
                    var bg = appt.Status switch { "Scheduled" => "#DBEAFE", "Confirmed" => "#D1FAE5", "Completed" => "#EDE9FE", _ => "#F3F4F6" };
                    cell = new Frame
                    {
                        BackgroundColor = Color.FromArgb(bg),
                        CornerRadius = 8,
                        Padding = 6,
                        Margin = 2,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 2,
                            Children =
                            {
                                new Label { Text = appt.PatientName ?? "", FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B"), LineBreakMode = LineBreakMode.TailTruncation },
                                new Label { Text = appt.ConsultationType == "Video" ? "📹" : "🏥", FontSize = 10 }
                            }
                        }
                    };
                }
                else
                {
                    cell = new Frame { BackgroundColor = Color.FromArgb("#FAFAFA"), BorderColor = Color.FromArgb("#F1F5F9"), CornerRadius = 6, Padding = 4, Margin = 2 };
                }
                _calendarGrid.Children.Add(cell);
                Grid.SetColumn(cell, col + 1);
                Grid.SetRow(cell, row + 1);
            }
        }
    }
}

// ===== ADMIN PAGES =====
public class AdminDashboardPage : ContentPage
{
    private readonly ApiService _api;

    public AdminDashboardPage(ApiService api)
    {
        _api = api;
        Title = "Admin Dashboard";
        BackgroundColor = Color.FromArgb("#0F172A");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStats();
    }

    private async Task LoadStats()
    {
        var loading = new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#60A5FA"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Content = new VerticalStackLayout { BackgroundColor = Color.FromArgb("#0F172A"), VerticalOptions = LayoutOptions.Center, Children = { loading } };

        DashboardStatsDto stats;
        try { stats = await _api.GetAdminDashboardAsync(); }
        catch { stats = new DashboardStatsDto(); }

        var scroll = new ScrollView { BackgroundColor = Color.FromArgb("#0F172A") };
        var stack = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 16, BackgroundColor = Color.FromArgb("#0F172A") };

        // Header
        var logoutBtn = new Button { Text = "Logout", BackgroundColor = Color.FromArgb("#7F1D1D"), TextColor = Color.FromArgb("#FCA5A5"), CornerRadius = 10, HeightRequest = 40, FontSize = 13 };
        logoutBtn.Clicked += (s, e) => { Preferences.Clear(); Application.Current!.MainPage = new NavigationPage(new LoginPage(ServiceHelper.GetService<ApiService>())); };

        var hdrGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        hdrGrid.Children.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "⚕️ Admin Panel", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = "Platform Management", FontSize = 13, TextColor = Color.FromArgb("#475569") }
            }
        });
        Grid.SetColumn(logoutBtn, 1);
        hdrGrid.Children.Add(logoutBtn);
        stack.Children.Add(hdrGrid);

        // Stats
        var g1 = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        g1.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var ds1 = DarkStat("👨‍⚕️", stats.TotalDoctors.ToString(), "Doctors", "#1E3A5F", "#60A5FA");
        var ds2 = DarkStat("🧑", stats.TotalPatients.ToString(), "Patients", "#0D3D2F", "#34D399");
        var ds3 = DarkStat("👩‍⚕️", stats.TotalNurses.ToString(), "Nurses", "#2D1B4E", "#A78BFA");
        Grid.SetColumn(ds1, 0); Grid.SetColumn(ds2, 1); Grid.SetColumn(ds3, 2);
        g1.Children.Add(ds1); g1.Children.Add(ds2); g1.Children.Add(ds3);
        stack.Children.Add(g1);

        var g2 = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        g2.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var ds4 = DarkStat("📅", stats.TodayAppointments.ToString(), "Today", "#1E293B", "#F8FAFC");
        var ds5 = DarkStat("⏳", stats.PendingAppointments.ToString(), "Pending", "#1E293B", "#F59E0B");
        Grid.SetColumn(ds4, 0); Grid.SetColumn(ds5, 1);
        g2.Children.Add(ds4); g2.Children.Add(ds5);
        stack.Children.Add(g2);

        stack.Children.Add(new Label { Text = "Management", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });

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

        var addDocCard = AdminCard("👨‍⚕️", "Add Doctor", "#1E3A5F");
        addDocCard.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new CreateDoctorPage(_api))) });
        var addNurseCard = AdminCard("👩‍⚕️", "Add Nurse", "#0D3D2F");
        var usersCard = AdminCard("👥", "Manage Users", "#2D1B4E");
        usersCard.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PushAsync(new ManageUsersPage(_api))) });
        var apptsCard = AdminCard("🗓️", "Appointments", "#3D2000");

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

    private Frame DarkStat(string emoji, string value, string label, string bg, string textColor)
    {
        return new Frame
        {
            BackgroundColor = Color.FromArgb(bg),
            CornerRadius = 18,
            Padding = new Thickness(14, 12),
            HasShadow = false,
            BorderColor = Colors.Transparent,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = emoji, FontSize = 24 },
                    new Label { Text = value, FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(textColor) },
                    new Label { Text = label, FontSize = 11, TextColor = Color.FromArgb("#64748B") }
                }
            }
        };
    }

    private Frame AdminCard(string emoji, string title, string bg)
    {
        return new Frame
        {
            BackgroundColor = Color.FromArgb(bg),
            CornerRadius = 20,
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
                    new Label { Text = title, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }
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

    private async void CreateDoctor_Clicked(object sender, EventArgs e)
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
    public ChatContactsPage()
    {
        Title = "Messages";
        BackgroundColor = Color.FromArgb("#F8FAFC");
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 12,
            Children =
            {
                new Label { Text = "💬", FontSize = 60, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "Messages", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B"), HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "Coming soon", FontSize = 14, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center }
            }
        };
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
    public AdminAppointmentsPage()
    {
        Title = "All Appointments";
        Content = new Label { Text = "Appointments", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    }
}

public class CreateNursePage : ContentPage
{
    public CreateNursePage(ApiService api)
    {
        Title = "Add Nurse";
        Content = new Label { Text = "Create Nurse - Coming Soon", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    }
}