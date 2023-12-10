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

using JKChat.Android.Services;
using JKChat.Android.ValueConverters;
using JKChat.Android.Views.Main;
using JKChat.Core;
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
		public const string WidgetLinkAction = nameof(ServerMonitorAppWidget)+nameof(WidgetLinkAction);
		public const string RefreshAction = nameof(ServerMonitorAppWidget)+nameof(RefreshAction);
		public const string PlayersAction = nameof(ServerMonitorAppWidget)+nameof(PlayersAction);
		public const string UpdateAction = nameof(ServerMonitorAppWidget)+nameof(UpdateAction);
		public const string AddAction = nameof(ServerMonitorAppWidget)+nameof(AddAction);
		public const string ServerAddressExtraKey = nameof(ServerMonitorAppWidget)+nameof(ServerAddressExtraKey);
		public const string WidgetIdExtraKey = nameof(ServerMonitorAppWidget)+nameof(WidgetIdExtraKey);
		public const string PlayersExtraKey = nameof(ServerMonitorAppWidget)+nameof(PlayersExtraKey);

		public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int []appWidgetIds) {
			var serverAddresses = AppSettings.ServerMonitorServers;
			foreach (var appWidgetId in appWidgetIds) {
				if (serverAddresses.TryGetValue(appWidgetId, out string serverAddress)) {
					Update(context, appWidgetManager, appWidgetId, serverAddress);
				} else {
					UpdateEmpty(context, appWidgetManager, appWidgetId);
				}
			}
		}

		public override void OnReceive(Context context, Intent intent) {
			base.OnReceive(context, intent);
			var appWidgetManager = AppWidgetManager.GetInstance(context);
			var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(ServerMonitorAppWidget)));
			if (intent.Action == UpdateAction) {
				var appWidgetIds = appWidgetManager.GetAppWidgetIds(componentName);
				OnUpdate(context, appWidgetManager, appWidgetIds);
			} else if (intent.Action == RefreshAction
				&& intent.Extras.GetString(ServerAddressExtraKey, null) is string serverAddress
				&& intent.Extras.GetInt(WidgetIdExtraKey, -1) is int appWidgetId && appWidgetId != -1) {
				Update(context, appWidgetManager, appWidgetId, serverAddress);
			}
		}

		public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions) {
			base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
			var serverAddresses = AppSettings.ServerMonitorServers;
			if (serverAddresses.TryGetValue(appWidgetId, out string serverAddress)) {
				Task.Run(update);
				async Task update() {
					var server = await ServerListItemVM.FindExistingOrLoad(serverAddress, true, false);
					if (server != null) {
						await MainThread.InvokeOnMainThreadAsync(() => {
							SetView(context, appWidgetManager, appWidgetId, newOptions, server);
						});
					}
				}
			}
		}

		private void Update(Context context, AppWidgetManager appWidgetManager, int appWidgetId, string serverAddress) {
			Task.Run(update);
			async Task update() {
				await setLoading(true);
				var server = await ServerListItemVM.FindExistingOrLoad(serverAddress, true);
				if (server != null) {
					await setView();
				}
				bool refreshed = await server?.Refresh();
				await setLoading(false);
				if (refreshed) {
					await setView();
				}

				async Task setLoading(bool loading) {
					await MainThread.InvokeOnMainThreadAsync(() => {
						var views = new RemoteViews(context.PackageName, Resource.Layout.server_monitor_widget);
						views.SetViewVisibility(Resource.Id.loading_progressbar, loading ? ViewStates.Visible : ViewStates.Gone);
						appWidgetManager.UpdateAppWidget(appWidgetId, views);
					});
				}
				async Task setView() {
					await MainThread.InvokeOnMainThreadAsync(() => {
						var options = appWidgetManager.GetAppWidgetOptions(appWidgetId) ?? new();
						SetView(context, appWidgetManager, appWidgetId, options, server);
					});
				}
			}
		}

		private void SetView(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions, ServerListItemVM server) {
			string serverAddress = server.Address;

			var views = new RemoteViews(context.PackageName, Resource.Layout.server_monitor_widget);

			var serverInfoIntent = new Intent(context, typeof(MainActivity));
			serverInfoIntent.SetAction(WidgetLinkAction);
			serverInfoIntent.PutExtra(ServerAddressExtraKey, serverAddress);
			var serverInfoPendingIntent = PendingIntent.GetActivity(context, appWidgetId, serverInfoIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
			views.SetOnClickPendingIntent(Resource.Id.widget_layout, serverInfoPendingIntent);

			var refreshIntent = new Intent(context, typeof(ServerMonitorAppWidget));
			refreshIntent.SetAction(RefreshAction);
			refreshIntent.PutExtra(ServerAddressExtraKey, serverAddress);
			refreshIntent.PutExtra(WidgetIdExtraKey, appWidgetId);
			var refreshPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, refreshIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
			views.SetOnClickPendingIntent(Resource.Id.refresh_button, refreshPendingIntent);

			var playersIntent = new Intent(context, typeof(ServerMonitorDialogActivity));
			playersIntent.SetAction(PlayersAction);
			playersIntent.PutExtra(PlayersExtraKey, server.PlayersList);
			var playersPendingIntent = PendingIntent.GetActivity(context, appWidgetId, playersIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
			views.SetOnClickPendingIntent(Resource.Id.players_button, playersPendingIntent);
			
			int minHeight = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMinHeight),
				maxHeight = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMaxHeight),
				minWidth = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMinWidth),
				maxWidth = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMaxWidth);
			bool bigView = maxHeight >= 120;
			if (bigView) {
				views.SetTextViewText(Resource.Id.players_textview, $"{server.Players} players");
				views.SetViewVisibility(Resource.Id.map_textview, ViewStates.Visible);
			} else {
				views.SetTextViewText(Resource.Id.players_textview, $"{server.Players}, {server.MapName}");
				views.SetViewVisibility(Resource.Id.map_textview, ViewStates.Gone);
			}
			views.SetTextViewText(Resource.Id.server_name_textview, ColourTextValueConverter.Convert(server.ServerName));
			views.SetTextViewText(Resource.Id.map_textview, server.MapName);
			//to be safe
			views.SetViewVisibility(Resource.Id.loading_progressbar, ViewStates.Gone);
			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}

		private void UpdateEmpty(Context context, AppWidgetManager appWidgetManager, int appWidgetId) {
			var views = new RemoteViews(context.PackageName, Resource.Layout.server_monitor_empty_widget);

			var addIntent = new Intent(context, typeof(ServerMonitorDialogActivity));
			addIntent.SetAction(AddAction);
			addIntent.PutExtra(WidgetIdExtraKey, appWidgetId);
			var addPendingIntent = PendingIntent.GetActivity(context, appWidgetId, addIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
			views.SetOnClickPendingIntent(Resource.Id.add_textview, addPendingIntent);

			appWidgetManager.UpdateAppWidget(appWidgetId, views);
		}

		public override void OnDeleted(Context context, int []appWidgetIds)
		{
			var serverAddresses = AppSettings.ServerMonitorServers;
			foreach (var appWidgetId in appWidgetIds) {
				serverAddresses.Remove(appWidgetId);
			}
			AppSettings.ServerMonitorServers = serverAddresses;
			base.OnDeleted(context, appWidgetIds);
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
			Platform.Init(this, savedInstanceState);
			if (Intent.Action == ServerMonitorAppWidget.PlayersAction && Intent.Extras.GetStringArray(ServerMonitorAppWidget.PlayersExtraKey) is string[] players) {
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
			} else if (Intent.Action == ServerMonitorAppWidget.AddAction && Intent.Extras.GetInt(ServerMonitorAppWidget.WidgetIdExtraKey, -1) is int appWidgetId && appWidgetId != -1) {
				Task.Run(add);
				async Task add() {
					var servers = await Mvx.IoCProvider.Resolve<ICacheService>().LoadFavouriteServers();
					await MainThread.InvokeOnMainThreadAsync(() => {
						if (!servers.IsNullOrEmpty()) {
							DialogService.Show(new() {
								Title = "Add Server",
								List = new DialogListViewModel(servers.Select(s => new DialogItemVM() { Name = s.ServerName, Id = s.Address }), DialogSelectionType.InstantSelection),
								OkText = "OK",
								OkAction = config => {
									string serverAddress = config.List.SelectedItem?.Id as string;
									var serversAddresses = AppSettings.ServerMonitorServers;
									serversAddresses[appWidgetId] = serverAddress;
									AppSettings.ServerMonitorServers = serversAddresses;
									var intent = new Intent(this, typeof(ServerMonitorAppWidget));
									intent.SetAction(ServerMonitorAppWidget.RefreshAction);
									intent.PutExtra(ServerMonitorAppWidget.ServerAddressExtraKey, serverAddress);
									intent.PutExtra(ServerMonitorAppWidget.WidgetIdExtraKey, appWidgetId);
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

