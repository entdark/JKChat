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
			string address = null;
/*			int id = -1;
			bool cancel = true;
            await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Select the game",
				RightButton = "OK",
                LeftButton = "Cancel",
				ListViewModel = new Dialog.DialogListViewModel() {
					Items = new List<Dialog.Items.DialogItemVM>() {
						new Dialog.Items.DialogItemVM() { Name = "Quake III Arena", Id = 0, IsSelected = true },
						new Dialog.Items.DialogItemVM() { Name = "Jedi Academy", Id = 1 },
						new Dialog.Items.DialogItemVM() { Name = "Jedi Outcast", Id = 2 }
					}
				},
                RightClick = (input) => {
					id = input is int i ? i : -1;
					cancel = false;
				},
				Type = JKDialogType.Title | JKDialogType.List
			});
			if (cancel)
				return;
			var protocol = id switch {
				0 => ProtocolVersion.Protocol68,
                1 => ProtocolVersion.Protocol26,
                2 => ProtocolVersion.Protocol15,
                _ => ProtocolVersion.Protocol26,
            };
			var version = id switch {
				0 => ClientVersion.Q3_v1_32,
                1 => ClientVersion.JA_v1_01,
                2 => ClientVersion.JO_v1_02,
                _ => ClientVersion.JA_v1_01,
            };*/
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Add server",
				RightButton = "Add",
                LeftButton = "Cancel",
                RightClick = (input) => {
                    address = input as string;
				},
				Type = JKDialogType.Title | JKDialogType.Input
			});
			if (string.IsNullOrEmpty(address)) {
				return;
			}
			bool resolved = false;
            IsLoading = true;
            await Helpers.Common.ExceptionalTaskRun(async () => {
                var netAddress = NetAddress.FromString(address);
				if (netAddress == null) {
					return;
                }
                IsLoading = false;
                resolved = true;
                bool connect = false;
                if (Items.FirstOrDefault(item => item.ServerInfo.Address == netAddress) is ServerListItemVM item) {
					await DialogService.ShowAsync(new JKDialogConfig() {
						Title = "Server exists",
						Message = $"The server \"{address}\" (\"{ColourTextHelper.CleanString(item.ServerName)}\") already exists.\nWould you like to connect to that server?",
						RightButton = "Connect",
						LeftButton = "Cancel",
						RightClick = (input) => {
							connect = true;
						},
						Type = JKDialogType.Title | JKDialogType.Message
					});
					if (connect) {
                        await ItemClickExecute(item);
					}
                    return;
				}
                var serverInfoTasks = serverBrowsers.Select(s => s.GetServerInfo(netAddress));
                var infoString = await (await Task.WhenAny<InfoString>(serverInfoTasks));
                var serverInfo = new ServerInfo(infoString) {
					Address = netAddress,
					HostName = address
				};
				var server = new ServerListItemVM(serverInfo);
				Items.Insert(0, server);
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Server added",
					Message = $"Would you like to connect to \"{address}\"?",
					RightButton = "Connect",
					LeftButton = "Cancel",
					RightClick = (input) => {
						connect = true;
					},
					Type = JKDialogType.Title | JKDialogType.Message
				});
				if (connect) {
                    await ItemClickExecute(server);
				}
			});
            IsLoading = false;
            if (resolved) {
				return;
			}
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Failed adding server",
				Message = $"Could not resolve \"{address}\"",
				RightButton = "OK",
				Type = JKDialogType.Title | JKDialogType.Message
			});
		}

		private async Task RefreshExecute() {
			if (IsLoading) {
//				IsRefreshing = false;
//				return;
			}
			IsRefreshing = true;
			try {
				IEnumerable<ServerListItemVM> newItems = null;
				var recentServers = await cacheService.LoadRecentServers();
				var servers = await serverListService.RefreshList();
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
				await ExceptionCallback(exception);
			}
			IsRefreshing = false;
		}

		private async Task ItemClickExecute(ServerListItemVM item) {
			if (SelectedItem != null) {
				return;
			}
/*			try {
				var info = await serverListService.GetServerInfo(item.ServerInfo);
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Server info",
					Message = string.Join('\n', info.Select(i => i.Key + ": " + i.Value)),
					RightButton = "OK",
					Type = JKDialogType.Title | JKDialogType.Message
				});
			} catch {}
			return;*/
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
