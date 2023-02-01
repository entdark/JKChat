using System.Collections.Generic;
using System.Threading.Tasks;

using JKClient;

namespace JKChat.Core.Services {
	public interface IServerListService {
		Task<IEnumerable<ServerInfo>> GetCurrentList();
		Task<IEnumerable<ServerInfo>> GetNewList();
		Task<IEnumerable<ServerInfo>> RefreshList();
		Task<InfoString> GetServerInfo(string address, ushort port, ProtocolVersion protocol);
		Task<InfoString> GetServerInfo(string address, ushort port, int protocol = 0);
		Task<InfoString> GetServerInfo(NetAddress address, ProtocolVersion protocol);
		Task<InfoString> GetServerInfo(NetAddress address, int protocol = 0);
		Task<InfoString> GetServerInfo(ServerInfo serverInfo);
	}
}
