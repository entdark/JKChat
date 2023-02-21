using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.ServerList.Items;

using JKClient;

using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.ServerList {
	public class ServerListViewModel : ReportViewModel<ServerListItemVM> {
		private MvxSubscriptionToken serverInfoMessageToken;
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;
		private readonly IServerListService serverListService;

		public IMvxCommand ItemClickCommand { get; init; }
		public IMvxCommand RefreshCommand { get; init; }
		public IMvxCommand AddServerCommand { get; init; }
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

		public ServerListViewModel(ICacheService cacheService, IGameClientsService gameClientsService, IServerListService serverListService) {
			Title = "Server list";
			ItemClickCommand = new MvxAsyncCommand<ServerListItemVM>(ItemClickExecute);
			RefreshCommand = new MvxAsyncCommand(RefreshExecute);
			AddServerCommand = new MvxAsyncCommand(AddServerExecute);
			Items = new MvxObservableCollection<ServerListItemVM>();
			serverInfoMessageToken = Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			this.cacheService = cacheService;
			this.gameClientsService = gameClientsService;
			this.serverListService = serverListService;
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

		private async Task AddServerExecute() {
			string inputAddress = null;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Add server",
				RightButton = "Add",
				LeftButton = "Cancel",
				RightClick = (input) => {
					inputAddress = input as string;
				},
				Type = JKDialogType.Title | JKDialogType.Input
			});
			if (string.IsNullOrEmpty(inputAddress)) {
				return;
			}
			IsLoading = true;
			NetAddress netAddress = null;
			bool success = await JKChat.Core.Helpers.Common.ExceptionalTaskRun(() => {
				try {
					netAddress = NetAddress.FromString(inputAddress);
				} catch {
					IsLoading = false;
					throw;
				}
			});
			if (!success) {
				IsLoading = false;
				return;
			}
			if (netAddress == null) {
				IsLoading = false;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Failed adding server",
					Message = $"Could not resolve \"{inputAddress}\"",
					RightButton = "OK",
					Type = JKDialogType.Title | JKDialogType.Message
				});
				return;
			}
			bool connect = false;
			if (Items.FirstOrDefault(item => item.ServerInfo.Address == netAddress) is ServerListItemVM item) {
				IsLoading = false;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Server exists",
					Message = $"Would you like to connect to \"{item.ServerName}{JKClient.Common.EscapeCharacter}\" (\"{inputAddress}\")?",
					RightButton = "Connect",
					LeftButton = "Cancel",
					RightClick = _ => {
						connect = true;
					},
					Type = JKDialogType.Title | JKDialogType.Message
				});
				if (connect) {
					await ItemClickExecute(item);
				}
				return;
			}
			var infoString = await serverListService.GetServerInfo(netAddress);
			IsLoading = false;
			if (infoString == null) {
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Failed adding server",
					Message = $"There is no server with address \"{inputAddress}\"",
					RightButton = "OK",
					Type = JKDialogType.Title | JKDialogType.Message
				});
				return;
			}
			var serverInfo = new ServerInfo(infoString) {
				Address = netAddress,
				HostName = inputAddress
			};
			var server = new ServerListItemVM(serverInfo);
			Items.Insert(0, server);
			IsLoading = false;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Server added",
				Message = $"Would you like to connect to \"{inputAddress}\"?",
				RightButton = "Connect",
				LeftButton = "Cancel",
				RightClick = _ => {
					connect = true;
				},
				Type = JKDialogType.Title | JKDialogType.Message
			});
			if (connect) {
				await ItemClickExecute(server);
			}
		}

		private async Task RefreshExecute() {
			if (IsLoading) {
				return;
			}
			IsRefreshing = true;
			try {
				IEnumerable<ServerListItemVM> newItems = null;
				var recentServers = await cacheService.LoadRecentServers();
				var servers = await serverListService.RefreshList();
				if (servers != null && servers.Any()) {
					var serverItems = servers.Where(server => server.Ping != 0).OrderByDescending(server => server.Clients).Select(SetupItem).ToList();
					var newCollection = new ObservableCollection<ServerListItemVM>(serverItems);
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
				await ExceptionCallback(exception);
			}
			IsRefreshing = false;
		}

		private async Task ItemClickExecute(ServerListItemVM item) {
			if (SelectedItem != null) {
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

		protected override Task BackgroundInitialize() {
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
				IEnumerable<ServerListItemVM> newItems = null;
				var recentServers = await cacheService.LoadRecentServers();
				var servers = await serverListService.GetCurrentList();
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
				await ExceptionCallback(exception);
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
			}
			base.ViewDestroy(viewFinishing);
		}
	}
}
