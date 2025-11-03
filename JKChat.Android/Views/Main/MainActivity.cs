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
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;

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

using MvvmCross;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Main {
	[Activity(
		Icon = "@mipmap/ic_launcher",
		Label = "@string/app_name",
		Theme = "@style/AppThemeSplashScreen",
		MainLauncher = true,
		LaunchMode = LaunchMode.SingleInstance,
		ConfigurationChanges = ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
		WindowSoftInputMode = SoftInput.StateAlwaysHidden | SoftInput.AdjustResize
	)]
	[MvxActivityPresentation]
	public class MainActivity() : BaseActivity<MainActivityViewModel>(Resource.Layout.activity_main) {
		private readonly Handler handler = new(Looper.MainLooper);
		private ActivityResultLauncher notificationsPermissionActivityResultLauncher;
		private MvxSubscriptionToken serverInfoMessageToken;
		private View contentMasterView, contentDetailView;
		private Intent pendingIntent;

		protected override void OnCreate(Bundle savedInstanceState) {
			var splashScreen = AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);
			base.OnCreate(savedInstanceState);
			EdgeToEdgeUtils.ApplyEdgeToEdge(this.Window, true);
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
			}
			serverInfoMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().SubscribeOnMainThread<ServerInfoMessage>(OnServerInfoMessage);

			contentMasterView = FindViewById(Resource.Id.content_master);
			contentDetailView = FindViewById(Resource.Id.content_detail);
			if (savedInstanceState == null) {
				pendingIntent = Intent;
			}

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
				})
			);
			CheckNotificationsPermission();
			UpdateWidgets();
		}

		protected override void OnDestroy() {
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
				serverInfoMessageToken = null;
			}
			base.OnDestroy();
		}

		protected override void OnNewIntent(Intent intent) {
			base.OnNewIntent(intent);
			pendingIntent = intent;
		}

		protected override void OnResume() {
			base.OnResume();
			System.Diagnostics.Debug.WriteLine($"flags: {pendingIntent?.Flags}");
			if (pendingIntent is { Flags: var flags, Extras: var extras, Action: var action } && !flags.HasFlag(ActivityFlags.LaunchedFromHistory)) {
				var navigationService = Mvx.IoCProvider.Resolve<INavigationService>();
				bool isEmpty = extras?.IsEmpty == true;
				if (action == NotificationsService.NotificationAction && !isEmpty) {
					var parameters = extras?.ToDictionary();
					navigationService.Navigate(parameters);
				} if (action == ForegroundGameClientsService.ForegroundAction) {
					var activeServers = Mvx.IoCProvider.Resolve<IGameClientsService>().ActiveServers.ToArray();
					if (activeServers.Length == 1) {
						var address = activeServers[0].Address;
						var parameters = navigationService.MakeNavigationParameters($"jkchat://chat?address={address}", address);
						navigationService.Navigate(parameters);
					}
				} else if (action == ServerMonitorAppWidget.WidgetLinkAction && !isEmpty
					&& extras.GetString(ServerMonitorAppWidget.ServerAddressExtraKey, null) is string address) {
					var widgetLink = AppSettings.WidgetLink;
					if (widgetLink != WidgetLink.Application) {
						var connected = Mvx.IoCProvider.Resolve<IGameClientsService>().GetStatus(address) == ConnectionStatus.Connected;
						string path = string.Empty;
						if (widgetLink == WidgetLink.Chat || (widgetLink == WidgetLink.ChatIfConnected && connected)) {
							path = "chat";
						} else if (widgetLink == WidgetLink.ServerInfo || widgetLink == WidgetLink.ChatIfConnected) {
							path = "info";
						}
						var parameters = navigationService.MakeNavigationParameters($"jkchat://{path}?address={address}", address);
						navigationService.Navigate(parameters);
					}
				}
			}
			pendingIntent = null;
		}

		protected override WindowInsetsCompat OnApplyWindowInsets(View view, WindowInsetsCompat windowInsets) {
			windowInsets = base.OnApplyWindowInsets(view, windowInsets);
			return windowInsets;
		}

		protected override void OnConfigurationChanged(Configuration configuration) {
			bool wasExpandedWindow = ExpandedWindow;
			base.OnConfigurationChanged(configuration);
#if false && DEBUG
			const float railCollapsedWidth = 128.0f, railExpandedWidth = 220.0f, masterWidth = 360.0f;
#else
			const float railCollapsedWidth = 128.0f, railExpandedWidth = 220.0f, masterCollapsedWidth = 320.0f, masterExpandedWidth = 480.0f;
#endif
			if (contentMasterView?.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters) {
				int width = LayoutState switch {
					LayoutState.Small => ViewGroup.LayoutParams.MatchParent,
					LayoutState.Medium => (railCollapsedWidth + masterCollapsedWidth).DpToPx(),
					LayoutState.Large => (railExpandedWidth + masterExpandedWidth).DpToPx()
				};
				marginLayoutParameters.Width = width;
				contentMasterView.LayoutParameters = marginLayoutParameters;
			}
			if (contentDetailView?.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters2) {
				int width = LayoutState switch {
					LayoutState.Small => 0.0f.DpToPx(),
					LayoutState.Medium => (railCollapsedWidth + masterCollapsedWidth).DpToPx(),
					LayoutState.Large => (railExpandedWidth + masterExpandedWidth).DpToPx()
				};
				marginLayoutParameters2.LeftMargin = width;
				contentDetailView.LayoutParameters = marginLayoutParameters2;
			}
		}

		public override void Exit(int order) {
			base.Exit(order);
			if (!ExpandedWindow) {
				if (order == 1) {
					var exitAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.fragment_push_exit);
					contentMasterView.StartAnimation(exitAnimation);
				} else if (order > 1) {
					handler.RemoveCallbacksAndMessages(null);
					contentMasterView.Alpha = 0.0f;
					handler.PostDelayed(() => {
						contentMasterView.Alpha = 1.0f;
					}, 400);
				}
			}
		}

		public override void PopEnter(int order) {
			base.PopEnter(order);
			if (!ExpandedWindow) {
				if (order == 1) {
					var popEnterAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.fragment_push_pop_enter);
					contentMasterView.StartAnimation(popEnterAnimation);
				} else if (order > 1) {
					handler.RemoveCallbacksAndMessages(null);
					contentMasterView.Alpha = 0.0f;
					handler.PostDelayed(() => {
						contentMasterView.Alpha = 1.0f;
					}, 400);
				}
			}
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			if (Mvx.IoCProvider.Resolve<IGameClientsService>().ActiveServers.Any()
				&& !IsServiceRunning(typeof(ForegroundGameClientsService))) {
				var intent = new Intent(this, typeof(ForegroundGameClientsService));
				ContextCompat.StartForegroundService(this, intent);
			}
		}

		private void UpdateWidgets() {
			var intent = new Intent(this, typeof(ServerMonitorAppWidget));
			intent.SetAction(ServerMonitorAppWidget.UpdateAction);
			SendBroadcast(intent);
		}

		private bool IsServiceRunning(Type serviceClass) {
			var manager = (ActivityManager)GetSystemService(Context.ActivityService);
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
			} else if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.PostNotifications)) {
				//don't bother
			} else {
				notificationsPermissionActivityResultLauncher.Launch(new Java.Lang.String(Manifest.Permission.PostNotifications));
			}
		}
	}

	public class MainActivityViewModel : MvxViewModel {}
}