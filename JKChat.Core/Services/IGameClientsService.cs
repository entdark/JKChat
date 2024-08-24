using System.Collections.Generic;

using JKChat.Core.Models;

namespace JKChat.Core.Services {
	public interface IGameClientsService {
		int ActiveClients { get; }
		int UnreadMessages { get; }
		internal GameClient GetClient(JKClient.ServerInfo serverInfo, bool startNew = false);
		internal GameClient GetClient(JKClient.NetAddress address);
		internal IEnumerable<JKClient.NetAddress> AddressesWithStatus(ConnectionStatus status, bool without = false);
		void DisconnectFromAll();
		void ShutdownAll();
	}
}