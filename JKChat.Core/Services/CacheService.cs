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

using Microsoft.Maui.Storage;

namespace JKChat.Core.Services {
	public class CacheService : ICacheService {
		private readonly SQLiteAsyncConnection connection;

		public CacheService() {
			var path = Path.Combine(FileSystem.AppDataDirectory, "jkchat.db3");
			connection = new SQLiteAsyncConnection(path);
			connection.CreateTableAsync<RecentServer>().Wait();
			connection.CreateTableAsync<ReportedServer>().Wait();
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

		public async Task UpdateRecentServer(ServerListItemVM server) {
			var recentServer = await connection.GetAsync<RecentServer>(server.ServerInfo.Address.ToString());
			await connection.UpdateAsync(recentServer);
		}

		public async Task<IEnumerable<ServerListItemVM>> LoadRecentServers() {
			return (await connection.Table<RecentServer>().ToArrayAsync())
				.OrderBy(recentServer => recentServer.LastConnected)
				.Select(recentServer => recentServer.ToServerVM());
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
					Clients = int.TryParse(players[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int clients) ? clients : 0,
					MaxClients = int.TryParse(players[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxClients) ? maxClients : 0,
					Ping = int.TryParse(this.Ping, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ping) ? ping : 0,
					Protocol = this.Protocol,
					GameType = ServerListItemVM.GetGameType(this.GameType)
				});
			}
		}

		public async Task AddReportedServer(ServerListItemVM server) {
			var reportedServer = new ReportedServer(server);
			await connection.InsertOrReplaceAsync(reportedServer);
		}

		public async Task<IEnumerable<ServerListItemVM>> LoadReportedServers() {
			return (await connection.Table<ReportedServer>().ToArrayAsync())
				.OrderBy(recentServer => recentServer.AddedTime)
				.Select(recentServer => recentServer.ToServerVM());
		}

		private class ReportedServer {
			[PrimaryKey]
			public string Address { get; set; }
			public string ServerName { get; set; }
			public DateTime AddedTime { get; set; }

			public ReportedServer() {}
			public ReportedServer(ServerListItemVM server) {
				Address = server.ServerInfo.Address.ToString();
				ServerName = server.ServerName;
				AddedTime = DateTime.UtcNow;
			}

			public ServerListItemVM ToServerVM() {
				return new ServerListItemVM(new ServerInfo() {
					Address = NetAddress.FromString(this.Address),
					HostName = this.ServerName
				});
			}
		}
	}
}
