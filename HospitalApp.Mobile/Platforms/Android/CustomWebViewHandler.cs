using Android.Webkit;
using Microsoft.Maui.Handlers;

namespace HospitalApp.Mobile.Platforms.Android;

public class CustomWebViewHandler : WebViewHandler
{
    protected override global::Android.Webkit.WebView CreatePlatformView()
    {
        var webView = base.CreatePlatformView();
        webView.Settings.JavaScriptEnabled = true;
        webView.Settings.MediaPlaybackRequiresUserGesture = false;
        webView.Settings.AllowFileAccess = true;
        webView.Settings.DomStorageEnabled = true;
        webView.SetWebChromeClient(new PermissionWebChromeClient());
        return webView;
    }
}

public class PermissionWebChromeClient : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest? request)
    {
        // Grant camera, microphone, and audio capture for Jitsi Meet
        request?.Grant(request.GetResources());
    }
}
