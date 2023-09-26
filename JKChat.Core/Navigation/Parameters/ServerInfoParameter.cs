using JKChat.Core.Models;
using JKChat.Core.ViewModels.ServerList.Items;

namespace JKChat.Core.Navigation.Parameters {
	public class ServerInfoParameter {
		internal JKClient.ServerInfo ServerInfo { get; init; }
		internal ConnectionStatus Status { get; init; }
		public bool LoadInfo { get; init; }
		public bool IsFavourite { get; init; }

		public ServerInfoParameter(ServerListItemVM server) : this(server.ServerInfo) {
			Status = server.Status;
			IsFavourite = server.IsFavourite;
		}
		internal ServerInfoParameter(JKClient.ServerInfo serverInfo) {
			ServerInfo = serverInfo;
		}
	}
}