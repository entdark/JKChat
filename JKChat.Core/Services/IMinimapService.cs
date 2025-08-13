using System.Collections.Generic;

using JKChat.Core.Models;

using JKClient;

namespace JKChat.Core.Services;

public interface IMinimapService {
	internal MapData GetMapData(ServerInfo serverInfo);
	internal MapData GetEmbeddedMapData(ServerInfo serverInfo);
	internal MapData GetInternalMapData(ServerInfo serverInfo);
	internal MapProgressData DownloadMapAndGenerateMinimap(ServerInfo serverInfo, bool forceReport = false);
	internal bool IsMapProgressActive(ServerInfo serverInfo);
	internal bool DeleteMinimap(ServerInfo serverInfo);
	internal MapProgressData GetActiveMapProgress(ServerInfo serverInfo);
	internal IEnumerable<KeyValuePair<string, MapProgressData>> GetActiveMapProgresses();
}