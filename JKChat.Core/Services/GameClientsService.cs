using System;
using System.Collections.Generic;
using System.Linq;

using JKChat.Core.Models;

namespace JKChat.Core.Services {
	internal class GameClientsService : IGameClientsService {
		private readonly Dictionary<JKClient.NetAddress, GameClient> clients = new(new JKClient.NetAddressComparer());

		public int ActiveClients => clients.Count(kv => kv.Value.Status != ConnectionStatus.Disconnected);

		public int UnreadMessages => clients.Sum(kv => kv.Value.UnreadMessages);

		public GameClient GetClient(JKClient.ServerInfo serverInfo, bool startNew = false) {
			var address = serverInfo.Address;
			var client = GetClient(address);
			if (client == null && startNew) {
				return (clients[address] = new GameClient(serverInfo));
			}
			return client;
		}

		public GameClient GetClient(JKClient.NetAddress address) {
			clients.TryGetValue(address, out var client);
			return client;
		}

		public IEnumerable<JKClient.NetAddress> AddressesWithStatus(ConnectionStatus status, bool without = false) {
			if (clients.Count <= 0) {
				return null;
			}
			Func<ConnectionStatus, bool> condition;
			if (!without) {
				condition = (s) => s == status;
			} else {
				condition = (s) => s != status;
			}
			return clients.Where(kv => condition(kv.Value.Status)).Select(kv => kv.Key);
		}

		public void DisconnectFromAll() {
			foreach (var client in clients) {
				client.Value.Disconnect(true);
			}
		}

		public void ShutdownAll() {
			foreach (var client in clients) {
				client.Value.Shutdown();
			}
			clients.Clear();
		}
	}
}