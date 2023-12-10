using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using JKChat.Core.Helpers;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.ViewModels;

namespace JKChat.Core.Models {
	public class Filter : MvxNotifyPropertyChanged {
		private const bool ShowFullDefault = true;
		private const bool ShowEmptyDefault = true;
		private const int GameDefault = ClientVersionExtensions.ClientVersionAll;
		private const int GameTypeDefault = GameTypeExtensions.GameTypeAll;

		private bool showFull;
		public bool ShowFull {
			get => showFull;
			set => SetProperty(ref showFull, value);
		}

		private bool showEmpty;
		public bool ShowEmpty {
			get => showEmpty;
			set => SetProperty(ref showEmpty, value);
		}

		private int game;
		public int Game {
			get => game;
			set => SetProperty(ref game, value);
		}

		private int gameType;
		public int GameType {
			get => gameType;
			set => SetProperty(ref gameType, value);
		}

		private Dictionary<string, bool> gameMod;
		public Dictionary<string, bool> GameMod {
			get => gameMod;
			set => SetProperty(ref gameMod, value);
		}

		public Filter() {
			GameMod = new(StringComparer.InvariantCultureIgnoreCase) {
				[string.Empty] = true
			};
			Reset();
			PropertyChanged += FilterPropertyChanged;
		}

		private void FilterPropertyChanged(object sender, PropertyChangedEventArgs ev) {
			AppSettings.Filter = this;
		}

		public void Reset() {
			showFull = ShowFullDefault;
			showEmpty = ShowEmptyDefault;
			game = GameDefault;
			gameType = GameTypeDefault;
			ResetGameMod(true);
			RaiseAllPropertiesChanged();
		}

		public bool IsReset =>
			ShowFull == ShowFullDefault
			&& ShowEmpty == ShowEmptyDefault
			&& Game == GameDefault
			&& GameType == GameTypeDefault
			&& (GameMod.Count <= 0 || GameMod.All(gm => gm.Value));

		public void AddGameMods(IEnumerable<string> gameMods) {
			var uniqueGameMods = gameMods
				.Where(item => !string.IsNullOrEmpty(item))
				.Distinct(StringComparer.InvariantCultureIgnoreCase)
				.Except(GameMod.Keys, StringComparer.InvariantCultureIgnoreCase);
			foreach (var uniqueGameMod in uniqueGameMods) {
				GameMod.TryAdd(uniqueGameMod, true);
			}
			RaisePropertyChanged(nameof(GameMod));
		}

		public void SetGameMods(IEnumerable<KeyValuePair<string, bool>> gameMods) {
			foreach (var gameMod in gameMods) {
				if (GameMod.TryGetValue(gameMod.Key, out bool value) && value != gameMod.Value) {
					GameMod[gameMod.Key] = gameMod.Value;
				}
			}
			RaisePropertyChanged(nameof(GameMod));
		}

		public void ResetGameMod(bool silently) {
			foreach (var gameMod in GameMod) {
				if (GameMod.TryGetValue(gameMod.Key, out bool value) && !value) {
					GameMod[gameMod.Key] = true;
				}
			}
			if (!silently) {
				RaisePropertyChanged(nameof(GameMod));
			}
		}

		public IEnumerable<ServerListItemVM> Apply(IEnumerable<ServerListItemVM> items) {
			return IsReset ? items : items.Where(item => {
				var serverInfo = item.ServerInfo;
				if (!ShowFull && serverInfo.Clients == serverInfo.MaxClients)
					return false;
				if (!ShowEmpty && serverInfo.Clients == 0)
					return false;
				if (!Game.HasField(serverInfo.Version))
					return false;
				if (!GameType.HasField(serverInfo.GameType))
					return false;
				if (GameMod.Count > 0) {
					if (!string.IsNullOrEmpty(serverInfo.GameName)) {
						if (GameMod.TryGetValue(serverInfo.GameName, out bool value) && !value)
							return false;
					} else {
						if (GameMod.TryGetValue(string.Empty, out bool value) && !value)
							return false;
					}
				}
				return true;
			});
		}
	}
}