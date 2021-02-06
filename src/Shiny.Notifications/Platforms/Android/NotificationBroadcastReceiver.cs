﻿using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using RemoteInput = AndroidX.Core.App.RemoteInput;


namespace Shiny.Notifications
{
    [BroadcastReceiver(
        Name = ReceiverName,
        Enabled = true,
        Exported = false
    )]
    [IntentFilter(new[] {
        IntentAction
    })]
    public class NotificationBroadcastReceiver : ShinyBroadcastReceiver
    {
        public const string ReceiverName = "com.shiny.notifications." + nameof(NotificationBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            // TODO: alarm, get notificationId from intent, get notification from repo, fire notification without scheduledate
            var manager = ShinyHost.Resolve<INotificationManager>();
            var serializer = ShinyHost.Resolve<ISerializer>();

            var stringNotification = intent.GetStringExtra("Notification");
            var action = intent.GetStringExtra("Action");
            var notification = serializer.Deserialize<Notification>(stringNotification);
            var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");

            context.SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs));

            var response = new NotificationResponse(notification, action, text);
            await ShinyHost.Container.RunDelegates<INotificationDelegate>(x => x.OnEntry(response));
            await manager.Cancel(notification.Id);
        }
    }
}
