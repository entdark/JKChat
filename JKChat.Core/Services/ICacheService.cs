using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using JKChat.Core.ViewModels.ServerList.Items;

namespace JKChat.Core.Services {
	public interface ICacheService {
		Task SaveRecentServer(ServerListItemVM server);
		Task SaveRecentServers(IList<ServerListItemVM> servers);
		Task<IEnumerable<ServerListItemVM>> LoadRecentServers();
		Task UpdateRecentServer(ServerListItemVM server);
		Task AddReportedServer(ServerListItemVM server);
		Task<IEnumerable<ServerListItemVM>> LoadReportedServers();
	}
}
