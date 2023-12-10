using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Navigation.Parameters;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.ServerList.Items;

using JKClient;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.ServerList {
	public class ServerListViewModel : ReportViewModel<ServerListItemVM> {
		private readonly MvxObservableCollection<ServerListItemVM> items;
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;
		private readonly IServerListService serverListService;
		private readonly Filter filter;

		public IMvxCommand ItemClickCommand { get; init; }
		public IMvxCommand RefreshCommand { get; init; }
		public IMvxCommand AddServerCommand { get; init; }
		public IMvxCommand FilterCommand { get; init; }

		protected override string ReportTitle => "Report Server";
		protected override string ReportMessage => "Do you want to report this server?";
		protected override string ReportedTitle => "Server Reported";
		protected override string ReportedMessage => "Thank you for reporting this server";

		private bool isRefreshing;
		public bool IsRefreshing {
			get => isRefreshing;
			set => SetProperty(ref isRefreshing, value);
		}

		private string searchText;
		public string SearchText {
			get => searchText;
			set => SetProperty(ref searchText, value, ApplyFilter);
		}

		private bool filterApplied;
		public bool FilterApplied {
			get => filterApplied;
			set => SetProperty(ref filterApplied, value);
		}

		public ServerListViewModel(ICacheService cacheService, IGameClientsService gameClientsService, IServerListService serverListService) {
			Title = "Server list";
			ItemClickCommand = new MvxAsyncCommand<ServerListItemVM>(ItemClickExecute);
			RefreshCommand = new MvxAsyncCommand(RefreshExecute);
			AddServerCommand = new MvxAsyncCommand(AddServerExecute);
			FilterCommand = new MvxAsyncCommand(FilterExecute);
			items = new MvxObservableCollection<ServerListItemVM>();
			filter = AppSettings.Filter ?? new();
			FilterApplied = !filter.IsReset;
			this.cacheService = cacheService;
			this.gameClientsService = gameClientsService;
			this.serverListService = serverListService;
		}

		private void FilterPropertyChanged(object sender, PropertyChangedEventArgs ev) {
			ApplyFilter();
			FilterApplied = !filter.IsReset;
		}

		private void ApplyFilter() {
			IEnumerable<ServerListItemVM> filteredItems = items;
			bool replace = !filter.IsReset || Items.Count != items.Count;
			filteredItems = filter.Apply(filteredItems);
			if (!string.IsNullOrEmpty(SearchText)) {
				filteredItems = filteredItems.Where(item => item.CleanServerName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)
					|| item.MapName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase));
				replace = true;
			}

			if (replace) {
				lock (Items) {
					Items.ReplaceWith(filteredItems);
				}
			}
		}

		protected override void OnServerInfoMessage(ServerInfoMessage message) {
			base.OnServerInfoMessage(message);
			var item = items.FirstOrDefault(it => it.ServerInfo.Address == message.ServerInfo.Address);
			if (item != null) {
				if (message.Status != Models.ConnectionStatus.Disconnected
					&& item.Status == Models.ConnectionStatus.Disconnected) {
					MoveItem(item, 0);
				}
				item.Set(message.ServerInfo, message.Status);
			}
		}

		protected override void OnFavouriteMessage(FavouriteMessage message) {
			base.OnFavouriteMessage(message);
			var item = items.FirstOrDefault(it => it.ServerInfo.Address == message.ServerInfo.Address);
			item?.SetFavourite(message.IsFavourite);
		}

		private async Task FilterExecute() {
			await NavigationService.NavigateFromRoot<FilterViewModel, Filter>(filter);
		}

		private async Task AddServerExecute() {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Add Server",
				OkText = "Add",
				CancelText = "Cancel",
				OkAction = config => {
					string inputAddress = config?.Input?.Text;
					if (string.IsNullOrEmpty(inputAddress)) {
						return;
					}
					Task.Run(async () => await addServerContinue(inputAddress));
				},
				Input = new DialogInputViewModel()
			});
			async Task addServerContinue(string inputAddress) {
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
						Title = "Failed Adding Server",
						Message = $"Could not resolve \"{inputAddress}\"",
						OkText = "OK"
					});
					return;
				}
				if (Items.FirstOrDefault(item => item.ServerInfo.Address == netAddress) is ServerListItemVM item) {
					IsLoading = false;
					await DialogService.ShowAsync(new JKDialogConfig() {
						Title = "Server Exists",
						Message = $"Would you like to connect to \"{item.ServerName}{JKClient.Common.EscapeCharacter}\" (\"{inputAddress}\")?",
						OkText = "Connect",
						CancelText = "Cancel",
						OkAction = _ => {
							Task.Run(async () => await ItemClickExecute(item));
						}
					});
					return;
				}
				var serverInfo = await serverListService.GetServerInfo(netAddress);
				IsLoading = false;
				if (serverInfo == null) {
					await DialogService.ShowAsync(new JKDialogConfig() {
						Title = "Failed Adding Server",
						Message = $"There is no server with address \"{inputAddress}\"",
						OkText = "OK"
					});
					return;
				}
				var server = new ServerListItemVM(serverInfo);
				InsertItem(0, server);
				IsLoading = false;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Server Added",
					Message = $"Would you like to connect to \"{inputAddress}\"?",
					OkText = "Connect",
					CancelText = "Cancel",
					OkAction = config => {
						Task.Run(async () => await ItemClickExecute(server));
					}
				});
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
				if (!servers.IsNullOrEmpty()) {
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
					await UpdateFavourites(newItems);
					filter.AddGameMods(newItems.Select(item => item.ServerInfo.GameName));
					SetItems(newItems.Except(reportedServer));
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
			if (item.Status != Models.ConnectionStatus.Disconnected) {
				await NavigationService.NavigateFromRoot<ChatViewModel, ServerInfoParameter>(new(item), item.ServerInfo);
				MoveItem(item, 0);
			} else {
				await NavigationService.NavigateFromRoot<ServerInfoViewModel, ServerInfoParameter>(new(item) { LoadInfo = true }, item.ServerInfo);
			}
		}

		protected override async Task ReportExecute(ServerListItemVM item, Action<bool> reported = null) {
			await base.ReportExecute(item, report => {
				if (report) {
					RemoveItem(item);
					Task.Run(async () => await cacheService.AddReportedServer(item));
				}
			});
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
				Title = "Enter Player Name",
				Input = new DialogInputViewModel(name, true),
				OkText = "OK",
				OkAction = config => {
					AppSettings.PlayerName = config?.Input?.Text;
				},
				CancelText = "Cancel"
			});
		}

		private async Task LoadServerList() {
			IsLoading = true;
			try {
				IEnumerable<ServerListItemVM> newItems = null;
				var recentServers = await cacheService.LoadRecentServers();
				var servers = await serverListService.GetCurrentList();
				if (!servers.IsNullOrEmpty()) {
					var serverItems = servers.Where(server => server.Ping != 0).OrderByDescending(server => server.Clients).Select(SetupItem);
					var newCollection = new ObservableCollection<ServerListItemVM>(serverItems);
					AddRecentServers(newCollection, recentServers);
					UpdateServersStatuses(newCollection);
					newItems = newCollection;
				} else if (recentServers.Any()) {
					UpdateServersStatuses(recentServers);
					newItems = recentServers.Reverse();
				}
				if (newItems != null) {
					var reportedServers = await cacheService.LoadReportedServers();
					await UpdateFavourites(newItems);
					filter.AddGameMods(newItems.Select(item => item.ServerInfo.GameName));
					SetItems(newItems.Except(reportedServers));
				}
			} catch (Exception exception) {
				await ExceptionCallback(exception);
			}
			IsLoading = false;
		}

		private void SetItems(IEnumerable<ServerListItemVM> items) {
			this.items.ReplaceWith(items);
			lock (Items) {
				this.Items.ReplaceWith(filter.Apply(this.items));
			}
		}

		private void MoveItem(ServerListItemVM item, int newIndex) {
			int oldIndex = items.IndexOf(item);
			if (oldIndex >= 0)
				items.Move(oldIndex, newIndex);
			lock (Items) {
				oldIndex = Items.IndexOf(item);
				if (oldIndex >= 0)
					Items.Move(oldIndex, newIndex);
			}
		}

		private void InsertItem(int index, ServerListItemVM item) {
			items.Insert(index, item);
			lock (Items) {
				Items.Insert(index, item);
			}
		}

		private void RemoveItem(ServerListItemVM item) {
			items.Remove(item);
			lock (Items) {
				Items.Remove(item);
			}
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

		private async Task UpdateFavourites(IEnumerable<ServerListItemVM> items) {
			var favouriteItems = await cacheService.LoadFavouriteServers();
			foreach (var favouriteItem in favouriteItems) {
				var existingItem = items.FirstOrDefault(item => item.ServerInfo == favouriteItem.ServerInfo);
				existingItem?.SetFavourite(true);
			}
		}

		private ServerListItemVM SetupItem(ServerInfo server) {
			return new ServerListItemVM(server);
		}

		public override void ViewCreated() {
			base.ViewCreated();
			filter.PropertyChanged -= FilterPropertyChanged;
			filter.PropertyChanged += FilterPropertyChanged;
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				filter.PropertyChanged -= FilterPropertyChanged;
			}
			base.ViewDestroy(viewFinishing);
		}
	}
}