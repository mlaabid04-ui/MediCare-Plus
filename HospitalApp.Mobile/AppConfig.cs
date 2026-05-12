namespace HospitalApp.Mobile;

public static class AppConfig
{
    // ── Build-time defaults ──────────────────────────────────────
#if PHONE_DEBUG
    private const string _defaultBase = "http://192.168.1.111:5001";
#elif DEBUG
    private const string _defaultBase = "http://10.0.2.2:5001";
#else
    private const string _defaultBase = "https://medicare-plus.railway.app";
#endif

    // ── Runtime-overridable URLs (debug only; Release always uses Railway) ──
    public static string ServerBase
    {
#if DEBUG
        get => Preferences.Get("ServerBase", _defaultBase).TrimEnd('/');
        set => Preferences.Set("ServerBase", value.TrimEnd('/'));
#else
        get => _defaultBase;
        set { }
#endif
    }

    public static string ApiBaseUrl => ServerBase + "/api/";
    public static string HubBaseUrl => ServerBase;

    // ── Flavor role guard ────────────────────────────────────────
#if DOCTOR_APP
    public const string AllowedRole = "Doctor";
#elif PATIENT_APP
    public const string AllowedRole = "Patient";
#else
    public const string AllowedRole = "";
#endif
}
