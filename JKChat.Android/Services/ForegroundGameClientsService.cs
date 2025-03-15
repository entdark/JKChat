using System;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

using AndroidX.Core.App;
using AndroidX.Lifecycle;

using JKChat.Android.ValueConverters;
using JKChat.Android.Views.Main;
using JKChat.Android.Widgets;
using JKChat.Core.Messages;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross;
using MvvmCross.Plugin.Messenger;

using Microsoft.Maui.ApplicationModel;

namespace JKChat.Android.Services {
	[Service(Name = "com.vlbor.jkchat.ForegroundGameClientsService", Enabled = true, ForegroundServiceType = ForegroundService.TypeSpecialUse)]
	public class ForegroundGameClientsService : Service {
		private const string NotificationChannelID = "JKChat Foreground Service";
		private const int NotificationID = 2;

		public const string ForegroundAction = nameof(ForegroundGameClientsService)+nameof(ForegroundAction);

		private MvxSubscriptionToken serverInfoMessageToken;

		internal static bool IsRunning = false;

		public override void OnCreate() {
			IsRunning = true;
			base.OnCreate();
			CreateNotificationChannel();
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
			}
			serverInfoMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().SubscribeOnMainThread<ServerInfoMessage>(OnServerInfoMessage);
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
			UpdateWidgets(message);
		}

		private void HandleForeground(bool start) {
			FreeMemory();
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			var activeServers = gameClientsService.ActiveServers.ToArray();
			int unreadMessages = gameClientsService.UnreadMessages;
			if (activeServers.Length > 0/* || unreadMessages > 0*/) {
				var notification = CreateNotification(activeServers, unreadMessages);
				if (start) {
					ServiceCompat.StartForeground(this, NotificationID, notification, (int)ForegroundService.TypeSpecialUse);
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
			ServiceCompat.StopForeground(this, (int)StopForegroundFlags.Remove);
		}

		private void UpdateWidgets(ServerInfoMessage message) {
			var intent = new Intent(this, typeof(ServerMonitorAppWidget));
			intent.SetAction(ServerMonitorAppWidget.UpdateAction);
			intent.PutExtra(ServerMonitorAppWidget.ServerAddressExtraKey, message.Address);
			SendBroadcast(intent);
		}

		private Notification CreateNotification(ServerListItemVM[] servers, int messages) {
			var activityIntent = new Intent(this, typeof(MainActivity));
			activityIntent.SetAction(ForegroundGameClientsService.ForegroundAction);
			var activityPendingIntent = PendingIntent.GetActivity(this, 0, activityIntent, PendingIntentFlags.Immutable);
			var closeIntent = new Intent(this, typeof(ForegroundReceiver));
			var closePendingIntent = PendingIntent.GetBroadcast(this, 2, closeIntent, PendingIntentFlags.Immutable);
			string title = $"🌐 Connected to " + (servers.Length > 1 ? $"{servers.Length} servers" : servers[0].ServerName);
			var notification = new NotificationCompat.Builder(this, NotificationChannelID)
				.SetAutoCancel(false)
				.SetContentTitle(ColourTextValueConverter.Convert(title))
				.SetSmallIcon(Resource.Mipmap.ic_launcher)
				.SetContentIntent(activityPendingIntent)
				.SetPriority(NotificationCompat.PriorityLow)
				.SetOngoing(true)
				.AddAction(new NotificationCompat.Action(0, servers.Length > 1 ? "Disconnect from all" : "Disconnect", closePendingIntent));
			if (messages > 0 || servers.Length == 1	) {
				string message = servers.Length > 1 ? string.Empty : $"👤 {servers[0].Players}/{servers[0].MaxPlayers} players{(messages > 0 ? "\n" : string.Empty)}";
				if (messages > 0) {
					message += $"💬 {messages} unread message" + (messages > 1 ? "s" : string.Empty);
				}
				notification.SetContentText(message);
			}
			return notification.Build();
		}

		private void CreateNotificationChannel() {
			var builder = new NotificationChannelCompat.Builder(NotificationChannelID, NotificationManagerCompat.ImportanceLow)
				.SetName(NotificationChannelID)
				.SetShowBadge(false);
			var channel = builder.Build();
			var notificationManager = NotificationManagerCompat.From(this);
			notificationManager.CreateNotificationChannel(channel);
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