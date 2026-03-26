namespace HospitalApp.Mobile;

public static class AppConfig
{
#if DEBUG
    public const string ApiBaseUrl = "http://192.168.11.163:5001/api/";
    public const string HubBaseUrl = "http://192.168.11.163:5001";
#else
    public const string ApiBaseUrl = "https://medicare-plus.railway.app/api/";
    public const string HubBaseUrl = "https://medicare-plus.railway.app";
#endif
}
