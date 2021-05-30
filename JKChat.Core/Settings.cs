using System;

using Xamarin.Essentials;

namespace JKChat.Core {
	public static class Settings {
		public const string DefaultName = "^5Jedi Knight";
		public static bool FirstLaunch {
			get {
				bool firstLaunch = Preferences.Get(nameof(FirstLaunch), true);
				if (firstLaunch) {
					Preferences.Set(nameof(FirstLaunch), false);
				}
				return firstLaunch;
			}
			set => Preferences.Set(nameof(FirstLaunch), value);
		}
		public static string PlayerName {
			get => Preferences.Get(nameof(PlayerName), DefaultName);
			set => Preferences.Set(nameof(PlayerName), value);
		}
		private static readonly Guid DefaultPlayerId = Guid.NewGuid();
		public static Guid PlayerId {
			get {
				if (!Preferences.ContainsKey(nameof(PlayerId))) {
					Preferences.Set(nameof(PlayerId), DefaultPlayerId.ToString());
					return DefaultPlayerId;
				}
				return Guid.TryParse(Preferences.Get(nameof(PlayerId), DefaultPlayerId.ToString()), out Guid playerId) ? playerId : DefaultPlayerId;
			}
			set => Preferences.Set(nameof(PlayerId), value.ToString());
		}
	}
}
