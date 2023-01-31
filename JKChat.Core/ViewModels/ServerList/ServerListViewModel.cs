using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Messages;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Main;
using JKChat.Core.ViewModels.ServerList.Items;

using JKClient;

using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;
using MvvmCross.Presenters.Hints;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.ServerList {
	public class ServerListViewModel : ReportViewModel<ServerListItemVM> {
		private ServerBrowser[] serverBrowsers;
		private MvxSubscriptionToken serverInfoMessageToken;
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;

		public IMvxCommand ItemClickCommand { get; init; }
		public IMvxCommand RefreshCommand { get; init; }
//		public IMvxCommand FilterCommand;

		protected override string ReportTitle => "Report server";
		protected override string ReportMessage => "Do you want to report this server?";
		protected override string ReportedTitle => "Server reported";
		protected override string ReportedMessage => "Thank you for reporting this server";

		private bool isRefreshing;
		public bool IsRefreshing {
			get => isRefreshing;
			set => SetProperty(ref isRefreshing, value);
		}

		public ServerListViewModel(ICacheService cacheService, IGameClientsService gameClientsService) {
			Title = "Server list";
			ItemClickCommand = new MvxAsyncCommand<ServerListItemVM>(ItemClickExecute);
			RefreshCommand = new MvxAsyncCommand(RefreshExecute);
			Items = new MvxObservableCollection<ServerListItemVM>();
			serverInfoMessageToken = Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			this.cacheService = cacheService;
			this.gameClientsService = gameClientsService;
		}

		private async Task InitServerBrowsers() {
			if (serverBrowsers != null) {
				return;
			}
			await Helpers.Common.ExceptionalTaskRun(() => {
				serverBrowsers = new ServerBrowser[] {
					new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol25)),
					new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol26)),
					new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol15)),
					new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol16)),
					new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol68)),
					new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol71))
				};
				foreach (var serverBrowser in serverBrowsers) {
					serverBrowser.Start(Helpers.Common.ExceptionCallback);
				}
			});
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			var item = Items.FirstOrDefault(it => it.ServerInfo.Address == message.ServerInfo.Address);
			if (item != null) {
/*				if (message.Status != Models.ConnectionStatus.Disconnected) {
					Items.Move(Items.IndexOf(item), 0);
				}*/
				item.Set(message.ServerInfo, message.Status);
				cacheService.UpdateRecentServer(item);
			}
		}

		private async Task RefreshExecute() {
			if (IsLoading) {
				return;
			}
			IsRefreshing = true;
			try {
				await InitServerBrowsers();
				IEnumerable<ServerListItemVM> newItems = null;
				var recentServers = await cacheService.LoadRecentServers();
				var refreshListTasks = serverBrowsers.Select(s => s.RefreshList());
				var servers = (await Task.WhenAll(refreshListTasks)).SelectMany(t => t).Distinct(new ServerInfoComparer());
				if (servers != null && servers.Any()) {
					var serverItems = servers.Where(server => server.Ping != 0).OrderByDescending(server => server.Clients).Select(SetupItem).ToList();
					var newCollection = new ObservableCollection<ServerListItemVM>(serverItems);
/*					var connectedItems = Items.Where(it => it.Status != Models.ConnectionStatus.Disconnected);
					if (connectedItems != null) {
						foreach (var connectedItem in connectedItems) {
							var item = newItems.FirstOrDefault(it => it.ServerInfo.Address == connectedItem.ServerInfo.Address);
							if (item != null) {
								//newItems.Move(newItems.IndexOf(item), 0);
								item.Status = connectedItem.Status;
							}
						}
					}*/
					AddRecentServers(newCollection, recentServers);
					UpdateServersStatuses(newCollection);
					newItems = newCollection;
				} else if (recentServers.Any()) {
					UpdateServersStatuses(recentServers);
					newItems = recentServers.Reverse();
				}
				if (newItems != null) {
					var reportedServer = await cacheService.LoadReportedServers();
					Items.ReplaceWith(newItems.Except(reportedServer));
				}
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			IsRefreshing = false;
		}

		private async Task ItemClickExecute(ServerListItemVM item) {
			var selectedItem = GetSelectedItem();
			if (selectedItem != null) {
				return;
			}
			Items.Move(Items.IndexOf(item), 0);
			await NavigationService.NavigateFromRoot<ChatViewModel, ServerListItemVM>(item, viewModel => {
				return (viewModel as ChatViewModel)?.ServerInfo != item.ServerInfo;
			});
			await cacheService.SaveRecentServer(item);
		}

		protected override async Task<bool> ReportExecute(ServerListItemVM item) {
			bool report = await base.ReportExecute(item);
			if (report) {
				Items.Remove(item);
				await cacheService.AddReportedServer(item);
			}
			return report;
		}

		protected override void SelectExecute(ServerListItemVM item) {
			base.SelectExecute(item);
			if (SelectedItem == null) {
				Title = "Server list";
			}
		}

		public override Task Initialize() {
			return LoadData();
		}

		private async Task LoadData() {
			if (AppSettings.FirstLaunch) {
				await RequestPlayerName();
			}
			await LoadServerList();
		}

		private async Task RequestPlayerName() {
			string name = AppSettings.PlayerName;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Choose your name",
				Input = name,
				RightButton = "OK",
				RightClick = (input) => {
					name = input as string;
				},
				Type = JKDialogType.Title | JKDialogType.Input
			});
			AppSettings.PlayerName = name;
		}

		private async Task LoadServerList() {
			IsLoading = true;
			try {
				await InitServerBrowsers();
				IEnumerable<ServerListItemVM> newItems = null;
				var recentServers = await cacheService.LoadRecentServers();
				var getNewListTasks = serverBrowsers.Select(s => s.GetNewList());
				var servers = (await Task.WhenAll(getNewListTasks)).SelectMany(t => t).Distinct(new ServerInfoComparer());
				if (servers != null && servers.Any()) {
					var serverItems = servers.Where(server => server.Ping != 0).OrderByDescending(server => server.Clients).Select(SetupItem);
					var newCollection = new ObservableCollection<ServerListItemVM>(serverItems/*.Where(s => s.GameType.Contains("Siege"))*/);
					AddRecentServers(newCollection, recentServers);
					UpdateServersStatuses(newCollection);
					newItems = newCollection;
				} else if (recentServers.Any()) {
					UpdateServersStatuses(recentServers);
					newItems = recentServers.Reverse();
				}
				if (newItems != null) {
					var reportedServer = await cacheService.LoadReportedServers();
					Items.ReplaceWith(newItems.Except(reportedServer));
				}
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			IsLoading = false;
		}

		private static void AddRecentServers(ObservableCollection<ServerListItemVM> items, IEnumerable<ServerListItemVM> recentServers) {
			foreach (var recentServer in recentServers) {
				var exitingItem = items.FirstOrDefault(item => item.ServerInfo.Address == recentServer.ServerInfo.Address);
				if (exitingItem != null) {
					items.Move(items.IndexOf(exitingItem), 0);
				} else {
					items.Insert(0, recentServer);
				}
			}
		}

		private void UpdateServersStatuses(IEnumerable<ServerListItemVM> items) {
			foreach (Models.ConnectionStatus status in Enum.GetValues(typeof(Models.ConnectionStatus))) {
				var addresses = gameClientsService.AddressesWithStatus(status);
				if (addresses == null) {
					continue;
				}
				foreach (var address in addresses) {
					var existingItem = items.FirstOrDefault(item => item.ServerInfo.Address == address);
					if (existingItem != null) {
						existingItem.Status = status;
					}
				}
			}
		}

		private ServerListItemVM SetupItem(ServerInfo server) {
			return new ServerListItemVM(server);
		}

		public override void ViewCreated() {
			base.ViewCreated();
			if (serverInfoMessageToken == null) {
				serverInfoMessageToken = Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			}
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				if (serverInfoMessageToken != null) {
					Messenger.Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
					serverInfoMessageToken = null;
				}
				foreach (var serverBrowser in serverBrowsers) {
					if (serverBrowser != null) {
						serverBrowser.Stop();
						serverBrowser.Dispose();
					}
				}
				serverBrowsers = null;
			}
			base.ViewDestroy(viewFinishing);
		}
	}
}
