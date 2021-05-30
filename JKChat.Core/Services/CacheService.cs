using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JKChat.Core.ViewModels.ServerList.Items;

using JKClient;

using SQLite;

namespace JKChat.Core.Services {
	public class CacheService : ICacheService {
		private readonly SQLiteAsyncConnection connection;

		public CacheService() {
			var path = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, "jkchat.db3");
			connection = new SQLiteAsyncConnection(path);
			connection.CreateTableAsync<RecentServer>().Wait();
		}

		public async Task SaveRecentServer(ServerListItemVM server) {
			var recentServer = new RecentServer(server);
			await connection.InsertOrReplaceAsync(recentServer);
		}

		public async Task SaveRecentServers(IList<ServerListItemVM> servers) {
			if (servers.Count <= 0) {
				return;
			}
			foreach (var server in servers) {
				await SaveRecentServer(server);
			}
		}

		public async Task<ICollection<ServerListItemVM>> LoadRecentServers() {
			return (await connection.Table<RecentServer>().ToArrayAsync())
				.OrderBy(recentServer => recentServer.LastConnected)
				.Select(recentServer => recentServer.ToServerVM())
				.ToArray();
		}

		private class RecentServer {
			[PrimaryKey]
			public string Address { get; set; }
			public bool NeedPassword { get; set; }
			public string ServerName { get; set; }
			public string MapName { get; set; }
			public string Players { get; set; }
			public string Ping { get; set; }
			public string GameType { get; set; }
			public DateTime LastConnected { get; set; }
			public ProtocolVersion Protocol { get; set; }

			public RecentServer() {}
			public RecentServer(ServerListItemVM server) {
				Address = server.ServerInfo.Address.ToString();
				NeedPassword = server.NeedPassword;
				ServerName = server.ServerName;
				MapName = server.MapName;
				Players = server.Players;
				Ping = server.Ping;
				GameType = server.GameType;
				LastConnected = DateTime.UtcNow;
				Protocol = server.ServerInfo.Protocol;
			}

			public ServerListItemVM ToServerVM() {
				string []players = null;
				if (!string.IsNullOrEmpty(this.Players)) {
					players = this.Players.Split('/');
				}
				if (players == null) {
					players = new string [2]{ string.Empty, string.Empty };
				}
				return new ServerListItemVM(new ServerInfo() {
					Address = NetAddress.FromString(this.Address),
					NeedPassword = this.NeedPassword,
					HostName = this.ServerName,
					MapName = this.MapName,
					Clients = int.TryParse(players[0], out int clients) ? clients : 0,
					MaxClients = int.TryParse(players[1], out int maxClients) ? maxClients : 0,
					Ping = int.TryParse(this.Ping, out int ping) ? ping : 0,
					Protocol = this.Protocol,
					GameType = ServerListItemVM.GetGameType(this.GameType)
				});
			}
		}
	}
}
