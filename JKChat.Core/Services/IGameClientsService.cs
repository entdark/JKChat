using System.Collections.Generic;

using JKChat.Core.Models;

namespace JKChat.Core.Services {
	public interface IGameClientsService {
		int ActiveClients { get; }
		int UnreadMessages { get; }
		GameClient GetOrStartClient(JKClient.ServerInfo serverInfo);
		GameClient GetClient(JKClient.NetAddress address);
		IEnumerable<JKClient.NetAddress> AddressesWithStatus(ConnectionStatus status, bool without = false);
		void DisconnectFromAll();
		void ShutdownAll();
	}
}