using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;

using AndroidX.Core.App;
using AndroidX.Lifecycle;

using JKChat.Android.Views.Main;
using JKChat.Core.Messages;
using JKChat.Core.Services;

using MvvmCross;
using MvvmCross.Plugin.Messenger;

using Microsoft.Maui.ApplicationModel;
using Android.Content.PM;

namespace JKChat.Android.Services {
	[Service(Enabled = true, ForegroundServiceType = ForegroundService.TypeNone)]
	public class ForegroundGameClientsService : Service {
		private const string NotificationChannelID = "JKChat Foreground Service";
		private const int NotificationID = 2;

		private MvxSubscriptionToken serverInfoMessageToken;

		internal static bool IsRunning = false;

		public override void OnCreate() {
			IsRunning = true;
			base.OnCreate();
			CreateNotificationChannel();
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
			}
			serverInfoMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			HandleForeground(true);
		}

		public override void OnDestroy() {
			IsRunning = false;
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
			HandleForeground(true);
			return StartCommandResult.Sticky;
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			HandleForeground(false);
		}

		private void HandleForeground(bool start) {
			FreeMemory();
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			int clientsCount = gameClientsService.ActiveClients;
			int unreadMessages = gameClientsService.UnreadMessages;
			if (clientsCount > 0/* || unreadMessages > 0*/) {
				var notification = CreateNotification(clientsCount, unreadMessages);
				if (start) {
					StartForeground(NotificationID, notification);
				} else {
					var notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
					notificationManager.Notify(NotificationID, notification);
				}
			} else {
				StopForeground();
				StopSelf();
			}
		}

		private void StopForeground() {
			if (Build.VERSION.SdkInt >= BuildVersionCodes.N) {
				StopForeground(StopForegroundFlags.Remove);
			} else {
				StopForeground(true);
			}
		}

		private Notification CreateNotification(int count, int messages) {
			var activityIntent = new Intent(this, typeof(MainActivity));
			var pendingIntent = PendingIntent.GetActivity(this, 0, activityIntent, PendingIntentFlags.Immutable);
			var closeIntent = new Intent(this, typeof(ForegroundReceiver));
			closeIntent.SetAction("Test");
			var closePendingIntent = PendingIntent.GetBroadcast(this, 2, closeIntent, PendingIntentFlags.Immutable);
			var notification = new NotificationCompat.Builder(this, NotificationChannelID)
				.SetAutoCancel(false)
				.SetContentTitle($"You are connected to {count} server" + (count > 1 ? "s" : string.Empty))
				.SetSmallIcon(Resource.Mipmap.ic_launcher)
				.SetContentIntent(pendingIntent)
				.SetPriority(NotificationCompat.PriorityLow)
				.SetOngoing(true)
				.AddAction(new NotificationCompat.Action(0, count > 1 ? "Disconnect from all" : "Disconnect", closePendingIntent));
			if (messages > 0) {
				notification.SetContentText($"You have {messages} unread message" + (messages > 1 ? "s" : string.Empty));
			}
			return notification.Build();
		}

		private void CreateNotificationChannel() {
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
				var channel = new NotificationChannel(NotificationChannelID, NotificationChannelID, NotificationImportance.Low);
				channel.SetShowBadge(false);
				var notificationManager = NotificationManagerCompat.From(this);
				notificationManager.CreateNotificationChannel(channel);
			}
		}

		private static void FreeMemory() {
			if (Platform.CurrentActivity is MainActivity mainActivity && mainActivity.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Resumed)) {
				return;
			}
			GC.Collect();
		}
	}

	[BroadcastReceiver]
	public class ForegroundReceiver : BroadcastReceiver {
		public ForegroundReceiver() {
		}
		public ForegroundReceiver(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}
		public override void OnReceive(Context context, Intent intent) {
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			gameClientsService.DisconnectFromAll();
		}
	}
}