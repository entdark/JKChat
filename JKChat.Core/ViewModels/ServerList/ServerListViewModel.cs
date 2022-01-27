using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
	public class ServerListViewModel : BaseViewModel {
		private ServerBrowser serverBrowser;
		private MvxSubscriptionToken serverInfoMessageToken;
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;

		public IMvxCommand SelectionChangedCommand { get; private set; }
		public IMvxCommand RefreshCommand { get; private set; }
//		public IMvxCommand FilterCommand;

		private MvxObservableCollection<ServerListItemVM> items;
		public MvxObservableCollection<ServerListItemVM> Items {
			get => items;
			set => SetProperty(ref items, value);
		}

		private bool isRefreshing;
		public bool IsRefreshing {
			get => isRefreshing;
			set => SetProperty(ref isRefreshing, value);
		}

		public ServerListViewModel(ICacheService cacheService, IGameClientsService gameClientsService) {
			Title = "Server List";
			SelectionChangedCommand = new MvxAsyncCommand<ServerListItemVM>(SelectionChangedExecute);
			RefreshCommand = new MvxAsyncCommand(RefreshExecute);
			Items = new MvxObservableCollection<ServerListItemVM>();
			serverBrowser = new ServerBrowser();
			serverInfoMessageToken = Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			this.cacheService = cacheService;
			this.gameClientsService = gameClientsService;
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			var item = Items.FirstOrDefault(it => it.ServerInfo.Address == message.ServerInfo.Address);
			if (item != null) {
/*				if (message.Status != Models.ConnectionStatus.Disconnected) {
					InvokeOnMainThread(() => {
						Items.Move(Items.IndexOf(item), 0);
					});
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
				var recentServers = await cacheService.LoadRecentServers();
				var servers = await serverBrowser.RefreshList();
				if (servers != null && servers.Any()) {
					var serverItems = servers.Where(server => server.Ping != 0).OrderByDescending(server => server.Clients).Select(SetupItem).ToList();
					var newItems = new MvxObservableCollection<ServerListItemVM>(serverItems);
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
					AddRecentServers(newItems, recentServers);
					UpdateServersStatuses(newItems);
					InvokeOnMainThread(() => {
						Items = newItems;
					});
				} else if (recentServers.Count > 0) {
					UpdateServersStatuses(recentServers);
					InvokeOnMainThread(() => {
						Items = new MvxObservableCollection<ServerListItemVM>(recentServers.Reverse());
					});
				}
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			IsRefreshing = false;
		}

		private async Task SelectionChangedExecute(ServerListItemVM item) {
			InvokeOnMainThread(() => {
				Items.Move(Items.IndexOf(item), 0);
			});
			cacheService.SaveRecentServer(item);
			await NavigationService.Navigate<ChatViewModel, ServerListItemVM>(item);
		}

		public override Task Initialize() {
			serverBrowser.Start(ExceptionCallback);
			return LoadData();
		}

		private async Task LoadData() {
			if (Settings.FirstLaunch) {
				await RequestPlayerName();
				await LoadServerList();
			} else {
				await Task.WhenAll(RequestPlayerName(), LoadServerList());
			}
		}

		private async Task RequestPlayerName() {
/*			if (!Settings.FirstLaunch) {
				return;
			}*/
			string name = Settings.PlayerName;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Choose your name",
				Input = name,
				RightButton = "OK",
				RightClick = (input) => {
					name = input as string;
				},
				Type = JKDialogType.Title | JKDialogType.Input
			});
			if (string.IsNullOrEmpty(name)) {
				name = Settings.DefaultName;
			} else if (name.Length > 31) {
				name = name.Substring(0, 31);
			}
			Settings.PlayerName = name;
		}

		private async Task LoadServerList() {
			IsLoading = true;
			try {
				var recentServers = await cacheService.LoadRecentServers();
				var servers = await serverBrowser.GetNewList();
				if (servers != null && servers.Any()) {
					var serverItems = servers.Where(server => server.Ping != 0).OrderByDescending(server => server.Clients).Select(SetupItem);
					var newItems = new MvxObservableCollection<ServerListItemVM>(serverItems/*.Where(s => s.GameType.Contains("Siege"))*/);
					AddRecentServers(newItems, recentServers);
					UpdateServersStatuses(newItems);
					InvokeOnMainThread(() => {
						Items = new MvxObservableCollection<ServerListItemVM>(newItems);
					});
				} else if (recentServers.Count > 0) {
					UpdateServersStatuses(recentServers);
					InvokeOnMainThread(() => {
						Items = new MvxObservableCollection<ServerListItemVM>(recentServers.Reverse());
					});
				}
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			IsLoading = false;
		}

		private static void AddRecentServers(MvxObservableCollection<ServerListItemVM> items, ICollection<ServerListItemVM> recentServers) {
			foreach (var recentServer in recentServers) {
				var exitingItem = items.FirstOrDefault(item => item.ServerInfo.Address == recentServer.ServerInfo.Address);
				if (exitingItem != null) {
					items.Move(items.IndexOf(exitingItem), 0);
				} else {
					items.Insert(0, recentServer);
				}
			}
		}

		private void UpdateServersStatuses(ICollection<ServerListItemVM> items) {
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
			if (serverBrowser == null) {
				serverBrowser = new ServerBrowser();
				serverBrowser.Start(ExceptionCallback);
			}
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				if (serverInfoMessageToken != null) {
					Messenger.Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
					serverInfoMessageToken = null;
				}
				if (serverBrowser != null) {
					serverBrowser.Stop();
					serverBrowser.Dispose();
					serverBrowser = null;
				}
			}
			base.ViewDestroy(viewFinishing);
		}
	}
}
