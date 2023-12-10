using System.Collections.Generic;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;

using AndroidX.Core.App;
using AndroidX.Core.Content;

using Google.Android.Material.Color;

using JKChat.Android.Views.Main;
using JKChat.Core;
using JKChat.Core.Services;

namespace JKChat.Android.Services {
	public class NotificationsService : INotificationsService {
		private const string NotificationChannelID = "JKChat Chat";
		public const string NotificationAction = nameof(NotificationsService) +nameof(NotificationAction);

		private int notificationId = 1337;
		private readonly Dictionary<string, HashSet<int>> shownNotifications = new();

		public NotificationsService() {
			CreateNotificationChannel();
		}

		public void CancelNotifications(string tag = null) {
			var context = global::Android.App.Application.Context;
			var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
			bool hasIdsSet = shownNotifications.TryGetValue(tag, out var idsSet) && idsSet != null;
			if (string.IsNullOrEmpty(tag)) {
				notificationManager.CancelAll();
				shownNotifications.Clear();
			} else if (Build.VERSION.SdkInt >= BuildVersionCodes.M) {
				var notifications = notificationManager.GetActiveNotifications();
				if (notifications.IsNullOrEmpty())
					return;
				foreach (var notification in notifications) {
					if (tag == notification.Tag) {
						notificationManager.Cancel(tag, notification.Id);
					}
				}
				idsSet?.Clear();
			} else if (hasIdsSet) {
				foreach (int id in idsSet) {
					notificationManager.Cancel(tag, id);
				}
				idsSet.Clear();
				shownNotifications.Remove(tag);
			}
		}

		public void ShowNotification(string title, string message, IDictionary<string, string> data, string tag = null) {
			var context = global::Android.App.Application.Context;
			var activityIntent = new Intent(context, typeof(MainActivity));
			activityIntent.SetAction(NotificationAction);
			foreach (var kv in data) {
				activityIntent.PutExtra(kv.Key, kv.Value);
			}
			int id = notificationId++;
			var pendingIntent = PendingIntent.GetActivity(context, id, activityIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
			var notification = new NotificationCompat.Builder(context, NotificationChannelID)
				.SetContentTitle(title)
				.SetContentText(message)
				.SetSmallIcon(Resource.Mipmap.ic_launcher)
				.SetContentIntent(pendingIntent)
				.SetPriority(NotificationCompat.PriorityHigh)
				.SetLights(MaterialColors.GetColor(context, Resource.Attribute.colorPrimaryFixed, Color.Blue), 2000, 2000)
				.SetDefaults(NotificationCompat.DefaultAll)
				.SetSilent(false)
				.SetAutoCancel(true);
			var notificationManager = NotificationManagerCompat.From(context);
			tag ??= string.Empty;
			notificationManager.Notify(tag, id, notification.Build());
			if (!shownNotifications.TryGetValue(tag, out var idsSet) || idsSet == null) {
				shownNotifications[tag] = idsSet = new();
			}
			idsSet.Add(id);
		}

		public bool NotificationsEnabled => ContextCompat.CheckSelfPermission(global::Android.App.Application.Context, Manifest.Permission.PostNotifications) == Permission.Granted;

		private void CreateNotificationChannel() {
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
				var context = global::Android.App.Application.Context;
				var channel = new NotificationChannel(NotificationChannelID, NotificationChannelID, NotificationImportance.Max) {
					LightColor = MaterialColors.GetColor(context, Resource.Attribute.colorPrimaryFixed, Color.Blue)
				};
				channel.EnableLights(true);
				channel.EnableVibration(true);
				channel.SetShowBadge(true);
				var notificationManager = NotificationManagerCompat.From(context);
				notificationManager.CreateNotificationChannel(channel);
			}
		}
	}
}