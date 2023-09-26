using System;
using System.Globalization;
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
		internal JKClient.ServerInfo ServerInfo { get; init; }
		public IMvxCommand ConnectCommand { get; init; }

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

		private string players;
		public string Players {
			get => players;
			set => SetProperty(ref players, value);
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

		private bool isFavourite;
		public bool IsFavourite {
			get => isFavourite;
			set => SetProperty(ref isFavourite, value, () => {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Publish(new FavouriteMessage(this, ServerInfo, value));
			});
		}

		public ServerListItemVM() {
			ConnectCommand = new MvxAsyncCommand(ConnectExecute);
		}

		internal ServerListItemVM(JKClient.ServerInfo serverInfo) : this() {
			ServerInfo = serverInfo;
			Game = serverInfo.Version.ToGame();
			GameName = serverInfo.Version.ToDisplayString();
			Set(serverInfo, ConnectionStatus.Disconnected);
		}

		internal void Set(JKClient.ServerInfo serverInfo, ConnectionStatus status) {
			Set(serverInfo);
			Status = status;
		}

		internal void Set(JKClient.ServerInfo serverInfo) {
			NeedPassword = serverInfo.NeedPassword;
			ServerName = serverInfo.HostName;
			MapName = serverInfo.MapName;
			Players = $"{serverInfo.Clients.ToString(CultureInfo.InvariantCulture)}/{serverInfo.MaxClients.ToString(CultureInfo.InvariantCulture)}";
			Modification = serverInfo.GameName;
		}

		public void SetFavourite(bool isFavourite, bool silently = true) {
			SetProperty(ref this.isFavourite, isFavourite, nameof(IsFavourite));
		}

		private async Task ConnectExecute() {
			if (Status == ConnectionStatus.Disconnected) {
				await Mvx.IoCProvider.Resolve<INavigationService>().NavigateFromRoot<ChatViewModel, ServerInfoParameter>(new(this), viewModel => {
					return (viewModel as ChatViewModel)?.ServerInfo != this.ServerInfo;
				});
			} else {
				Mvx.IoCProvider.Resolve<IGameClientsService>().GetOrStartClient(ServerInfo).Disconnect();
			}
		}

		public bool Equals(ServerListItemVM other) {
			return this.ServerInfo.Address == other.ServerInfo.Address;
		}

		public override int GetHashCode() {
			return this.ServerInfo.Address.GetHashCode();
		}
	}
}