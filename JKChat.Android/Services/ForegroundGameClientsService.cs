using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using AndroidX.Core.App;
using AndroidX.Lifecycle;

using JKChat.Android.Views.Main;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Services;

using MvvmCross;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Android.Services {
	[Service(Enabled = true)]
	public class ForegroundGameClientsService : Service {
		private const string NotificationChannelID = "JKChat Foreground Service";
		private const int NotificationID = 1337;

		private MvxSubscriptionToken serverInfoMessageToken;

		public override void OnCreate() {
			base.OnCreate();
			CreateNotificationChannel();
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
			}
			serverInfoMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().Subscribe<ServerInfoMessage>(OnServerInfoMessage);
		}

		public override void OnDestroy() {
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
				serverInfoMessageToken = null;
			}
			base.OnDestroy();
		}

		public override IBinder OnBind(Intent intent) {
			return new Binder();
		}

		public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) {
			var notification = CreateNotification(1, 0);
			StartForeground(NotificationID, notification);
			return StartCommandResult.Sticky;
		}

		public void Stop() {
			StopForeground(true);
			StopSelf();
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			FreeMemory();
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			int clientsCount = gameClientsService.ActiveClients;
			int unreadMessages = gameClientsService.UnreadMessages;
			if (clientsCount > 0/* || unreadMessages > 0*/) {
				var notification = CreateNotification(clientsCount, unreadMessages);
				var notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
				notificationManager.Notify(NotificationID, notification);
			} else {
				StopForeground(true);
				StopSelf();
			}
		}

		private Notification CreateNotification(int count, int messages) {
			var activityIntent = new Intent(this, typeof(MainActivity));
			var pendingIntent = PendingIntent.GetActivity(this, 0, activityIntent, 0);
			var closeIntent = new Intent(this, typeof(ForegroundReceiver));
			closeIntent.SetAction("Test");
			var closePendingIntent = PendingIntent.GetBroadcast(this, 2, closeIntent, 0);
			var notification = new NotificationCompat.Builder(this, NotificationChannelID)
				.SetAutoCancel(false)
				.SetContentTitle($"You are connected to {count} server" + (count > 1 ? "s" : string.Empty))
				.SetSmallIcon(Resource.Mipmap.ic_launcher)
				.SetContentIntent(pendingIntent)
				.SetPriority(NotificationCompat.PriorityLow)
				.AddAction(new NotificationCompat.Action(0, count > 1 ? "Disconnect from all" : "Disconnect", closePendingIntent));
			if (messages > 0) {
				notification.SetContentText($"You have {messages} unread message" + (messages > 1 ? "s" : string.Empty));
			}
			return notification.Build();
		}

		private void CreateNotificationChannel() {
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
				var channel = new NotificationChannel(NotificationChannelID, NotificationChannelID, NotificationImportance.Low);
				var notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
				notificationManager.CreateNotificationChannel(channel);
			}
		}

		private static void FreeMemory() {
			if (Xamarin.Essentials.Platform.CurrentActivity is MainActivity mainActivity && mainActivity.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Resumed)) {
				return;
			}
			GC.Collect();
		}
	}

	[BroadcastReceiver]
	public class ForegroundReceiver : BroadcastReceiver {
		public override void OnReceive(Context context, Intent intent) {
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			gameClientsService.DisconnectFromAll();
		}
	}
}