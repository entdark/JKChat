using System;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

using AndroidX.AppCompat.App;
using AndroidX.Core.App;

using JKChat.Android.Services;
using JKChat.Android.ValueConverters;
using JKChat.Android.Views.Main;
using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;
using JKChat.Core.ViewModels.ServerList.Items;

using Microsoft.Maui.ApplicationModel;

using MvvmCross;

namespace JKChat.Android.Widgets {
	[BroadcastReceiver(Label = "Server monitor", Exported = false)]
	[IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
	[MetaData("android.appwidget.provider", Resource = "@xml/server_monitor_widget_provider")]
	public class ServerMonitorAppWidget : AppWidgetProvider {
		private const string Prefix = nameof(ServerMonitorAppWidget);
		public const string WidgetLinkAction = Prefix+nameof(WidgetLinkAction);
		public const string RefreshAction = Prefix+nameof(RefreshAction);
		public const string PlayersAction = Prefix+nameof(PlayersAction);
		public const string UpdateAction = Prefix+nameof(UpdateAction);
		public const string AddAction = Prefix+nameof(AddAction);
		public const string ServerAddressExtraKey = Prefix+nameof(ServerAddressExtraKey);
		public const string WidgetIdExtraKey = Prefix+nameof(WidgetIdExtraKey);
		public const string FirstRefreshExtraKey = Prefix+nameof(FirstRefreshExtraKey);
		public const string PlayersExtraKey = Prefix+nameof(PlayersExtraKey);

		private readonly TasksQueue tasksQueue = new();

		public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int []appWidgetIds) {
			UpdateMultiple(context, appWidgetManager, appWidgetIds, true);
		}

		public override void OnReceive(Context context, Intent intent) {
			base.OnReceive(context, intent);
			var appWidgetManager = AppWidgetManager.GetInstance(context);
			var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(ServerMonitorAppWidget)));
			if (intent.Action == UpdateAction) {
				var serversAddresses = AppSettings.ServerMonitorServers;
				int []appWidgetIds = null;
				bool refresh;
				if (intent.Extras?.IsEmpty == false && intent.Extras.GetString(ServerAddressExtraKey, null) is string serverAddress) {
					appWidgetIds = serversAddresses.Where(kvp => kvp.Value == serverAddress).Select(kvp => kvp.Key).ToArray();
					refresh = false;
				} else {
					appWidgetIds = appWidgetManager.GetAppWidgetIds(componentName);
					//remove saved servers associated with widgets if OnDeleted failed to get called and do the job
					serversAddresses = serversAddresses.Where(kvp => appWidgetIds.Contains(kvp.Key)).ToDictionary();
					AppSettings.ServerMonitorServers = serversAddresses;
					refresh = true;
				}
				UpdateMultiple(context, appWidgetManager, appWidgetIds, refresh);
			} else if (intent is { Action: RefreshAction, Extras: { IsEmpty: false } extras }
				&& extras.GetString(ServerAddressExtraKey, null) is string serverAddress
				&& extras.GetInt(WidgetIdExtraKey, -1) is int appWidgetId && appWidgetId != -1) {
				bool firstRefresh = extras.GetBoolean(FirstRefreshExtraKey, false);
				Update(context, appWidgetManager, appWidgetId, serverAddress, true, firstRefresh: firstRefresh);
			}
		}

		public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions) {
			base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
			var serverAddresses = AppSettings.ServerMonitorServers;
			if (serverAddresses.TryGetValue(appWidgetId, out string serverAddress)) {
				tasksQueue.Enqueue(async () => {
					var server = await ServerListItemVM.FindExistingOrLoad(serverAddress, true, false);
					if (server != null) {
						await MainThread.InvokeOnMainThreadAsync(() => {
							SetView(context, appWidgetManager, appWidgetId, newOptions, server);
						});
					}
				});
			}
		}

		public override void OnDeleted(Context context, int []appWidgetIds) {
			var serverAddresses = AppSettings.ServerMonitorServers;
			foreach (var appWidgetId in appWidgetIds) {
				serverAddresses.Remove(appWidgetId);
			}
			AppSettings.ServerMonitorServers = serverAddresses;
			base.OnDeleted(context, appWidgetIds);
		}

		private void UpdateMultiple(Context context, AppWidgetManager appWidgetManager, int []appWidgetIds, bool refresh) {
			var serverAddresses = AppSettings.ServerMonitorServers;
			foreach (var appWidgetId in appWidgetIds) {
				if (serverAddresses.TryGetValue(appWidgetId, out string serverAddress)) {
					Update(context, appWidgetManager, appWidgetId, serverAddress, false, refresh);
				} else {
					UpdateEmpty(context, appWidgetManager, appWidgetId);
				}
			}
		}

		private void Update(Context context, AppWidgetManager appWidgetManager, int appWidgetId, string serverAddress, bool showLoading, bool refresh = true, bool firstRefresh = false) {
			tasksQueue.Enqueue(async () => {
				await setLoading(true);
				var server = await ServerListItemVM.FindExistingOrLoad(serverAddress, true, preferGameClient: !refresh);
				await Task.Delay(500);
				if (server != null) {
					if (refresh) {
						await server.Refresh();
					}
					await setView(server);
				} else {
					await setLoading(false);
				}
			});

			async Task setView(ServerListItemVM server) {
				await MainThread.InvokeOnMainThreadAsync(() => {
					var options = appWidgetManager.GetAppWidgetOptions(appWidgetId) ?? new();
					SetView(context, appWidgetManager, appWidgetId, options, server);
				});
			}
			async Task setLoading(bool loading) {
				if (!showLoading)
					return;
				await MainThread.InvokeOnMainThreadAsync(() => {
					var views = new RemoteViews(context.PackageName, Resource.Layout.server_monitor_widget);
					if (loading && firstRefresh) {
						views.SetTextViewText(Resource.Id.refresh_button, string.Empty);
						views.SetTextViewText(Resource.Id.players_button, string.Empty);
						views.SetTextViewText(Resource.Id.players_textview, string.Empty);
						views.SetTextViewText(Resource.Id.server_name_textview, string.Empty);
						views.SetTextViewText(Resource.Id.map_textview, string.Empty);
						views.SetTextViewText(Resource.Id.datetime_textview, string.Empty);
					}
					views.SetViewVisibility(Resource.Id.loading_progressbar, loading ? ViewStates.Visible : ViewStates.Gone);
					appWidgetManager.UpdateAppWidget(appWidgetId, views);
				});
			}
		}

		private static void SetView(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions, ServerListItemVM server, bool resetLoading = true) {
			string serverAddress = server.Address;

			var views = new RemoteViews(context.PackageName, Resource.Layout.server_monitor_widget);

			var serverInfoIntent = new Intent(context, typeof(MainActivity));
			serverInfoIntent.SetAction(WidgetLinkAction);
			serverInfoIntent.PutExtra(ServerAddressExtraKey, serverAddress);
			var serverInfoPendingIntent = PendingIntentCompat.GetActivity(context, appWidgetId, serverInfoIntent, (int)PendingIntentFlags.UpdateCurrent, false);
			views.SetOnClickPendingIntent(Resource.Id.widget_layout, serverInfoPendingIntent);

			var refreshIntent = new Intent(context, typeof(ServerMonitorAppWidget));
			refreshIntent.SetAction(RefreshAction);
			refreshIntent.PutExtra(ServerAddressExtraKey, serverAddress);
			refreshIntent.PutExtra(WidgetIdExtraKey, appWidgetId);
			var refreshPendingIntent = PendingIntentCompat.GetBroadcast(context, appWidgetId, refreshIntent, (int)PendingIntentFlags.UpdateCurrent, false);
			views.SetOnClickPendingIntent(Resource.Id.refresh_button, refreshPendingIntent);
			views.SetTextViewText(Resource.Id.refresh_button, "Refresh");

			var playersIntent = new Intent(context, typeof(ServerMonitorDialogActivity));
			playersIntent.SetAction(PlayersAction);
			playersIntent.PutExtra(PlayersExtraKey, server.PlayersList ?? []);
			var playersPendingIntent = PendingIntentCompat.GetActivity(context, appWidgetId, playersIntent, (int)PendingIntentFlags.UpdateCurrent, false);
			views.SetOnClickPendingIntent(Resource.Id.players_button, playersPendingIntent);
			views.SetTextViewText(Resource.Id.players_button, "Players");
			
			int minHeight = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMinHeight),
				maxHeight = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMaxHeight),
				minWidth = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMinWidth),
				maxWidth = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMaxWidth);
			bool bigView = maxHeight >= 120;
			string players = $"{server.Players}/{server.MaxPlayers}";
			if (bigView) {
				views.SetTextViewText(Resource.Id.players_textview, $"{players} players");
				views.SetViewVisibility(Resource.Id.map_textview, ViewStates.Visible);
			} else {
				views.SetTextViewText(Resource.Id.players_textview, $"{players}, {server.MapName}");
				views.SetViewVisibility(Resource.Id.map_textview, ViewStates.Gone);
			}
			views.SetTextViewText(Resource.Id.server_name_textview, ColourTextValueConverter.Convert(server.ServerName));
			views.SetTextViewText(Resource.Id.map_textview, server.MapName);
			views.SetTextViewText(Resource.Id.datetime_textview, $"{DateTime.Now:g}");
			//to be safe
			if (resetLoading) {
				views.SetViewVisibility(Resource.Id.loading_progressbar, ViewStates.Gone);
			}
			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}

		private static void UpdateEmpty(Context context, AppWidgetManager appWidgetManager, int appWidgetId) {
			var views = new RemoteViews(context.PackageName, Resource.Layout.server_monitor_empty_widget);

			var addIntent = new Intent(context, typeof(ServerMonitorDialogActivity));
			addIntent.SetAction(AddAction);
			addIntent.PutExtra(WidgetIdExtraKey, appWidgetId);
			var addPendingIntent = PendingIntentCompat.GetActivity(context, appWidgetId, addIntent, (int)PendingIntentFlags.UpdateCurrent, false);
			views.SetOnClickPendingIntent(Resource.Id.add_textview, addPendingIntent);

			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}
	}

	[Activity(
		Theme = "@style/AppThemeMaterial3.Translucent",
		LaunchMode = LaunchMode.SingleInstance,
		TaskAffinity = "."+nameof(ServerMonitorDialogActivity),
		ExcludeFromRecents = true,
		ConfigurationChanges = ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
		WindowSoftInputMode = SoftInput.StateAlwaysHidden | SoftInput.AdjustResize
	)]
	public class ServerMonitorDialogActivity : AppCompatActivity {
		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			if (Intent is { Action: ServerMonitorAppWidget.PlayersAction, Extras.IsEmpty: false } && Intent.Extras.GetStringArray(ServerMonitorAppWidget.PlayersExtraKey) is string []players) {
				if (players.Length > 0) {
					DialogService.Show(new(new DialogListViewModel(players.Select(p => new DialogItemVM() { Name = p }), DialogSelectionType.NoSelection), _ => {
						Finish();
					}, "Players"));
				} else {
					DialogService.Show(new() {
						Title = "Server is Empty",
						OkText = "OK",
						OkAction = _ => {
							Finish();
						}
					});
				}
			} else if (Intent is { Action: ServerMonitorAppWidget.AddAction, Extras.IsEmpty: false } && Intent.Extras.GetInt(ServerMonitorAppWidget.WidgetIdExtraKey, -1) is int appWidgetId && appWidgetId != -1) {
				Task.Run(add);
				async Task add() {
					var servers = (await Mvx.IoCProvider.Resolve<ICacheService>().LoadFavouriteServers()).ToArray();
					await MainThread.InvokeOnMainThreadAsync(() => {
						if (!servers.IsNullOrEmpty()) {
							DialogService.Show(new() {
								Title = "Add Server",
								List = new DialogListViewModel(servers.Select(s => new DialogItemVM() { Name = s.ServerName, Id = s.Address }), DialogSelectionType.InstantSelection),
								OkAction = config => {
									string serverAddress = config.List.SelectedItem?.Id as string;
									var serversAddresses = AppSettings.ServerMonitorServers;
									serversAddresses[appWidgetId] = serverAddress;
									AppSettings.ServerMonitorServers = serversAddresses;
									var intent = new Intent(this, typeof(ServerMonitorAppWidget));
									intent.SetAction(ServerMonitorAppWidget.RefreshAction);
									intent.PutExtra(ServerMonitorAppWidget.ServerAddressExtraKey, serverAddress);
									intent.PutExtra(ServerMonitorAppWidget.WidgetIdExtraKey, appWidgetId);
									intent.PutExtra(ServerMonitorAppWidget.FirstRefreshExtraKey, true);
									SendBroadcast(intent);
									Finish();
								},
								CancelText = "Cancel",
								CancelAction = _ => {
									Finish();
								}
							});
						} else {
							DialogService.Show(new() {
								Title = "Favourites is Empty",
								OkText = "OK",
								OkAction = _ => {
									Finish();
								}
							});
						}
					});
				}
			} else {
				Finish();
			}
		}
	}
}

