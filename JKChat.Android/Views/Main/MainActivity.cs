using System;
using System.Linq;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Views.Animations;

using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.Content;

using Google.Android.Material.Internal;

using JKChat.Android.Callbacks;
using JKChat.Android.Helpers;
using JKChat.Android.Services;
using JKChat.Android.Views.Base;
using JKChat.Android.Widgets;
using JKChat.Core;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Navigation;
using JKChat.Core.Services;

using Microsoft.Maui.ApplicationModel;

using MvvmCross;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Main {
	[Activity(
		Label = "@string/app_name",
		Theme = "@style/AppThemeMaterial3",
		MainLauncher = true,
		LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
		WindowSoftInputMode = SoftInput.StateAlwaysHidden | SoftInput.AdjustResize
	)]
	[MvxActivityPresentation]
	public class MainActivity : BaseActivity<MainActivityViewModel> {
		private ActivityResultLauncher notificationsPermissionActivityResultLauncher;
		private MvxSubscriptionToken serverInfoMessageToken;
		private View contentMasterView, contentDetailView;
		private Intent pendingIntent;

		public MainActivity() : base(Resource.Layout.activity_main) {}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			EdgeToEdgeUtils.ApplyEdgeToEdge(this.Window, true);
			Platform.Init(this, savedInstanceState);
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
			}
			serverInfoMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().Subscribe<ServerInfoMessage>(OnServerInfoMessage);

			contentMasterView = FindViewById(Resource.Id.content_master);
			contentDetailView = FindViewById(Resource.Id.content_detail);
			pendingIntent = Intent;

			notificationsPermissionActivityResultLauncher = RegisterForActivityResult(
				new ActivityResultContracts.RequestPermission(),
				new ActivityResultCallback<Java.Lang.Boolean>(jgranted => {
					bool granted = jgranted.BooleanValue();
					var options = AppSettings.NotificationOptions;
					if (granted)
						options |= NotificationOptions.Enabled;
					else
						options &= ~NotificationOptions.Enabled;
					AppSettings.NotificationOptions = options;
				}
			));
			CheckNotificationsPermission();
			var intent = new Intent(this, typeof(ServerMonitorAppWidget));
			intent.SetAction(ServerMonitorAppWidget.UpdateAction);
			SendBroadcast(intent);
		}

		protected override void OnDestroy() {
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
				serverInfoMessageToken = null;
			}
/*			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			gameClientsService.ShutdownAll();*/
			base.OnDestroy();
		}

		protected override void OnNewIntent(Intent intent) {
			base.OnNewIntent(intent);
			pendingIntent = intent;
		}

		protected override void OnResume() {
			base.OnResume();
			if (pendingIntent is { Extras: { IsEmpty: false } extras, Action: {} action }) {
				var navigationService = Mvx.IoCProvider.Resolve<INavigationService>();
				if (action == NotificationsService.NotificationAction) {
					var parameters = extras?.ToDictionary();
					navigationService.Navigate(parameters);
				} else if (action == ServerMonitorAppWidget.WidgetLinkAction
					&& extras.GetString(ServerMonitorAppWidget.ServerAddressExtraKey, null) is string serverAddress) {
					var widgetLink = AppSettings.WidgetLink;
					if (widgetLink != WidgetLink.Application) {
						string path = widgetLink switch {
							WidgetLink.ServerInfo => "info",
							WidgetLink.Chat => "chat",
							_ => string.Empty
						};
						var parameters = navigationService.MakeNavigationParameters($"jkchat://{path}?address={serverAddress}", serverAddress);
						navigationService.Navigate(parameters);
					}
				}
				pendingIntent = null;
			}
		}

		protected override void ConfigurationChanged(Configuration configuration) {
			base.ConfigurationChanged(configuration);
			const float primaryWidth = 480.0f;
			if (contentMasterView != null && contentMasterView.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters) {
				marginLayoutParameters.Width = ExpandedWindow ? primaryWidth.DpToPx() : ViewGroup.LayoutParams.MatchParent;
				contentMasterView.LayoutParameters = marginLayoutParameters;
			}
			if (contentDetailView != null && contentDetailView.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters2) {
				marginLayoutParameters2.LeftMargin = ExpandedWindow ? primaryWidth.DpToPx() : 0.0f.DpToPx();
				contentDetailView.LayoutParameters = marginLayoutParameters2;
			}
		}

		public override void Exit() {
			base.Exit();
			if (!ExpandedWindow) {
				var exitAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.fragment_hslide_rtl);
				contentMasterView.StartAnimation(exitAnimation);
			}
		}

		public override void PopEnter() {
			base.PopEnter();
			if (!ExpandedWindow) {
				var popEnterAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.fragment_hslide_ltr);
				contentMasterView.StartAnimation(popEnterAnimation);
			}
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			int activeClients = gameClientsService.ActiveClients;
			if (activeClients > 0 && !IsServiceRunning(typeof(ForegroundGameClientsService))) {
				var intent = new Intent(this, typeof(ForegroundGameClientsService));
				ContextCompat.StartForegroundService(this, intent);
			}
		}
		private bool IsServiceRunning(Type serviceClass) {
			ActivityManager manager = (ActivityManager)GetSystemService(Context.ActivityService);
			foreach (var service in manager.GetRunningServices(int.MaxValue)) {
				string className = service.Service.ClassName;
				if (className.Equals(serviceClass.Name, StringComparison.OrdinalIgnoreCase)
					|| (className.Contains(serviceClass.Name, StringComparison.OrdinalIgnoreCase)/* && className.Contains("md5")*/)) {
					return true;
				}
			}
			return ForegroundGameClientsService.IsRunning;
		}

		private void CheckNotificationsPermission() {
			if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
				return;
			var permission = ContextCompat.CheckSelfPermission(this, Manifest.Permission.PostNotifications);
			if (permission == Permission.Granted) {
				//we are good
			} else if (ShouldShowRequestPermissionRationale(Manifest.Permission.PostNotifications)) {
				//don't bother
			} else {
				notificationsPermissionActivityResultLauncher.Launch(new Java.Lang.String(Manifest.Permission.PostNotifications));
			}
		}
	}

	public class MainActivityViewModel : MvxViewModel {}
}