using System;

using JKChat.Core.Messages;

using MvvmCross;
using MvvmCross.Plugin.Messenger;

using Xamarin.Essentials;

namespace JKChat.Core {
	public static class AppSettings {
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
			set {
				if (value == Preferences.Get(nameof(PlayerName), DefaultName)) {
					return;
				}
				if (string.IsNullOrEmpty(value)) {
					value = AppSettings.DefaultName;
				} else if (value.Length > 31) {
					value = value.Substring(0, 31);
				}
				Preferences.Set(nameof(PlayerName), value);
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Publish(new PlayerNameMessage(value));
			}
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
		public static int EncodingId {
			get => Preferences.Get(nameof(EncodingId), 1);
			set => Preferences.Set(nameof(EncodingId), value);
		}
		public static bool LocationUpdate {
			get => Preferences.Get(nameof(LocationUpdate), false);
			set {
				Preferences.Set(nameof(LocationUpdate), value);
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Publish(new LocationUpdateMessage(value));
			}
		}
	}
}
