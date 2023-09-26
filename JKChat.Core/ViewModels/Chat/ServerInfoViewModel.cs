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

using JKClient;

using Microsoft.Maui.ApplicationModel.DataTransfer;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Chat {
	public class ServerInfoViewModel : BaseServerViewModel<ServerInfoParameter> {
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;
		private readonly IServerListService serverListService;
		private readonly object serverInfoLocker = new();

		private bool loadData;

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

		public List<KeyValueItemVM> PrimaryInfoItems { get; init; }
		public LimitedObservableCollection<KeyValueItemVM> FullInfoItems { get; init; }
		public LimitedObservableCollection<KeyValueItemVM> PlayerItems { get; init; }
		public TabItems []AllSecondaryItems { get; init; }
		public LimitedObservableCollection<KeyValueItemVM> AllItems { get; init; }

		public ServerInfoViewModel(ICacheService cacheService, IGameClientsService gameClientsService, IServerListService serverListService) {
			this.cacheService = cacheService;
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
			FullInfoItems = new(512);
			PlayerItems = new(512);
			AllSecondaryItems = new TabItems[] {
				new(0, PlayerItems),
				new(1, FullInfoItems)
			};
			AllItems = new(512);
			AllItems.ReplaceWith(PrimaryInfoItems);
		}

		public override void Prepare(ServerInfoParameter parameter) {
			ServerInfo = parameter.ServerInfo;
			Status = parameter.Status;
			IsFavourite = parameter.IsFavourite;
			loadData = parameter.LoadInfo;
		}

		protected override async Task BackgroundInitialize() {
			if (loadData) {
				await LoadData();
			}
		}

		protected override void OnServerInfoMessage(ServerInfoMessage message) {
			base.OnServerInfoMessage(message);
			if (ServerInfo.Address == message.ServerInfo.Address) {
				Status = message.Status;
				ServerInfo = message.ServerInfo;
			}
		}

		protected override void OnFavouriteMessage(FavouriteMessage message) {
			base.OnFavouriteMessage(message);
			if (ServerInfo.Address == message.ServerInfo.Address) {
				IsFavourite = message.IsFavourite;
			}
		}

		private async Task LoadData() {
			IsLoading = true;
			var serverInfo = await this.serverListService.GetServerInfo(ServerInfo);
			ServerInfo = serverInfo ?? ServerInfo;
			IsLoading = false;
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
					FullInfoItems.ReplaceWith<int>(ServerInfo.RawInfo?.Select(kv => new KeyValueItemVM() { Key = kv.Key, Value = kv.Value }), (oldItem, newItem) => {
						bool theSame = oldItem.Key == newItem.Key;
						if (theSame) {
							oldItem.Value = newItem.Value;
						}
						return theSame;
					});
				}
				if (ServerInfo.Players != null) {
					PlayerItems.ReplaceWith(ServerInfo.Players.Select(player => new KeyValueItemVM() { Key = player.Name, Value = player.Score.ToString(), Data = player }), (oldItem, newItem) => {
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
						AllItems.ReplaceWith(PrimaryInfoItems.Concat(PlayerItems), (oldItem, newItem) => {
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
						AllItems.ReplaceWith<int>(PrimaryInfoItems.Concat(FullInfoItems), (oldItem, newItem) => {
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
				if (loadData)
					await NavigationService.NavigateFromRoot<ChatViewModel, ServerInfoParameter>(new(ServerInfo) { IsFavourite = IsFavourite, Status = Status });
			} else {
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Disconnect",
					Message = "Disconnect from this server?",
					CancelText = "No",
					OkText = "Yes",
					OkAction = _ => {
						gameClientsService.GetOrStartClient(ServerInfo).Disconnect();
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
			public int Tab { get; init; }
			public MvxObservableCollection<KeyValueItemVM> Items { get; init; }
			public TabItems(int tab, MvxObservableCollection<KeyValueItemVM> items) {
				Tab = tab;
				Items = items;
			}
		}
	}
}