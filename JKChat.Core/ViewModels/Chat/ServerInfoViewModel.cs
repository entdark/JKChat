using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Navigation.Parameters;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.Core.ViewModels.ServerList.Items;

using JKClient;

using Microsoft.Maui.ApplicationModel.DataTransfer;

using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

[assembly: MvxNavigation(typeof(ServerInfoViewModel), @"jkchat://info\?address=(?<address>.*)")]
namespace JKChat.Core.ViewModels.Chat {
	public class ServerInfoViewModel : BaseServerViewModel<ServerInfoParameter>, IFromRootNavigatingViewModel {
		private readonly IGameClientsService gameClientsService;
		private readonly IServerListService serverListService;
		private readonly ICacheService cacheService;
		private readonly Lock serverInfoLocker = new();
		
		private string address;
		private GameClient gameClient;
		private bool loadData;
		private bool hasDeaths;

		private ServerInfo serverInfo;
		internal ServerInfo ServerInfo {
			get => serverInfo;
			private set {
				lock (serverInfoLocker) {
					serverInfo = value;
					UpdateServerInfo();
				}
			}
		}

		public IMvxCommand ConnectCommand { get; init; }
		public IMvxCommand FavouriteCommand { get; init; }
		public IMvxCommand ShareCommand { get; init; }
		public IMvxCommand ServerReportCommand { get; init; }

		private bool needPassword;
		public bool NeedPassword {
			get => needPassword;
			set => SetProperty(ref needPassword, value);
		}

		private Models.ConnectionStatus status;
		public Models.ConnectionStatus Status {
			get => status;
			set => SetProperty(ref status, value);
		}

		private Game game;
		public Game Game {
			get => game;
			set => SetProperty(ref game, value);
		}

		private int selectedTab;
		public int SelectedTab {
			get => selectedTab;
			set => SetProperty(ref selectedTab, value, () => {
				lock (serverInfoLocker) {
					UpdateServerInfo(true);
				}
			});
		}

		private bool isFavourite;
		public bool IsFavourite {
			get => isFavourite;
			set => SetProperty(ref isFavourite, value);
		}

		public List<KeyValueItemVM> PrimaryInfoItems { get; init; } //first 4 items
		public MvxObservableCollection<PlayerInfoItemVM> PlayerItems { get; init; } //first tab items
		public MvxObservableCollection<KeyValueItemVM> FullInfoItems { get; init; } //second tab items
		public TabItems []AllSecondaryItems { get; init; } //array of 2 collections of first and second tab items
		public MvxObservableCollection<KeyValueItemVM> AllItems { get; init; } //first 4 items + either first tab items or second tab items

		public ServerInfoViewModel(IGameClientsService gameClientsService, IServerListService serverListService, ICacheService cacheService) {
			this.gameClientsService = gameClientsService;
			this.serverListService = serverListService;
			this.cacheService = cacheService;
			ConnectCommand = new MvxAsyncCommand(ConnectExecute);
			FavouriteCommand = new MvxCommand(FavouriteExecute);
			ShareCommand = new MvxAsyncCommand(ShareExecute);
			ServerReportCommand = new MvxAsyncCommand(ReportServerExecute);
			PrimaryInfoItems = new([
				new() { Key = "Game name & version" },
				new() { Key = "Map" },
				new() { Key = "Players online" },
				new() { Key = "Game type" }
			]);
			FullInfoItems = [];
			PlayerItems = [];
			AllSecondaryItems = [
				new(0, "Scoreboard", PlayerItems),
				new(1, "Server info", FullInfoItems)
			];
			AllItems = new(PrimaryInfoItems);
		}

		public override void Prepare(ServerInfoParameter parameter) {
			loadData = parameter.LoadInfo;
//			address = parameter.ServerInfo.Address.ToString();
			Prepare(parameter.ServerInfo, parameter.IsFavourite, parameter.Status, parameter.LoadInfo);
		}

		private void Prepare(ServerInfo serverInfo, bool isFavourite, Models.ConnectionStatus status, bool loadData) {
			Status = status;
			ServerInfo = serverInfo;
			IsFavourite = isFavourite;
			if (!loadData) {
				gameClient = gameClientsService.GetClient(ServerInfo, true);
				hasDeaths = gameClient.Modification == GameModification.JAPlus;
			}
		}

		public void Init(string address) {
			if (string.IsNullOrEmpty(this.address))
				this.address = address;
			if (!string.IsNullOrEmpty(this.address) && gameClientsService.GetClient(NetAddress.FromString(this.address)) is { } gameClient) {
				loadData = true;
				Prepare(gameClient.ServerInfo, false, gameClient.Status, true);
			}
		}

		protected override void SaveStateToBundle(IMvxBundle bundle) {
			base.SaveStateToBundle(bundle);
			bundle.Data[nameof(address)] = ServerInfo?.Address?.ToString() ?? string.Empty;
			bundle.Data[nameof(loadData)] = loadData.ToString();
		}

		protected override void ReloadFromBundle(IMvxBundle state) {
			base.ReloadFromBundle(state);
			if (state.Data.TryGetValue(nameof(this.address), out string address))
				this.address = address;
			if (state.Data.TryGetValue(nameof(this.loadData), out string loadData))
				_ = bool.TryParse(loadData, out this.loadData);
		}

		protected override Task BackgroundInitialize() {
			return LoadData();
		}
		
		protected override void OnServerInfoMessage(ServerInfoMessage message) {
			base.OnServerInfoMessage(message);
			if (ServerInfo == message.ServerInfo) {
				Status = message.Status ?? Status;
				ServerInfo = message.ServerInfo;
			}
		}

		protected override void OnFavouriteMessage(FavouriteMessage message) {
			base.OnFavouriteMessage(message);
			if (ServerInfo == message.ServerInfo) {
				IsFavourite = message.IsFavourite;
			}
		}

		public bool ShouldLetOtherNavigateFromRoot(object data) {
			if (data is ServerInfo serverInfo)
				return this.ServerInfo != serverInfo;
			else if (data is string s && NetAddress.FromString(s) is var address)
				return this.ServerInfo?.Address != address;
			return true;
		}

		private async Task LoadData() {
			if (!loadData && ServerInfo != null)
				return;
			try {
				int delay = 777;
				if (ServerInfo == null) {
					IsLoading = true;
					if (!string.IsNullOrEmpty(this.address)) {
						var server = await ServerListItemVM.FindExistingOrLoad(this.address).ExecuteWithin(delay);
						delay = 0;
						if (server == null) {
							await DialogService.ShowAsync(new JKDialogConfig() {
								Title = "Failed to Load",
								Message = $"There is no server with address \"{address}\"",
								OkText = "OK",
								OkAction = _ => {
									Task.Run(close);
								}
							});
							return;
						}
						Prepare(server.ServerInfo, server.IsFavourite, server.Status, loadData);
					} else {
						await DialogService.ShowAsync(new JKDialogConfig() {
							Title = "Failed to Load",
							Message = $"Server address is empty",
							OkText = "OK",
							OkAction = _ => {
								Task.Run(close);
							}
						});
						return;
					}
				}
				if (ServerInfo != null) {
					var server = await cacheService.GetCachedServer(ServerInfo);
					IsFavourite = server?.IsFavourite == true;
					bool update = false;
					var status = gameClientsService.GetStatus(serverInfo);
					if (status == null || status == Models.ConnectionStatus.Disconnected) {
						IsLoading = true;
						var newServerInfo = await this.serverListService.GetServerInfo(serverInfo).ExecuteWithin(delay);
						if (newServerInfo != null) {
							ServerInfo = newServerInfo;
							update = true;
						}
					}
					if (update) {
						await cacheService.UpdateServer(ServerInfo);
					}
				}
			} catch (Exception exception) {
				Helpers.Common.ExceptionCallback(exception);
			} finally {
				IsLoading = false;
			}

			async Task close() {
				await NavigationService.Close(this);
			}
		}

		private void UpdateServerInfo(bool tabChanged = false) {
			InvokeOnMainThread(() => {
				Title = ServerInfo.HostName;
				NeedPassword = ServerInfo.NeedPassword;
				Game = ServerInfo.Version.ToGame();
				PrimaryInfoItems[0].Value = ServerInfo.Version.ToDisplayString();
				PrimaryInfoItems[1].Value = ServerInfo.MapName;
				PrimaryInfoItems[2].Value = $"{ServerInfo.Clients}/{ServerInfo.MaxClients}";
				PrimaryInfoItems[3].Value = ServerInfo.GameType.ToDisplayString(ServerInfo.Version);
				if (ServerInfo.RawInfo != null) {
					FullInfoItems.MergeWith(ServerInfo.RawInfo.Select(kv => new KeyValueItemVM() { Key = kv.Key, Value = kv.Value }), (oldItem, newItem) => {
						bool theSame = oldItem.Key == newItem.Key;
						if (theSame) {
							oldItem.Value = newItem.Value;
						}
						return theSame;
					});
				}
				if (ServerInfo.PlayersInfo != null) {
					PlayerItems.MergeWith(ServerInfo.PlayersInfo.Select(player => new PlayerInfoItemVM() { Key = player.Name, Value = player.Score.ToString() + (hasDeaths ? $"/{(player.ModData is int deaths ? deaths : 0)}" : string.Empty), Team = Status == Models.ConnectionStatus.Connected ? (Models.Team)player.Team : Models.Team.Spectator, Data = player }), (oldItem, newItem) => {
						bool theSame = oldItem.Data is ClientInfo oldPlayer
							&& newItem.Data is ClientInfo newPlayer
							&& oldPlayer.ClientNum >= 0 && newPlayer.ClientNum >= 0
							&& oldPlayer.ClientNum == newPlayer.ClientNum;
						if (theSame) {
							oldItem.Key = newItem.Key;
							oldItem.Value = newItem.Value;
							oldItem.Team = newItem.Team;
							oldItem.Data = newItem.Data;
						}
						return theSame;
					}, (newItem) => {
						return (newItem?.Data as ClientInfo?).GetComparerKey();
					});
				}
				if (SelectedTab == 0) {
					if (tabChanged) {
						AllItems.ReplaceWith(PrimaryInfoItems.Concat(PlayerItems));
					} else {
						AllItems.MergeWith(PrimaryInfoItems.Concat(PlayerItems), (oldItem, newItem) => {
							if (PrimaryInfoItems.Contains(oldItem) || PrimaryInfoItems.Contains(newItem)) {
								return oldItem == newItem;
							} else {
								bool theSame = oldItem is PlayerInfoItemVM
									&& newItem is PlayerInfoItemVM
									&& oldItem.Data is ClientInfo oldPlayer
									&& newItem.Data is ClientInfo newPlayer
									&& oldPlayer.ClientNum >= 0 && newPlayer.ClientNum >= 0
									&& oldPlayer.ClientNum == newPlayer.ClientNum;
								if (theSame) {
									oldItem.Key = newItem.Key;
									oldItem.Value = newItem.Value;
									oldItem.Data = newItem.Data;
									((PlayerInfoItemVM)oldItem).Team = ((PlayerInfoItemVM)newItem).Team;
								}
								return theSame;
							}
						}, (newItem) => {
							if (PrimaryInfoItems.Contains(newItem))
								return (long)int.MaxValue << 3;
							return (newItem?.Data as ClientInfo?).GetComparerKey();
						});
					}
				} else {
					if (tabChanged) {
						AllItems.ReplaceWith(PrimaryInfoItems.Concat(FullInfoItems));
					} else {
						AllItems.MergeWith(PrimaryInfoItems.Concat(FullInfoItems), (oldItem, newItem) => {
							if (PrimaryInfoItems.Contains(oldItem) || PrimaryInfoItems.Contains(newItem)) {
								return oldItem == newItem;
							} else {
								bool theSame = oldItem.Key == newItem.Key;
								if (theSame) {
									oldItem.Value = newItem.Value;
								}
								return theSame;
							}
						});
					}
				}
			});
		}

		private async Task ConnectExecute() {
			if (Status == Models.ConnectionStatus.Disconnected) {
				await NavigationService.Close(this);
				if (loadData || !string.IsNullOrEmpty(this.address))
					await NavigationService.NavigateFromRoot<ChatViewModel, ServerInfoParameter>(new(ServerInfo) { IsFavourite = IsFavourite, Status = Status }, ServerInfo);
			} else {
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Disconnect",
					Message = "Disconnect from this server?",
					CancelText = "No",
					OkText = "Yes",
					OkAction = _ => {
						gameClient?.Disconnect();
						NavigationService.Close(this);
					}
				});
			}
		}

		private void FavouriteExecute() {
			Messenger.Publish(new FavouriteMessage(this, ServerInfo, !IsFavourite));
		}

		private async Task ShareExecute() {
			if (ServerInfo == null)
				return;
			await Share.RequestAsync($"{ServerInfo.HostName.CleanString()}\n/connect {ServerInfo.Address}", $"Connect to {ServerInfo.Version.ToDisplayString()} server");
		}

		private async Task ReportServerExecute() {
			//TODO: copy paste from ServerListViewModel
		}

		public class TabItems(int tab, string tabTitle, IEnumerable<KeyValueItemVM> items) {
			public int TabIndex { get; init; } = tab;
			public string TabTitle { get; init; } = tabTitle;
			public IEnumerable<KeyValueItemVM> Items { get; init; } = items;
		}
	}
}