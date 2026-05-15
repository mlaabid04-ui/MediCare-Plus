using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace HospitalApp.Mobile.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderBroadcastReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        var title = intent.GetStringExtra("title") ?? "Rappel rendez-vous";
        var message = intent.GetStringExtra("message") ?? "Vous avez un rendez-vous bientôt";
        var notifId = intent.GetIntExtra("notifId", 1);

        var builder = new NotificationCompat.Builder(context, "appointment_reminders")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message));

        var notifManager = NotificationManagerCompat.From(context);
        notifManager.Notify(notifId, builder.Build());
    }
}
