using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Helpers;

using JKClient;

namespace JKChat.Core.Services {
	internal class ServerListService : IServerListService {
		private readonly ServerBrowser []serverBrowsers;
		private IEnumerable<ServerInfo> servers;

		public ServerListService() {
			serverBrowsers = new ServerBrowser[] {
				new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol25)),
				new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol26)),
				new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol15)),
				new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol16)),
				new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol68)),
				new ServerBrowser(ServerBrowser.GetKnownBrowserHandler(ProtocolVersion.Protocol71))
			};
			foreach (var serverBrowser in serverBrowsers) {
				serverBrowser.Start(Helpers.Common.ExceptionCallback, true);
			}
		}

		public async Task<IEnumerable<ServerInfo>> GetCurrentList() {
			if (this.servers == null || !this.servers.Any()) {
				return await GetNewList();
			}
			return this.servers;
		}

		public async Task<IEnumerable<ServerInfo>> GetNewList() {
			var getNewListTasks = serverBrowsers.Select(s => s.GetNewList());
			this.servers = (await Task.WhenAll(getNewListTasks)).SelectMany(t => t).Distinct(new ServerInfoComparer());
			return this.servers;
		}

		public async Task<IEnumerable<ServerInfo>> RefreshList() {
			var refreshListTasks = serverBrowsers.Select(s => s.RefreshList());
			this.servers = (await Task.WhenAll(refreshListTasks)).SelectMany(t => t).Distinct(new ServerInfoComparer());
			return this.servers;
		}

		public async Task<IEnumerable<ServerInfo>> RefreshList(IEnumerable<ServerInfo> serverInfos) {
			var getServerInfoTasks = serverInfos.Select(GetServerInfo);
			var servers = (await Task.WhenAll(getServerInfoTasks)).Where(s => s != null).Distinct(new ServerInfoComparer());
			return servers;
		}

		public async Task<ServerInfo> GetServerInfo(string address, ushort port, ProtocolVersion protocol) {
			return await GetServerInfo(NetAddress.FromString(address, port), (int)protocol);
		}
		public async Task<ServerInfo> GetServerInfo(string address, ushort port, int protocol = 0) {
			return await GetServerInfo(NetAddress.FromString(address, port), protocol);
		}
		public async Task<ServerInfo> GetServerInfo(NetAddress address, ProtocolVersion protocol) {
			return await GetServerInfo(address, (int)protocol);
		}
		public async Task<ServerInfo> GetServerInfo(NetAddress address, int protocol = 0) {
			if (protocol > 0) {
				var serverBrowser = serverBrowsers.FirstOrDefault(s => s.Protocol == protocol);
				return await serverBrowser?.GetServerInfo(address).ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null, TaskContinuationOptions.None);
			}
			var serverInfoTasks = serverBrowsers.Select(s => s.GetServerInfo(address));
			var serverInfoTask = await serverInfoTasks.WhenAny(t => t.Status == TaskStatus.RanToCompletion);
			return serverInfoTask?.Result;
		}
		public async Task<ServerInfo> GetServerInfo(ServerInfo serverInfo) {
			return await GetServerInfo(serverInfo.Address, serverInfo.Protocol);
		}
	}
}