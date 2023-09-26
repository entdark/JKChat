using System.Collections.Generic;
using System.Threading.Tasks;

using JKClient;

namespace JKChat.Core.Services {
	public interface IServerListService {
		Task<IEnumerable<ServerInfo>> GetCurrentList();
		Task<IEnumerable<ServerInfo>> GetNewList();
		Task<IEnumerable<ServerInfo>> RefreshList();
		Task<IEnumerable<ServerInfo>> RefreshList(IEnumerable<ServerInfo> serverInfos);
		Task<ServerInfo> GetServerInfo(string address, ushort port, ProtocolVersion protocol);
		Task<ServerInfo> GetServerInfo(string address, ushort port, int protocol = 0);
		Task<ServerInfo> GetServerInfo(NetAddress address, ProtocolVersion protocol);
		Task<ServerInfo> GetServerInfo(NetAddress address, int protocol = 0);
		Task<ServerInfo> GetServerInfo(ServerInfo serverInfo);
	}
}