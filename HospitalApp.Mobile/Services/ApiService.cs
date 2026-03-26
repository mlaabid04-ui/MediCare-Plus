using HospitalApp.Mobile.Models;
using System.Text;
using System.Text.Json;

namespace HospitalApp.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(AppConfig.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    private void SetAuthHeader()
    {
        var token = Preferences.Get("Token", "");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        var refreshToken = Preferences.Get("RefreshToken", "");
        if (string.IsNullOrEmpty(refreshToken)) return false;

        try
        {
            var json = JsonSerializer.Serialize(new { refreshToken }, _jsonOpts);
            var response = await _http.PostAsync("Auth/refresh",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode) return false;

            var text = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthResult>(text, _jsonOpts);
            if (result?.Success == true && result.Token != null)
            {
                Preferences.Set("Token", result.Token);
                SetAuthHeader();
                return true;
            }
        }
        catch { }
        return false;
    }

    private async Task<T?> PostAsync<T>(string url, object body)
    {
        try
        {
            SetAuthHeader();
            var json = JsonSerializer.Serialize(body, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"POST {url} → {response.StatusCode}: {responseText}");

            if (string.IsNullOrWhiteSpace(responseText)) return default;
            return JsonSerializer.Deserialize<T>(responseText, _jsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PostAsync error [{url}]: {ex.Message}");
            throw;
        }
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            SetAuthHeader();
            var response = await _http.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                    response = await _http.GetAsync(url);
                else
                    return default;
            }

            var responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"GET {url} → {response.StatusCode}: {responseText}");

            if (string.IsNullOrWhiteSpace(responseText)) return default;
            return JsonSerializer.Deserialize<T>(responseText, _jsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetAsync error [{url}]: {ex.Message}");
            throw;
        }
    }

    private async Task<T?> PutAsync<T>(string url, object? body = null)
    {
        try
        {
            SetAuthHeader();
            HttpContent? content = null;
            if (body != null)
                content = new StringContent(
                    JsonSerializer.Serialize(body, _jsonOpts), Encoding.UTF8, "application/json");

            var response = await _http.PutAsync(url, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                    response = await _http.PutAsync(url, content);
                else
                    return default;
            }

            var text = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"PUT {url} → {response.StatusCode}: {text}");

            if (string.IsNullOrEmpty(text)) return default;
            return JsonSerializer.Deserialize<T>(text, _jsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PutAsync error [{url}]: {ex.Message}");
            throw;
        }
    }

    // ===== AUTH =====
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var result = await PostAsync<AuthResult>("Auth/login",
                new { email = email.Trim(), password });

            if (result?.Success == true)
            {
                if (result.Token != null) Preferences.Set("Token", result.Token);
                if (result.RefreshToken != null) Preferences.Set("RefreshToken", result.RefreshToken);
            }

            return result ?? new AuthResult { Success = false, Message = "Empty response from server" };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, Message = $"Connection error: {ex.Message}" };
        }
    }

    public async Task<AuthResult> RegisterPatientAsync(RegisterPatientRequest request)
    {
        try
        {
            var result = await PostAsync<AuthResult>("Auth/register", request);

            if (result?.Success == true)
            {
                if (result.Token != null) Preferences.Set("Token", result.Token);
                if (result.RefreshToken != null) Preferences.Set("RefreshToken", result.RefreshToken);
            }

            return result ?? new AuthResult { Success = false, Message = "Empty response from server" };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, Message = $"Connection error: {ex.Message}" };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = Preferences.Get("RefreshToken", "");
            if (!string.IsNullOrEmpty(refreshToken))
                await PostAsync<object>("Auth/logout", new { refreshToken });
        }
        catch { }
        finally
        {
            Preferences.Remove("Token");
            Preferences.Remove("RefreshToken");
        }
    }

    // ===== DOCTORS =====
    public async Task<List<DoctorListDto>> GetDoctorsAsync(Guid? specialtyId = null)
    {
        try
        {
            var url = specialtyId.HasValue ? $"doctors?specialtyId={specialtyId}" : "doctors";
            return await GetAsync<List<DoctorListDto>>(url) ?? new();
        }
        catch { return new(); }
    }

    public async Task<DoctorDetailDto?> GetDoctorByIdAsync(Guid id)
    {
        try { return await GetAsync<DoctorDetailDto>($"doctors/{id}"); }
        catch { return null; }
    }

    public async Task<List<TimeSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
    {
        try
        {
            return await GetAsync<List<TimeSlotDto>>(
                $"appointments/slots/{doctorId}?date={date:yyyy-MM-dd}") ?? new();
        }
        catch { return new(); }
    }

    // ===== APPOINTMENTS =====
    public async Task<AppointmentResult> BookAppointmentAsync(
        Guid doctorId, DateTime date, TimeSpan startTime,
        string reason, string consultationType)
    {
        try
        {
            var result = await PostAsync<AppointmentResult>("appointments/book", new
            {
                doctorId,
                appointmentDate = date.ToString("yyyy-MM-dd"),
                startTime = startTime.ToString(@"hh\:mm\:ss"),
                reason,
                consultationType
            });
            return result ?? new AppointmentResult { Success = false };
        }
        catch (Exception ex)
        {
            return new AppointmentResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<AppointmentDto>> GetPatientAppointmentsAsync(Guid patientId)
    {
        try { return await GetAsync<List<AppointmentDto>>($"appointments/patient/{patientId}") ?? new(); }
        catch { return new(); }
    }

    public async Task<List<AppointmentDto>> GetDoctorWeekAppointmentsAsync(Guid doctorId, DateTime weekStart)
    {
        try
        {
            return await GetAsync<List<AppointmentDto>>(
                $"appointments/doctor/{doctorId}/week?weekStart={weekStart:yyyy-MM-dd}") ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> CancelAppointmentAsync(Guid appointmentId)
    {
        try
        {
            SetAuthHeader();
            var response = await _http.PutAsync($"appointments/{appointmentId}/cancel", null);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ===== PATIENTS =====
    public async Task<PatientDetailDto?> GetPatientAsync(Guid patientId)
    {
        try { return await GetAsync<PatientDetailDto>($"patients/{patientId}"); }
        catch { return null; }
    }

    // ===== CHAT =====
    public async Task<List<ChatMessageDto>> GetChatMessagesAsync(Guid otherUserId)
    {
        try { return await GetAsync<List<ChatMessageDto>>($"chat/{otherUserId}") ?? new(); }
        catch { return new(); }
    }

    public async Task<List<ChatContactDto>> GetChatContactsAsync()
    {
        try { return await GetAsync<List<ChatContactDto>>("chat/contacts") ?? new(); }
        catch { return new(); }
    }

    // ===== NOTIFICATIONS =====
    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        try { return await GetAsync<List<NotificationDto>>("notifications") ?? new(); }
        catch { return new(); }
    }

    public async Task MarkNotificationReadAsync(Guid id)
    {
        try { await PutAsync<object>($"notifications/{id}/read"); }
        catch { }
    }

    public async Task MarkAllNotificationsReadAsync()
    {
        try { await PutAsync<object>("notifications/read-all"); }
        catch { }
    }

    // ===== ADMIN =====
    public async Task<DashboardStatsDto> GetAdminDashboardAsync()
    {
        try { return await GetAsync<DashboardStatsDto>("admin/dashboard") ?? new(); }
        catch { return new(); }
    }

    public async Task<AdminResult> CreateDoctorAsync(CreateDoctorRequest request)
    {
        try
        {
            var result = await PostAsync<AdminResult>("admin/doctors", request);
            return result ?? new AdminResult { Success = false };
        }
        catch (Exception ex) { return new AdminResult { Success = false, Message = ex.Message }; }
    }

    public async Task<AdminResult> CreateNurseAsync(CreateNurseRequest request)
    {
        try
        {
            var result = await PostAsync<AdminResult>("admin/nurses", request);
            return result ?? new AdminResult { Success = false };
        }
        catch (Exception ex) { return new AdminResult { Success = false, Message = ex.Message }; }
    }

    public async Task<List<UserListDto>> GetUsersAsync(string? role = null)
    {
        try
        {
            var url = role != null ? $"admin/users?role={role}" : "admin/users";
            return await GetAsync<List<UserListDto>>(url) ?? new();
        }
        catch { return new(); }
    }

    public async Task ToggleUserAsync(Guid userId)
    {
        try
        {
            SetAuthHeader();
            await _http.PutAsync($"admin/users/{userId}/toggle", null);
        }
        catch { }
    }

    public async Task<List<AppointmentDto>> GetAllAppointmentsAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var url = "admin/appointments";
            if (from.HasValue) url += $"?from={from:yyyy-MM-dd}";
            if (to.HasValue) url += (from.HasValue ? "&" : "?") + $"to={to:yyyy-MM-dd}";
            return await GetAsync<List<AppointmentDto>>(url) ?? new();
        }
        catch { return new(); }
    }
}
