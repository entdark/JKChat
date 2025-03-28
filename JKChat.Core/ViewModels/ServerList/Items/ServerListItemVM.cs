﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Navigation;
using JKChat.Core.Navigation.Parameters;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Chat;

using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.ViewModels.ServerList.Items {
	public class ServerListItemVM : SelectableItemVM, IEquatable<ServerListItemVM> {
		public IMvxCommand ConnectCommand { get; init; }

		internal JKClient.ServerInfo ServerInfo { get; private set; }

		private bool needPassword;
		public bool NeedPassword {
			get => needPassword;
			set => SetProperty(ref needPassword, value);
		}

		private string serverName;
		public string ServerName {
			get => serverName;
			set => SetProperty(ref serverName, value, () => {
				CleanServerName = ColourTextHelper.CleanString(value);
			});
		}

		public string CleanServerName { get; private set; }

		private string mapName;
		public string MapName {
			get => mapName;
			set => SetProperty(ref mapName, value);
		}

		private int players;
		public int Players {
			get => players;
			set => SetProperty(ref players, value);
		}

		private int maxPlayers;
		public int MaxPlayers {
			get => maxPlayers;
			set => SetProperty(ref maxPlayers, value);
		}

		private ConnectionStatus status;
		public ConnectionStatus Status {
			get => status;
			set => SetProperty(ref status, value);
		}

		private Game game;
		public Game Game {
			get => game;
			set => SetProperty(ref game, value);
		}

		private string gameName;
		public string GameName {
			get => gameName;
			set => SetProperty(ref gameName, value);
		}

		private string modification;
		public string Modification {
			get => modification;
			set => SetProperty(ref modification, value);
		}

		private string ping;
		public string Ping {
			get => ping;
			set => SetProperty(ref ping, value);
		}

		private bool isFavourite;
		public bool IsFavourite {
			get => isFavourite;
			set => SetProperty(ref isFavourite, value, () => {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Publish(new FavouriteMessage(this, ServerInfo, value));
			});
		}

		public string Address => ServerInfo.Address.ToString();

		public string []PlayersList => ServerInfo.PlayersInfo?.Select(p => p.Name).ToArray();

		public ServerListItemVM() {
			ConnectCommand = new MvxAsyncCommand(ConnectExecute);
		}

		internal ServerListItemVM(JKClient.ServerInfo serverInfo) : this() {
			Set(serverInfo, ConnectionStatus.Disconnected);
		}

		internal void Set(JKClient.ServerInfo serverInfo, ConnectionStatus? status) {
			Set(serverInfo);
			Status = status ?? Status;
		}

		internal void Set(JKClient.ServerInfo serverInfo) {
			ServerInfo = serverInfo;
			Game = serverInfo.Version.ToGame();
			GameName = serverInfo.Version.ToDisplayString();
			NeedPassword = serverInfo.NeedPassword;
			ServerName = serverInfo.HostName;
			MapName = serverInfo.MapName;
			Players = serverInfo.Clients;
			MaxPlayers = serverInfo.MaxClients;
			Modification = serverInfo.GameName;
			Ping = serverInfo.Ping.ToString();
		}

		public void SetFavourite(bool isFavourite, bool silently = true) {
			SetProperty(ref this.isFavourite, isFavourite, nameof(IsFavourite));
		}

		private async Task ConnectExecute() {
			if (Status == ConnectionStatus.Disconnected) {
				await Mvx.IoCProvider.Resolve<INavigationService>().NavigateFromRoot<ChatViewModel, ServerInfoParameter>(new(this), this.ServerInfo);
			} else {
				Mvx.IoCProvider.Resolve<IGameClientsService>().GetClient(ServerInfo)?.Disconnect();
			}
		}

		public Task Connect() {
			return ConnectExecute();
		}

		public static bool operator ==(ServerListItemVM server1, ServerListItemVM server2) {
			return server1?.ServerInfo == server2?.ServerInfo;
		}

		public static bool operator !=(ServerListItemVM server1, ServerListItemVM server2) {
			return (server1 == server2) != true;
		}

		public bool Equals(ServerListItemVM other) {
			if (other is null)
				return false;
			return this.ServerInfo == other.ServerInfo;
		}

		public override bool Equals(object obj) {
			return Equals(obj as ServerListItemVM);
		}

		public override int GetHashCode() {
			return this.ServerInfo.GetHashCode();
		}

		public async Task<bool> Refresh() {
			try {
				var serverInfo = await Mvx.IoCProvider.Resolve<IServerListService>().GetServerInfo(ServerInfo);
				if (serverInfo != null) {
					Set(serverInfo);
					await Mvx.IoCProvider.Resolve<ICacheService>().UpdateServer(this);
					return true;
				}
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			return false;
		}

		public static async Task<ServerListItemVM> FindExistingOrLoad(string address, bool silently = false, bool load = true, bool preferGameClient = false) {
			ServerListItemVM server = null;
			bool success = silently ? await task() : await Common.ExceptionalTaskRun(task);
			return server;
			async Task<bool> task() {
				var netAddress = JKClient.NetAddress.FromString(address);
				var gameClient = Mvx.IoCProvider.Resolve<IGameClientsService>().GetClient(netAddress);
				if (preferGameClient/* && gameClient?.Status != ConnectionStatus.Disconnected*/) {
					server = gameClient != null ? new ServerListItemVM(gameClient.ServerInfo) : null;
					return server != null;
				}
				server = await Mvx.IoCProvider.Resolve<ICacheService>().GetCachedServer(netAddress);
				if (server != null) {
					return true;
				} else if (gameClient != null) {
					server = new ServerListItemVM(gameClient.ServerInfo);
					return true;
				} else if (load) {
					var serverInfo = await Mvx.IoCProvider.Resolve<IServerListService>().GetServerInfo(netAddress);
					if (serverInfo != null) {
						server = new ServerListItemVM(serverInfo);
						return true;
					}
				}
				return false;
			}
		}

		public sealed class Comparer : EqualityComparer<ServerListItemVM> {
			public override bool Equals(ServerListItemVM x, ServerListItemVM y) {
				return x?.ServerInfo == y?.ServerInfo;
			}
			public override int GetHashCode(ServerListItemVM obj) {
				return obj?.ServerInfo.GetHashCode() ?? 0;
			}
		}
	}
}