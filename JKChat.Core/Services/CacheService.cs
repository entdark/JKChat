using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.ViewModels.ServerList.Items;
using JKChat.Core.Messages;

using JKClient;

using Microsoft.Maui.Storage;

using MvvmCross;
using MvvmCross.Plugin.Messenger;

using SQLite;

namespace JKChat.Core.Services {
	public class CacheService : ICacheService {
		private readonly SQLiteAsyncConnection connection;
		private readonly MvxSubscriptionToken serverInfoMessageToken, favouriteMessageToken;

		public CacheService() {
			var path = Path.Combine(FileSystem.AppDataDirectory, "jkchat.db3");
			connection = new SQLiteAsyncConnection(path);
			connection.CreateTableAsync<CachedServer>().Wait();
			var messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
			serverInfoMessageToken = messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			favouriteMessageToken = messenger.Subscribe<FavouriteMessage>(OnFavouriteMessage);
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			Task.Run(updateServerInfoTask);
			Task updateServerInfoTask() => UpdateServer(message.ServerInfo);
		}

		private void OnFavouriteMessage(FavouriteMessage message) {
			Task.Run(updateFavouriteTask);
			async Task updateFavouriteTask() {
				var cachedServer = await GetCachedServerAsync(message.ServerInfo);
				cachedServer ??= new CachedServer(message.ServerInfo);
				cachedServer.IsFavourite = message.IsFavourite;
				await connection.InsertOrReplaceAsync(cachedServer);
			}
		}

		public async Task SaveRecentServer(ServerListItemVM server) {
			await (this as ICacheService).SaveRecentServer(server.ServerInfo);
		}

		async Task ICacheService.SaveRecentServer(ServerInfo serverInfo) {
			var cachedServer = await GetCachedServerAsync(serverInfo);
			cachedServer ??= new CachedServer(serverInfo);
			cachedServer.LastConnected = DateTime.UtcNow;
			await connection.InsertOrReplaceAsync(cachedServer);
		}

		public async Task SaveRecentServers(IList<ServerListItemVM> servers) {
			if (servers.Count <= 0) {
				return;
			}
			foreach (var server in servers) {
				await SaveRecentServer(server);
			}
		}

		public async Task UpdateServer(ServerListItemVM server) {
			await UpdateServer(server.ServerInfo);
		}

		internal async Task UpdateServer(ServerInfo serverInfo) {
			var cachedServer = await GetCachedServerAsync(serverInfo);
			if (cachedServer != null) {
				cachedServer.Update(serverInfo);
				await connection.UpdateAsync(cachedServer);
			}
		}

		public async Task<IEnumerable<ServerListItemVM>> LoadRecentServers() {
			return await LoadCachedServers(server => server.IsRecent);
		}

		public async Task AddReportedServer(ServerListItemVM server) {
			var cachedServer = await GetCachedServerAsync(server.ServerInfo);
			cachedServer ??= new CachedServer(server);
			cachedServer.IsReported = true;
			await connection.InsertOrReplaceAsync(cachedServer);
		}

		public async Task<IEnumerable<ServerListItemVM>> LoadReportedServers() {
			return await LoadCachedServers(server => server.IsReported);
		}

		public async Task AddFavouriteServer(ServerListItemVM server) {
			var cachedServer = await GetCachedServerAsync(server.ServerInfo);
			cachedServer ??= new CachedServer(server);
			cachedServer.IsFavourite = true;
			await connection.InsertOrReplaceAsync(cachedServer);
		}

		public async Task<IEnumerable<ServerListItemVM>> LoadFavouriteServers() {
			return await LoadCachedServers(server => server.IsFavourite);
		}

		private async Task<IEnumerable<ServerListItemVM>> LoadCachedServers(Func<CachedServer, bool> predicate) {
			return (await connection.Table<CachedServer>().ToArrayAsync()).Where(predicate)
				.Select(recentServer => recentServer.ToServerVM());
		}

		private async Task<T> GetAsync<T>(object pk) where T : new() {
			try {
				return await connection.GetAsync<T>(pk);
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			return default;
		}

		private async Task<CachedServer> GetCachedServerAsync(ServerInfo serverInfo) {
			return await GetAsync<CachedServer>(serverInfo.Address.ToString());
		}

		private class CachedServer {
			[PrimaryKey]
			public string Address { get; set; }
			public string ServerName { get; set; }
			public string MapName { get; set; }
			public int Players { get; set; }
			public int MaxPlayers { get; set; }
			public bool NeedPassword { get; set; }
			public ProtocolVersion Protocol { get; set; }
			public ClientVersion Version { get; set; }
			public string Modification { get; set; }
			public DateTime LastConnected { get; set; }
			public bool IsReported { get; set; }
			public bool IsFavourite { get; set; }
			[Ignore]
			public bool IsRecent => LastConnected != default;

			public CachedServer() {}
			public CachedServer(ServerListItemVM server) : this(server.ServerInfo) {
				IsFavourite = server.IsFavourite;
			}
			public CachedServer(ServerInfo serverInfo) {
				Set(serverInfo);
			}
			public void Update(ServerListItemVM server) {
				Set(server.ServerInfo);
				IsFavourite = server.IsFavourite;
			}
			public void Update(ServerInfo serverInfo) {
				Set(serverInfo);
			}
			private void Set(ServerInfo serverInfo) {
				Address = serverInfo.Address.ToString();
				ServerName = serverInfo.HostName;
				MapName = serverInfo.MapName;
				Players = serverInfo.Clients;
				MaxPlayers = serverInfo.MaxClients;
				NeedPassword = serverInfo.NeedPassword;
				Protocol = serverInfo.Protocol;
				Version = serverInfo.Version;
				Modification = serverInfo.GameName;
			}

			public ServerListItemVM ToServerVM() {
				var server = new ServerListItemVM(new ServerInfo(NetAddress.FromString(this.Address)) {
					HostName = this.ServerName,
					MapName = this.MapName,
					Clients = this.Players,
					MaxClients = this.MaxPlayers,
					NeedPassword = this.NeedPassword,
					Protocol = this.Protocol,
					Version = this.Version,
					GameName = this.Modification
				});
				server.SetFavourite(this.IsFavourite);
				return server;
			}
		}
	}
}