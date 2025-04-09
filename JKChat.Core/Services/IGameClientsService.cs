using System.Collections.Generic;

using JKChat.Core.Models;
using JKChat.Core.ViewModels.ServerList.Items;

namespace JKChat.Core.Services {
	public interface IGameClientsService {
		IEnumerable<ServerListItemVM> ActiveServers { get; }
		int UnreadMessages { get; }
		internal ConnectionStatus? GetStatus(JKClient.ServerInfo serverInfo);
		internal ConnectionStatus? GetStatus(JKClient.NetAddress address);
		ConnectionStatus? GetStatus(string address);
		internal GameClient GetClient(JKClient.ServerInfo serverInfo, bool startNew = false);
		internal GameClient GetClient(JKClient.NetAddress address);
		internal IEnumerable<JKClient.ServerInfo> ServerInfosWithStatus(ConnectionStatus status, bool without = false);
		void DisconnectFromAll();
		void ShutdownAll();
	}
}