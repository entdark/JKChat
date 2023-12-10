using System.Collections.Generic;
using System.Threading.Tasks;

using JKChat.Core.ViewModels.ServerList.Items;

using JKClient;

namespace JKChat.Core.Services {
	public interface ICacheService {
		Task SaveRecentServer(ServerListItemVM server);
		internal Task SaveRecentServer(ServerInfo serverInfo);
		Task SaveRecentServers(IList<ServerListItemVM> servers);
		Task UpdateServer(ServerListItemVM server);
		Task<IEnumerable<ServerListItemVM>> LoadRecentServers();
		Task AddReportedServer(ServerListItemVM server);
		Task<IEnumerable<ServerListItemVM>> LoadReportedServers();
		Task AddFavouriteServer(ServerListItemVM server);
		Task<IEnumerable<ServerListItemVM>> LoadFavouriteServers();
		Task<ServerListItemVM> GetCachedServer(ServerInfo serverInfo);
		Task<ServerListItemVM> GetCachedServer(NetAddress address);
	}
}