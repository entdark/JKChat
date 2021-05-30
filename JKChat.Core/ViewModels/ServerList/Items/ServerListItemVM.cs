using JKChat.Core.Models;

using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.ServerList.Items {
	public class ServerListItemVM : MvxNotifyPropertyChanged {
		internal JKClient.ServerInfo ServerInfo { get; private set; }

		private bool needPassword;
		public bool NeedPassword {
			get => needPassword;
			set => SetProperty(ref needPassword, value);
		}

		private string serverName;
		public string ServerName {
			get => serverName;
			set => SetProperty(ref serverName, value);
		}

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

		private string ping;
		public string Ping {
			get => ping;
			set => SetProperty(ref ping, value);
		}

		private string gameType;
		public string GameType {
			get => gameType;
			set => SetProperty(ref gameType, value);
		}

		private ConnectionStatus status;
		public ConnectionStatus Status {
			get => status;
			set => SetProperty(ref status, value);
		}

		public ServerListItemVM() {}

		internal ServerListItemVM(JKClient.ServerInfo serverInfo) {
			ServerInfo = serverInfo;
			Set(serverInfo, ConnectionStatus.Disconnected);
		}

		internal void Set(JKClient.ServerInfo serverInfo, ConnectionStatus status) {
			NeedPassword = serverInfo.NeedPassword;
			ServerName = serverInfo.HostName;
			MapName = serverInfo.MapName;
			Players = $"{serverInfo.Clients}/{serverInfo.MaxClients}";
			if (serverInfo.Ping != 0) {
				Ping = serverInfo.Ping.ToString();
			}
			GameType = GetGameType(serverInfo.GameType);
			Status = status;
		}

		private static string GetGameType(JKClient.GameType gameType) {
			switch (gameType) {
			default:
			case JKClient.GameType.FFA:
				return "Free For All";
			case JKClient.GameType.Holocron:
				return "Holocron";
			case JKClient.GameType.JediMaster:
				return "Jedi Master";
			case JKClient.GameType.Duel:
				return "Duel";
			case JKClient.GameType.PowerDuel:
				return "Power Duel";
			case JKClient.GameType.SinglePlayer:
				return "Single Player";
			case JKClient.GameType.Team:
				return "Team FFA";
			case JKClient.GameType.Siege:
				return "Siege";
			case JKClient.GameType.CTF:
				return "Capture the Flag";
			case JKClient.GameType.CTY:
				return "CTY";
			}
		}

		public static JKClient.GameType GetGameType(string gameType) {
			switch (gameType) {
			default:
			case "Free For All":
				return JKClient.GameType.FFA;
			case "Holocron":
				return JKClient.GameType.Holocron;
			case "Jedi Master":
				return JKClient.GameType.JediMaster;
			case "Duel":
				return JKClient.GameType.Duel;
			case "Power Duel":
				return JKClient.GameType.PowerDuel;
			case "Single Player":
				return JKClient.GameType.SinglePlayer;
			case "Team FFA":
				return JKClient.GameType.Team;
			case "Siege":
				return JKClient.GameType.Siege;
			case "Capture the Flag":
				return JKClient.GameType.CTF;
			case "CTY":
				return JKClient.GameType.CTY;
			}
		}
	}
}
