using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Navigation.Parameters;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Chat;
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
		private readonly object serverInfoLocker = new();
		
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
		public MvxObservableCollection<KeyValueItemVM> PlayerItems { get; init; } //first tab items
		public MvxObservableCollection<KeyValueItemVM> FullInfoItems { get; init; } //second tab items
		public TabItems []AllSecondaryItems { get; init; } //array of 2 collections of first and second tab items
		public MvxObservableCollection<KeyValueItemVM> AllItems { get; init; } //first 4 items + either first tab items or second tab items

		public ServerInfoViewModel(IGameClientsService gameClientsService, IServerListService serverListService) {
			this.gameClientsService = gameClientsService;
			this.serverListService = serverListService;
			ConnectCommand = new MvxAsyncCommand(ConnectExecute);
			FavouriteCommand = new MvxCommand(FavouriteExecute);
			ShareCommand = new MvxAsyncCommand(ShareExecute);
			ServerReportCommand = new MvxAsyncCommand(ReportServerExecute);
			PrimaryInfoItems = new(new KeyValueItemVM[]{
				new() { Key = "Game name & version" },
				new() { Key = "Map" },
				new() { Key = "Players online" },
				new() { Key = "Game type" }
			});
			FullInfoItems = new();
			PlayerItems = new();
			AllSecondaryItems = new TabItems[] {
				new(0, "Scoreboard", PlayerItems),
				new(1, "Server info", FullInfoItems)
			};
			AllItems = new(PrimaryInfoItems);
		}

		public override void Prepare(ServerInfoParameter parameter) {
			loadData = parameter.LoadInfo;
//			address = parameter.ServerInfo.Address.ToString();
			Prepare(parameter.ServerInfo, parameter.IsFavourite, parameter.Status, parameter.LoadInfo);
		}

		private void Prepare(ServerInfo serverInfo, bool isFavourite, JKChat.Core.Models.ConnectionStatus status, bool loadData) {
			ServerInfo = serverInfo;
			Status = status;
			IsFavourite = isFavourite;
			if (!loadData) {
				gameClient = gameClientsService.GetOrStartClient(ServerInfo);
				hasDeaths = gameClient.Modification == GameModification.JAPlus;
			}
		}

		public void Init(string address) {
			if (string.IsNullOrEmpty(this.address))
				this.address = address;
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
			if (state.Data.TryGetValue(nameof(this.address), out string loadData))
				_ = bool.TryParse(loadData, out this.loadData);
		}

		protected override Task BackgroundInitialize() {
			return LoadData();
		}

		protected override void OnServerInfoMessage(ServerInfoMessage message) {
			base.OnServerInfoMessage(message);
			if (ServerInfo == message.ServerInfo) {
				Status = message.Status;
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
			else if (data is string s && NetAddress.FromString(s) is { } address)
				return this.ServerInfo.Address != address;
			return true;
		}

		private async Task LoadData() {
			if (!loadData && ServerInfo != null)
				return;
			IsLoading = true;
			try {
				if (ServerInfo == null) {
					if (!string.IsNullOrEmpty(this.address)) {
						var server = await ServerListItemVM.FindExistingOrLoad(this.address);
						if (server == null) {
							await DialogService.ShowAsync(new JKDialogConfig() {
								Title = "Failed to Load",
								Message = $"There is no server with address \"{address}\" {server}",
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
				var serverInfo = await this.serverListService.GetServerInfo(ServerInfo);
				ServerInfo = serverInfo ?? ServerInfo;
			} catch (Exception exception) {
				await ExceptionCallback(exception);
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
					FullInfoItems.MergeWith(ServerInfo.RawInfo?.Select(kv => new KeyValueItemVM() { Key = kv.Key, Value = kv.Value }), (oldItem, newItem) => {
						bool theSame = oldItem.Key == newItem.Key;
						if (theSame) {
							oldItem.Value = newItem.Value;
						}
						return theSame;
					});
				}
				if (ServerInfo.Players != null) {
					PlayerItems.MergeWith(ServerInfo.Players.Select(player => new KeyValueItemVM() { Key = player.Name, Value = player.Score.ToString() + (hasDeaths ? $"/{(player.ModData is int deaths ? deaths : 0)}" : string.Empty), Data = player }), (oldItem, newItem) => {
						bool theSame = oldItem.Data is ServerInfo.PlayerInfo oldPlayer
							&& newItem.Data is ServerInfo.PlayerInfo newPlayer
							&& oldPlayer.ClientNum >= 0 && newPlayer.ClientNum >= 0
							&& oldPlayer.ClientNum == newPlayer.ClientNum;
						if (theSame) {
							oldItem.Key = newItem.Key;
							oldItem.Value = newItem.Value;
							oldItem.Data = newItem.Data;
						}
						return theSame;
					}, (newItem) => {
						return (newItem?.Data as ServerInfo.PlayerInfo)?.Score ?? 0;
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
								bool theSame = oldItem.Data is ServerInfo.PlayerInfo oldPlayer
									&& newItem.Data is ServerInfo.PlayerInfo newPlayer
									&& oldPlayer.ClientNum >= 0 && newPlayer.ClientNum >= 0
									&& oldPlayer.ClientNum == newPlayer.ClientNum;
								if (theSame) {
									oldItem.Key = newItem.Key;
									oldItem.Value = newItem.Value;
									oldItem.Data = newItem.Data;
								}
								return theSame;
							}
						}, (newItem) => {
							if (PrimaryInfoItems.Contains(newItem))
								return int.MaxValue;
							return (newItem?.Data as ServerInfo.PlayerInfo)?.Score ?? 0;
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
			await Share.RequestAsync($"{ColourTextHelper.CleanString(ServerInfo.HostName)}\n/connect {ServerInfo.Address}", $"Connect to {ServerInfo.Version.ToDisplayString()} server");
		}

		private async Task ReportServerExecute() {
			//TODO: copy paste from ServerListViewModel
		}

		public class TabItems {
			public int TabIndex { get; init; }
			public string TabTitle { get; init; }
			public MvxObservableCollection<KeyValueItemVM> Items { get; init; }
			public TabItems(int tab, string tabTitle, MvxObservableCollection<KeyValueItemVM> items) {
				TabIndex = tab;
				TabTitle = tabTitle;
				Items = items;
			}
		}
	}
}