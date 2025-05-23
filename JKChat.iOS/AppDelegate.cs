﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using CoreLocation;

using Foundation;

using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.iOS.Helpers;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;

using MvvmCross;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Plugin.Messenger;

using UIKit;

using UserNotifications;

namespace JKChat.iOS {
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : MvxApplicationDelegate<Setup, App>, IUNUserNotificationCenterDelegate {
		public const string BackgroundNotificationRequestId = "JKChatNotificationBackground";
		public const string ServerInfoNotificationRequestId = "JKChatNotificationServerInfo";

		private CLLocationManager locationManager;
		private NSUrl delayedOpenUrl;
		private int lastActiveCount, lastMessages;
		private MvxSubscriptionToken serverInfoMessageToken, locationUpdateMessageToken, widgetFavouritesMessageToken;
		private IDisposable liveActivityDisconnectObserver = null;

		private CLAuthorizationStatus AuthorizationStatus {
			get {
				return UIDevice.CurrentDevice.CheckSystemVersion(14, 0) && locationManager != null ?
					locationManager.AuthorizationStatus : CLLocationManager.Status;
			}
		}

		// class-level declarations

		public override UIWindow Window {
			get;
			set;
		}

		private bool isActive;
		public bool IsActive {
			get => isActive;
			set {
				isActive = value;
				if (isActive && delayedOpenUrl != null) {
					OpenUrl(delayedOpenUrl);
					delayedOpenUrl = null;
				}
			}
		}

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) {
#if !__MACCATALYST__
			AppCenter.Start(Core.ApiKeys.AppCenter.iOS, typeof(Crashes));
#endif
			bool finishedLaunching = base.FinishedLaunching(application, null);
			Window.TintColor = Theme.Color.Accent;
			Mvx.IoCProvider.Resolve<IAppService>().AppTheme = AppSettings.AppTheme;

			UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound/* | UNAuthorizationOptions.Provisional*/, (granted, error) => {
				Debug.WriteLine("UserNotifications granted: " + granted);
				var options = AppSettings.NotificationOptions;
				if (granted)
					options |= NotificationOptions.Enabled;
				else
					options &= ~NotificationOptions.Enabled;
				AppSettings.NotificationOptions = options;
			});
			UNUserNotificationCenter.Current.Delegate = this;
			InitLocationManager();
			RequestLocationAuthorization(locationManager, this.AuthorizationStatus);
			IsActive = true;
			var messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
			serverInfoMessageToken = messenger.SubscribeOnMainThread<ServerInfoMessage>(OnServerInfoMessage);
			locationUpdateMessageToken = messenger.Subscribe<LocationUpdateMessage>(OnLocationUpdateMessage);
			widgetFavouritesMessageToken = messenger.Subscribe<WidgetFavouritesMessage>(OnWidgetFavouritesMessage);
			NSNotificationCenter.DefaultCenter.AddObserver(new NSString("NSWindowDidBecomeMainNotification"), (notification) => {
				base.WillEnterForeground(application);
				IsActive = true;
			});
			NSNotificationCenter.DefaultCenter.AddObserver(new NSString("NSWindowDidResignMainNotification"), (notification) => {
				IsActive = false;
				base.DidEnterBackground(application);
			});
			SetCommonFavourites();
			OpenUrl(delayedOpenUrl);
			delayedOpenUrl = null;
			var userDefaults = new NSUserDefaults("group.com.vlbor.JKChat", NSUserDefaultsType.SuiteName);
			userDefaults.SetBool(false, "LiveActivityDisconnect");
			//dirty way, but the observer doesn't work for some reason
			Task.Run(async () => {
				var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
				while (true) {
					await Task.Delay(1000);
					bool active = gameClientsService.ActiveServers.Any();
					if (active) {
						var value = userDefaults.ValueForKey(new("LiveActivityDisconnect"));
						if (value is NSNumber { BoolValue: true }) {
							gameClientsService.DisconnectFromAll();
							userDefaults.SetBool(false, "LiveActivityDisconnect");
						}
					}
				}
			});
/*			var liveActivityDisconnectObserver = userDefaults.AddObserver("LiveActivityDisconnect", NSKeyValueObservingOptions.OldNew, change => {
				Debug.WriteLine("w00t");
				if (change.NewValue is NSNumber { BoolValue: true }) {
					Mvx.IoCProvider.Resolve<IGameClientsService>().DisconnectFromAll();
					userDefaults.SetBool(false, "LiveActivityDisconnect");
				}
			});*/
			return finishedLaunching;
		}

		public override void OnResignActivation(UIApplication application) {
			IsActive = false;
			// Invoked when the application is about to move from active to inactive state.
			// This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
			// or when the user quits the application and it begins the transition to the background state.
			// Games should use this method to pause the game.
		}

		public override void DidEnterBackground(UIApplication application) {
			ExecuteOnBackground();
			StartLocationUpdate();
			CreateLiveActivity();
			base.DidEnterBackground(application);
			// Use this method to release shared resources, save user data, invalidate timers and store the application state.
			// If your application supports background execution this method is called instead of WillTerminate when the user quits.
		}

		public override void WillEnterForeground(UIApplication application) {
			base.WillEnterForeground(application);
			IsActive = true;
			StopLocationUpdate();
			CreateLiveActivity();
			// Called as part of the transition from background to active state.
			// Here you can undo many of the changes made on entering the background.
		}

		public override void OnActivated(UIApplication application) {
			IsActive = true;
			// Restart any tasks that were paused (or not yet started) while the application was inactive. 
			// If the application was previously in the background, optionally refresh the user interface.
		}

		public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options) {
			OpenUrl(url);
			return true;
		}

		private void OpenUrl(NSUrl url) {
			if (!IsActive) {
				delayedOpenUrl = url;
				return;
			}
			if (url == null || url.Scheme != "jkchat")
				return;
			var navigationService = Mvx.IoCProvider.Resolve<INavigationService>();
			if (url.Host == "liveactivity") {
				var activeServers = Mvx.IoCProvider.Resolve<IGameClientsService>().ActiveServers.ToArray();
				if (activeServers.Length == 1) {
					var address = activeServers[0].Address;
					var parameters = navigationService.MakeNavigationParameters($"jkchat://chat?address={address}", address);
					navigationService.Navigate(parameters);
				}
				return;
			}
			var widgetLink = AppSettings.WidgetLink;
			if (widgetLink == WidgetLink.Application)
				return;
			if (url.Host != "widget")
				return;
			var queryItems = new NSUrlComponents(url, true).QueryItems;
			if (queryItems.IsNullOrEmpty() || (queryItems.FirstOrDefault(item => item.Name == "address") is not { Value: {} address2 }) || address2.Length == 0)
				return;
			var connected = Mvx.IoCProvider.Resolve<IGameClientsService>().GetStatus(address2) == ConnectionStatus.Connected;
			string path = string.Empty;
			if (widgetLink == WidgetLink.Chat || (widgetLink == WidgetLink.ChatIfConnected && connected)) {
				path = "chat";
			} else if (widgetLink == WidgetLink.ServerInfo || widgetLink == WidgetLink.ChatIfConnected) {
				path = "info";
			}
			var parameters2 = navigationService.MakeNavigationParameters($"jkchat://{path}?address={address2}", address2);
			navigationService.Navigate(parameters2);
		}

		[Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
		public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler) {
			var options = UNNotificationPresentationOptions.None;
			if (notification.Request.Identifier == BackgroundNotificationRequestId || notification.Request.Identifier == ServerInfoNotificationRequestId) {
				if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0)) {
					options = UNNotificationPresentationOptions.List;
				}
			} else {
				options = UNNotificationPresentationOptions.Sound | UNNotificationPresentationOptions.Badge;
				if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0)) {
					options |= UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Banner;
				} else {
					options |= UNNotificationPresentationOptions.Alert;
				}
			}
			completionHandler(options);
		}

		[Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
		public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler) {
			var userInfo = response.Notification.Request?.Content?.UserInfo;
			var parameters = userInfo?.ToDictionary();
			Mvx.IoCProvider.Resolve<INavigationService>().Navigate(parameters);
		}

		public override void WillTerminate(UIApplication application) {
			if (locationManager != null) {
				if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0)) {
					locationManager.DidChangeAuthorization -= LocationManagerDidChangeAuthorization;
				} else {
					locationManager.AuthorizationChanged -= LocationManagerAuthorizationChanged;
				}
				locationManager = null;
			}
			var messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
			if (serverInfoMessageToken != null) {
				messenger.Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
				serverInfoMessageToken = null;
			}
			if (locationUpdateMessageToken != null) {
				messenger.Unsubscribe<LocationUpdateMessage>(locationUpdateMessageToken);
				locationUpdateMessageToken = null;
			}
			if (widgetFavouritesMessageToken != null) {
				messenger.Unsubscribe<WidgetFavouritesMessage>(widgetFavouritesMessageToken);
				widgetFavouritesMessageToken = null;
			}
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			gameClientsService.ShutdownAll();
			WidgetShared.LiveActivityShared.StopLiveActivity(() => {});
			IsActive = false;
			liveActivityDisconnectObserver?.Dispose();
			base.WillTerminate(application);
			// Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
		}

		private void ExecuteOnBackground() {
			if (this.AuthorizationStatus == CLAuthorizationStatus.AuthorizedAlways
				|| this.AuthorizationStatus == CLAuthorizationStatus.AuthorizedWhenInUse) {
				return;
			}
			if (DeviceInfo.IsRunningOnMacOS) {
				return;
			}
			if (!Mvx.IoCProvider.Resolve<IGameClientsService>().ActiveServers.Any()) {
				return;
			}
			var taskID = UIApplication.SharedApplication.BeginBackgroundTask(() => {
			});
			Task.Run(async () => {
				await Task.Delay(1337);
				while (true) {
					int time = (int)UIApplication.SharedApplication.BackgroundTimeRemaining;
					Debug.WriteLine(time);

					if ((time % 25) == 0 || time == 10) {
						ShowNotification(time);
					} else if (time-1 <= 0) {
						break;
					}
					//must be longer than a second to not display the same notification twice
					await Task.Delay(1002);
				}
				InvokeOnMainThread(() => {
					UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
				});
				UIApplication.SharedApplication.EndBackgroundTask(taskID);
			});
		}

		private void ShowNotification(int time) {
			InvokeOnMainThread(() => {
				var content = new UNMutableNotificationContent() {
					Title = "JKChat is minimized",
//					Subtitle = "Notification Subtitle",
					Body = $"You have {time} seconds until it pauses the connection"
				};
				var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(double.Epsilon, false);
				var request = UNNotificationRequest.FromIdentifier(BackgroundNotificationRequestId, content, trigger);
				UNUserNotificationCenter.Current.AddNotificationRequest(request, (error) => {
					if (error != null) {
						Debug.WriteLine(error);
					}
				});
			});
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			FreeMemory();
			CreateLiveActivity();
		}

		private DateTime nextWidgetsUpdateTime = default;
		private void CreateLiveActivity() {
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			var activeServers = gameClientsService.ActiveServers.ToArray();
			InvokeOnMainThread(() => {
				if (activeServers.Length > 0) {
					var now = DateTime.Now;
					if (nextWidgetsUpdateTime < now) {
						WidgetShared.LiveActivityShared.RefreshWidgetsOfKind(null);
						nextWidgetsUpdateTime = now.AddSeconds(30.0);
					}
					WidgetShared.LiveActivityShared.ShowLiveActivityWithServers(
						activeServers.Select(s => s.ServerName).ToArray(),
						(uint)gameClientsService.UnreadMessages,
						(uint)activeServers[0].Players,
						(uint)activeServers[0].MaxPlayers,
						gameClientsService.DisconnectFromAll, () => {}
					);
				} else {
					WidgetShared.LiveActivityShared.StopLiveActivity(() => {});
				}
			});
		}

		private void OnWidgetFavouritesMessage(WidgetFavouritesMessage message) {
			SetCommonFavourites();
		}

		private readonly TasksQueue favouritesQueue = new();
		private void SetCommonFavourites() {
			favouritesQueue.Enqueue(add);
			static async Task add() {
				var servers = await Mvx.IoCProvider.Resolve<ICacheService>().LoadFavouriteServers();
				var jsonString = servers.Select(s => new {
					address = s.Address.Split(':')[0],
					port = ushort.TryParse(s.Address.Split(':')[1], out ushort port) ? port : 0,
					serverName = s.CleanServerName
				}).ToArray().Serialize();
				var userDefaults = new NSUserDefaults("group.com.vlbor.JKChat", NSUserDefaultsType.SuiteName);
				userDefaults.SetString(jsonString, "FavouritesServers");
				Debug.WriteLine(userDefaults.ValueForKey(new("FavouritesServers")));
			}
		}

		private void OnLocationUpdateMessage(LocationUpdateMessage message) {
			InitLocationManager();
			if (AppSettings.LocationUpdate) {
				RequestLocationAuthorization(locationManager, this.AuthorizationStatus);
			} else {
				StopLocationUpdate();
			}
		}

		private void InitLocationManager() {
			if (AppSettings.LocationUpdate) {
				if (locationManager == null) {
					locationManager = new CLLocationManager() {
						DesiredAccuracy = CLLocation.AccurracyBestForNavigation,
						DistanceFilter = CLLocationDistance.FilterNone,
						PausesLocationUpdatesAutomatically = false,
						AllowsBackgroundLocationUpdates = true
					};
					if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0)) {
						locationManager.DidChangeAuthorization += LocationManagerDidChangeAuthorization;
					} else {
						locationManager.AuthorizationChanged += LocationManagerAuthorizationChanged;
					}
				}
			}
		}

		private void StartLocationUpdate() {
			if (!AppSettings.LocationUpdate) {
				return;
			}
			if (DeviceInfo.IsRunningOnMacOS) {
				return;
			}
			RequestLocationAuthorization(locationManager, this.AuthorizationStatus);
			if (Mvx.IoCProvider.Resolve<IGameClientsService>().ActiveServers.Any()) {
				locationManager?.StartUpdatingLocation();
			}
		}

		private void LocationManagerDidChangeAuthorization(object sender, EventArgs ev) {
			if (sender is CLLocationManager locationManager) {
				RequestLocationAuthorization(locationManager, this.AuthorizationStatus);
			}
		}

		private void LocationManagerAuthorizationChanged(object sender, CLAuthorizationChangedEventArgs ev) {
			if (sender is CLLocationManager locationManager) {
				RequestLocationAuthorization(locationManager, ev.Status);
			}
		}
		private static void RequestLocationAuthorization(CLLocationManager locationManager, CLAuthorizationStatus status) {
			if (DeviceInfo.IsRunningOnMacOS) {
				return;
			}
			if (status == CLAuthorizationStatus.Authorized
				|| status == CLAuthorizationStatus.AuthorizedWhenInUse) {
				locationManager?.RequestAlwaysAuthorization();
			} else if (status == CLAuthorizationStatus.NotDetermined
				|| status != CLAuthorizationStatus.AuthorizedAlways) {
				locationManager?.RequestWhenInUseAuthorization();
			}
		}

		private void StopLocationUpdate() {
			locationManager?.StopUpdatingLocation();
		}

		private void FreeMemory() {
			if (IsActive) {
				return;
			}
			GC.Collect();
		}
	}
}