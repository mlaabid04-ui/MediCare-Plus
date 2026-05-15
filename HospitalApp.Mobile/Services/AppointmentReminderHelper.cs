namespace HospitalApp.Mobile.Services;

public static class AppointmentReminderHelper
{
    public static void ScheduleAndroidReminder(Guid? appointmentId, DateTime appointmentTime, string doctorName)
    {
#if ANDROID
        try
        {
            var reminderTime = appointmentTime.AddHours(-1);
            if (reminderTime <= DateTime.Now) return;

            var context = Android.App.Application.Context;

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var notifManager = (Android.App.NotificationManager?)context.GetSystemService(Android.Content.Context.NotificationService);
                var channel = new Android.App.NotificationChannel(
                    "appointment_reminders", "Rappels rendez-vous",
                    Android.App.NotificationImportance.High)
                {
                    Description = "Rappels pour vos rendez-vous médicaux"
                };
                notifManager?.CreateNotificationChannel(channel);
            }

            var intent = new Android.Content.Intent(context, typeof(HospitalApp.Mobile.Platforms.Android.ReminderBroadcastReceiver));
            intent.PutExtra("title", "📅 Rappel rendez-vous");
            intent.PutExtra("message", $"Votre rendez-vous avec {doctorName} est dans 1 heure");
            var notifId = appointmentId.HasValue ? (int)(appointmentId.Value.GetHashCode() & 0x7FFFFFFF) : 1;
            intent.PutExtra("notifId", notifId);

            var pendingIntent = Android.App.PendingIntent.GetBroadcast(
                context, notifId, intent,
                Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable);

            var alarmManager = (Android.App.AlarmManager?)context.GetSystemService(Android.Content.Context.AlarmService);
            var triggerAtMillis = new DateTimeOffset(reminderTime).ToUnixTimeMilliseconds();
            alarmManager?.SetExactAndAllowWhileIdle(Android.App.AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        catch { }
#endif
    }

    public static void CancelReminder(Guid appointmentId)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(context, typeof(HospitalApp.Mobile.Platforms.Android.ReminderBroadcastReceiver));
            var notifId = (int)(appointmentId.GetHashCode() & 0x7FFFFFFF);
            var pendingIntent = Android.App.PendingIntent.GetBroadcast(
                context, notifId, intent,
                Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable);
            var alarmManager = (Android.App.AlarmManager?)context.GetSystemService(Android.Content.Context.AlarmService);
            alarmManager?.Cancel(pendingIntent);
        }
        catch { }
#endif
    }
}
