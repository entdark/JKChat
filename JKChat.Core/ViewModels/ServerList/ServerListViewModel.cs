using System;
using System.Collections.Generic;
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
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.ServerList {
	public class ServerListViewModel : ReportViewModel<ServerListItemVM> {
		private readonly MvxObservableCollection<ServerListItemVM> items;
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;
		private readonly IServerListService serverListService;
		private Filter filter;
		private MvxSubscriptionToken filterMessageToken;

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
			filter = AppSettings.Filter;
			FilterApplied = !filter.IsReset;
			this.cacheService = cacheService;
			this.gameClientsService = gameClientsService;
			this.serverListService = serverListService;
			filterMessageToken = Messenger.Subscribe<FilterMessage>(OnFilterMessage);
		}

		private void OnFilterMessage(FilterMessage message) {
			filter = AppSettings.Filter;
			ApplyFilter();
			FilterApplied = !filter.IsReset;
		}

		private void ApplyFilter() {
			lock (items) lock (Items) {
				var filteredItems = items.ApplyFilter(filter, SearchText);
				bool replace = !filter.IsReset || Items.Count != items.Count || !string.IsNullOrEmpty(SearchText);
				if (replace) {
					Items.ReplaceWith(filteredItems);
				}
			}
		}

		protected override void OnServerInfoMessage(ServerInfoMessage message) {
			base.OnServerInfoMessage(message);
			var item = items.FirstOrDefault(it => it.ServerInfo == message.ServerInfo);
			if (item != null) {
				if (message.Status.HasValue
					&& message.Status.Value != Models.ConnectionStatus.Disconnected
					&& item.Status == Models.ConnectionStatus.Disconnected) {
					MoveItem(item, 0);
					if (!Items.Contains(item)) {
						Items.Insert(0, item);
					}
				}
				item.Set(message.ServerInfo, message.Status);
			} else if (message.Status.HasValue && message.Status.Value != Models.ConnectionStatus.Disconnected) {
				var server = new ServerListItemVM(message.ServerInfo) {
					Status = message.Status.Value
				};
				InsertItem(0, server);
			}
		}

		protected override void OnFavouriteMessage(FavouriteMessage message) {
			base.OnFavouriteMessage(message);
			var item = items.FirstOrDefault(it => it.ServerInfo == message.ServerInfo);
			item?.SetFavourite(message.IsFavourite);
		}

		private async Task FilterExecute() {
			await NavigationService.NavigateFromRoot<FilterViewModel>();
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
							Task.Run(item.Connect);
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
				await InvokeOnMainThreadAsync(() => {
					InsertItem(0, server);
				});
				IsLoading = false;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Server Added",
					Message = $"Would you like to connect to \"{server.ServerName}{JKClient.Common.EscapeCharacter}\" (\"{inputAddress}\")?",
					OkText = "Connect",
					CancelText = "Cancel",
					OkAction = config => {
						Task.Run(server.Connect);
					}
				});
			}
		}

		private async Task RefreshExecute() {
			if (IsLoading) {
				IsRefreshing = false;
				return;
			}
			IsRefreshing = true;
			await LoadServerList(serverListService.RefreshList);
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
			IsLoading = true;
			await LoadServerList(serverListService.GetCurrentList);
			IsLoading = false;
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

		private async Task LoadServerList(Func<Task<IEnumerable<ServerInfo>>> loadingFunc) {
			try {
				var serverInfosWithStatuses = Enum.GetValues<Models.ConnectionStatus>()
					.SelectMany(status => (gameClientsService.ServerInfosWithStatuses(status) ?? Enumerable.Empty<ServerInfo>()).Select(serverInfo => new { Status = status, ServerInfo = serverInfo }))
					.Where(serverInfoWithStatus => serverInfoWithStatus != null)
					.ToDictionary(serverInfoWithStatus => serverInfoWithStatus.ServerInfo, serverInfoWithStatus => serverInfoWithStatus.Status);
				IEnumerable<ServerListItemVM> newItems = null;
				var recentServers = (await cacheService.LoadRecentServers()).ToArray();
				var servers = await loadingFunc();
				if (!servers.IsNullOrEmpty()) {
					var serverItems = recentServers
						.ReverseWithUpdate(servers, serverInfosWithStatuses)
						.Union(servers
							.Where(server => server.Ping != 0)
							.OrderByDescending(server => server.Clients)
							.Select(server => new ServerListItemVM(server) {
								Status = serverInfosWithStatuses.TryGetValue(server, out var status) ? status : Models.ConnectionStatus.Disconnected
							})
						, new ServerListItemVM.Comparer());
					newItems = serverItems;
				} else if (recentServers.Length > 0) {
					newItems = recentServers
						.UpdateStatuses(serverInfosWithStatuses)
						.Reverse();
				}
				if (newItems != null) {
					var reportedServers = await cacheService.LoadReportedServers();
					var favouriteServers = await cacheService.LoadFavouriteServers();
					newItems = newItems
						.Except(reportedServers)
						.UpdateFavourites(favouriteServers);
					ServerListItemVM []updatedCachedServers = null;
					await InvokeOnMainThreadAsync(() => {
						SetItems(newItems);
						updatedCachedServers = this.items.Where(item => item.IsFavourite || recentServers.Any(recentServer => recentServer == item)).ToArray();
						if (filter.AddGameMods(items.Select(item => item.ServerInfo.GameName))) {
							AppSettings.Filter = filter;
						}
					});
					await cacheService.UpdateServers(updatedCachedServers ?? Array.Empty<ServerListItemVM>());
				}
			} catch (Exception exception) {
				Helpers.Common.ExceptionCallback(exception);
			}
		}

		private void SetItems(IEnumerable<ServerListItemVM> items) {
			lock (this.items) lock (this.Items) {
				this.items.ReplaceWith(items);
				this.Items.ReplaceWith(this.items.ApplyFilter(filter, SearchText));
			}
		}

		private void MoveItem(ServerListItemVM item, int newIndex) {
			lock (items) lock (Items) {
				int oldIndex = items.IndexOf(item);
				if (oldIndex >= 0)
					items.Move(oldIndex, newIndex);
				oldIndex = Items.IndexOf(item);
				if (oldIndex >= 0)
					Items.Move(oldIndex, newIndex);
			}
		}

		private void InsertItem(int index, ServerListItemVM item) {
			lock (items) lock (Items) {
				items.Insert(index, item);
				Items.Insert(index, item);
			}
		}

		private void RemoveItem(ServerListItemVM item) {
			lock (items) lock (Items) {
				items.Remove(item);
				Items.Remove(item);
			}
		}

		public override void ViewCreated() {
			base.ViewCreated();
			filterMessageToken ??= Messenger.Subscribe<FilterMessage>(OnFilterMessage);
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				if (filterMessageToken != null) {
					Messenger.Unsubscribe<FilterMessage>(filterMessageToken);
					filterMessageToken = null;
				}
			}
			base.ViewDestroy(viewFinishing);
		}
	}

	internal static class ServerListViewModelExtensions {
		public static IEnumerable<ServerListItemVM> ReverseWithUpdate(this IEnumerable<ServerListItemVM> servers, IEnumerable<ServerInfo> serverInfos, IDictionary<ServerInfo, Models.ConnectionStatus> serverInfosWithStatuses) {
			var serversArray = servers is ServerListItemVM []array ? array : servers.ToArray();
			for (int i = serversArray.Length-1; i >= 0; i--) {
				var server = serversArray[i];
				var serverInfoAndStatus = serverInfosWithStatuses.FirstOrDefault(kv => kv.Key == server.ServerInfo);
				if (serverInfoAndStatus.Key is { } serverInfo && serverInfoAndStatus.Value != Models.ConnectionStatus.Disconnected) {
					server.Set(serverInfo, serverInfoAndStatus.Value);
				} else {
					var updatedRecentServerInfo = serverInfos.FirstOrDefault(serverInfo => serverInfo.Ping != 0 && serverInfo == server.ServerInfo);
					if (updatedRecentServerInfo != null) {
						server.Set(updatedRecentServerInfo);
					}
				}
				yield return server;
			}
		}

		public static IEnumerable<ServerListItemVM> UpdateStatuses(this IEnumerable<ServerListItemVM> servers, IDictionary<ServerInfo, Models.ConnectionStatus> serverInfosWithStatuses) {
			foreach (var server in servers) {
				server.Status = serverInfosWithStatuses.TryGetValue(server.ServerInfo, out var status) ? status : default;
				yield return server;
			}
		}

		public static IEnumerable<ServerListItemVM> UpdateFavourites(this IEnumerable<ServerListItemVM> servers, IEnumerable<ServerListItemVM> favouriteServers) {
			foreach (var server in servers) {
				var updatedFavouriteItem = favouriteServers.FirstOrDefault(favouriteItem => favouriteItem == server);
				server.SetFavourite(updatedFavouriteItem != null);
				yield return server;
			}
		}

		public static IEnumerable<ServerListItemVM> ApplyFilter(this IEnumerable<ServerListItemVM> servers, Filter filter, string searchText) {
			var filteredItems = filter.Apply(servers, item => item.Status != Models.ConnectionStatus.Disconnected);
			if (!string.IsNullOrEmpty(searchText)) {
				filteredItems = filteredItems
					.Where(item =>
						item.CleanServerName.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)
						|| item.MapName.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)
					);
			}
			return filteredItems;
		}
	}
}