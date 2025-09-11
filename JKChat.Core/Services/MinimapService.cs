using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Models;

using JKClient;

using Microsoft.Maui.Storage;

using MvvmCross;

namespace JKChat.Core.Services;

public class MinimapService : IMinimapService {
	private readonly ConcurrentDictionary<string, MapProgressData> mapProgresses = new(StringComparer.FromComparison(StringComparison.OrdinalIgnoreCase));

	public MinimapService() {
	}

	MapData IMinimapService.GetMapData(ServerInfo serverInfo) {
		var mapData = GetEmbeddedMapData(serverInfo);
		return mapData ?? GetInternalMapData(serverInfo);
	}

	private MapData GetEmbeddedMapData(ServerInfo serverInfo) {
		return (this as IMinimapService).GetEmbeddedMapData(serverInfo);
	}
	MapData IMinimapService.GetEmbeddedMapData(ServerInfo serverInfo) {
		string mapName = serverInfo.MapName;
//special case for mappers who cannot properly recreate a map
//		const string siege_hoth = "siege_hoth";
//		if (mapName.StartsWith(siege_hoth) && mapName.Length > siege_hoth.Length && mapName[siege_hoth.Length] is char c && char.IsDigit(c) && (c - '0') >= 3)
//			resourceMapName = "mp/siege_hoth2";
		var assembly = this.GetType().Assembly;
		string path = $"JKChat.Core.Resources.Minimaps.{serverInfo.Version.ToGame()}.{mapName.Replace('/','.').Replace('-', '_')}.xy,";
		foreach (var resourceName in assembly.GetManifestResourceNames()) {
			if (resourceName.StartsWith(path, StringComparison.OrdinalIgnoreCase)) {
				var mapData = ParseMapData(resourceName, path.Length, serverInfo, assembly);
				if (mapData != null) {
					return mapData;
				}
			}
		}
		return null;
	}

	private MapData GetInternalMapData(ServerInfo serverInfo) {
		return (this as IMinimapService).GetInternalMapData(serverInfo);
	}
	MapData IMinimapService.GetInternalMapData(ServerInfo serverInfo) {
		if (IsMapProgressActive(serverInfo, out _))
			return null;

		string minimapPath = GetInternalPath(serverInfo);
		string path = Path.Combine(minimapPath, serverInfo.MapName);
		if (new DirectoryInfo(path) is { Exists: true } d && d.GetFiles() is { Length: > 0 } files && Path.GetExtension(files[0].FullName) == ".png") {
			var mapData = ParseMapData(files[0].FullName, path.Length+4/*/xy,*/, serverInfo);
			if (mapData != null) {
				return mapData;
			}
		}
		return null;
	}

	MapProgressData IMinimapService.GetActiveMapProgress(ServerInfo serverInfo) {
		string mapName = serverInfo.MapName;
		if (IsMapProgressActive(serverInfo, out var mapProgress)) {
			return mapProgress;
		}
		return null;
	}

	MapProgressData IMinimapService.DownloadMapAndGenerateMinimap(ServerInfo serverInfo, bool forceReport) {
		string mapName = serverInfo.MapName;
		if (IsMapProgressActive(serverInfo, out var mapProgress)) {
			return mapProgress;
		}
		mapProgress?.Cancel();
		mapProgress = mapProgresses[mapName] = new() {
			MapName = mapName,
			IsDownloading = true
		};
		bool report = forceReport;
		string tempFilePath = null;

		downloadAndGenerate();

		void downloadAndGenerate(string requestUri = null) {
			Task.Run(async () => {
				if (requestUri == null) {
					string dlURL = serverInfo[AppSettings.MinimapServerInfoKeyDownloadURL];
					string baseUrl = !string.IsNullOrEmpty(dlURL) ? dlURL : AppSettings.MinimapDownloadURL;
					string cleanMapName = mapName.Contains('/') ? mapName[(mapName.LastIndexOf('/') + 1)..] : mapName;
					requestUri = Path.Combine(baseUrl, cleanMapName+".pk3");
				}

				var fileName = mapName + ".pk3";
				var tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
				var pathDirectoryName = Path.GetDirectoryName(tempFilePath);
			
				mapProgress.IsDownloading = true;

				setProgress(0.02f);

				using var client = new HttpClient();
				using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, mapProgress.CancellationToken).ConfigureAwait(false);
				setProgress(0.05f);
				response.EnsureSuccessStatusCode();

//if we reached here then we found something valid to download so notify the user about any exception
				report = true;
				long totalSize = response.Content.Headers.ContentLength ?? long.MaxValue;
				using var contentStream = await response.Content.ReadAsStreamAsync(mapProgress.CancellationToken).ConfigureAwait(false);
				setProgress(0.1f);

				byte []buffer = new byte[8192];
				Directory.CreateDirectory(pathDirectoryName);
				if (File.Exists(tempFilePath)) {
					File.Delete(tempFilePath);
				}
				using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true)) {
					int totalRead = 0;
					do {
						int read = await contentStream.ReadAsync(buffer, mapProgress.CancellationToken).ConfigureAwait(false);
						if (read == 0) {
							break;
						}
						totalRead += read;
						await fileStream.WriteAsync(buffer.AsMemory(0, read), mapProgress.CancellationToken).ConfigureAwait(false);
						setProgress((float)((double)totalRead / totalSize), 0.1f, 0.2f);
					} while (true);
				}
				mapProgress.CancellationToken.ThrowIfCancellationRequested();
				
				mapProgress.IsGenerating = true;
				mapProgress.IsDownloading = false;

				var minimapPath = GetInternalPath(serverInfo);
				var minimapMapPath = Path.Combine(minimapPath, mapName);
				if (new DirectoryInfo(minimapMapPath) is { Exists: true } d && d.GetFiles() is { Length: > 0 } files && Path.GetExtension(files[0].FullName) == ".png") {
					File.Delete(files[0].FullName);
				}
				var stopwatch = Stopwatch.StartNew();
				long lastProgressTime = stopwatch.ElapsedMilliseconds;
				Q3MinimapGenerator.Helpers.GenerateMiniMap(pathDirectoryName, new() {
					OutputFolderPath = minimapPath,
					MaxWidth = AppSettings.MinimapSize,
					MaxHeight = AppSettings.MinimapSize,
					ExtraBorderUnits = 10,
					AxisPlane = Q3MinimapGenerator.MiniMapAxisPlane.XY,
					ImageType = Q3MinimapGenerator.ImageType.GrayscaleA,
					Predicate = (foundName, _) => {
						return mapName.Equals(foundName, StringComparison.OrdinalIgnoreCase);
					},
					ProgressCallback = p => {
						long currentTime = stopwatch.ElapsedMilliseconds;
						if (currentTime-lastProgressTime < 8L)
							return;
						lastProgressTime = currentTime;
						setProgress(p, 0.2f, 1.0f);
					},
					MakeMeta = false,
					ImageFilePathFormatter = (path, axisName, meta) => {
						return Path.Combine(path, $"{axisName},{meta}.png");
					},
					CancellationToken = mapProgress.CancellationToken
				}, true);
				mapProgress.IsGenerating = false;
				setProgress(1.0f);
			}).ContinueWith(t => {
				if (tempFilePath != null && File.Exists(tempFilePath)) {
					File.Delete(tempFilePath);
				}
				bool manualDownloading = forceReport && mapProgress.IsDownloading;
				mapProgress.IsDownloading = false;
				mapProgress.IsGenerating = false;
				if (t.IsFaulted) {
					setProgress(0.0f);
					if (!mapProgress.CancellationToken.IsCancellationRequested) {
						if (manualDownloading) {
							Mvx.IoCProvider.Resolve<IDialogService>().Show(new JKDialogConfig() {
								Title = "Failed to download",
								Message = $"Would you like to provide a direct URL to the map \"{mapName}\" PK3?",
								Input = new(),
								OkText = "Download",
								OkAction = config => {
									string directUri = config.Input?.Text;
									if (!string.IsNullOrEmpty(directUri)) {
										forceReport = false;
										downloadAndGenerate(directUri);
									}
								},
								CancelText = "Cancel",
							});
						} else if(report) {
							Helpers.Common.ExceptionCallback(t.Exception);
						}
					} else {
						Debug.WriteLine(t.Exception);
					}
				}
			});
		}

		void setProgress(float p, float min = 0.0f, float max = 1.0f) {
			if (p != 0.0f || min != 0.0f || max != 1.0f) {
				mapProgress.CancellationToken.ThrowIfCancellationRequested();
			}
			p = Math.Clamp(p, 0.0f, 1.0f);
			mapProgress.Progress = min+(max-min)*p;
		}
		return mapProgress;
	}

	private bool IsMapProgressActive(ServerInfo serverInfo, out MapProgressData mapProgress) {
		return mapProgresses.TryGetValue(serverInfo.MapName, out mapProgress) && mapProgress.IsActive;
	}
	bool IMinimapService.IsMapProgressActive(ServerInfo serverInfo) {
		return IsMapProgressActive(serverInfo, out _);
	}

	private bool DeleteMinimap(ServerInfo serverInfo) {
		return (this as IMinimapService).DeleteMinimap(serverInfo);
	}
	bool IMinimapService.DeleteMinimap(ServerInfo serverInfo) {
		if (IsMapProgressActive(serverInfo, out _))
			return false;

		string minimapPath = GetInternalPath(serverInfo);
		string path = Path.Combine(minimapPath, serverInfo.MapName);
		if (new DirectoryInfo(path) is { Exists: true } d && d.GetFiles() is { Length: > 0 } files && Path.GetExtension(files[0].FullName) == ".png") {
			File.Delete(files[0].FullName);
			return true;
		}
		return false;
	}

	IEnumerable<KeyValuePair<string, MapProgressData>> IMinimapService.GetActiveMapProgresses() {
		return mapProgresses.Where(kvp => kvp.Value.IsActive);
	}

	private static MapData ParseMapData(string path, int minMaxStart, ServerInfo serverInfo, Assembly assembly = null) {
		string []minMax = path.Substring(minMaxStart).Replace(".png","").Split(',');
		if (minMax?.Length == 6
			&& int.TryParse(minMax[0], out int minX)
			&& int.TryParse(minMax[1], out int minY)
			&& int.TryParse(minMax[2], out int minZ)
			&& int.TryParse(minMax[3], out int maxX)
			&& int.TryParse(minMax[4], out int maxY)
			&& int.TryParse(minMax[5], out int maxZ)) {
			return new() {
				Assembly = assembly,
				Path = path,
				Min = new(minX, minY, minZ),
				Max = new(maxX, maxY, maxZ),
				HasShadow = serverInfo.Version == ClientVersion.JO_v1_02
			};
		}
		return null;
	}

	private static string GetInternalPath(ServerInfo serverInfo) {
		return Path.Combine(FileSystem.AppDataDirectory, "Minimaps", serverInfo.Version.ToGame().ToString());
	}
}

internal class MapProgressData {
	private readonly CancellationTokenSource CTS = new();
	public CancellationToken CancellationToken => CTS.Token;
	public string MapName { get; init; }
	private float progress;
	public float Progress {
		get => progress;
		set {
			progress = value;
			ProgressChanged?.Invoke(this);
		}
	}
	public bool IsDownloading { get; set; }
	public bool IsGenerating { get; set; }
	public bool IsActive => IsDownloading || IsGenerating || Progress.IsProgressActive();
	public event Action<MapProgressData> ProgressChanged;
	public void Cancel() {
		CTS?.Cancel();
		Progress = 0.0f;
	}
	public void OfferCancel() {
		if (!IsActive)
			return;

		Mvx.IoCProvider.Resolve<IDialogService>().Show(new JKDialogConfig() {
			Title = "Minimap creation",
			Message = this.IsDownloading ? $"Downloading \"{MapName}\", would you like to cancel?" : $"Generating \"{MapName}\" minimap, would you like to stop?",
			OkText = this.IsDownloading ? "Cancel" : "Stop",
			OkAction = _ => {
				this?.Cancel();
			},
			CancelText = "Continue",
		});
	}
}