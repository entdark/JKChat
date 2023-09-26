using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

using JKChat.Core.Messages;
using JKChat.Core.Models;

using Microsoft.Maui.Storage;

using MvvmCross;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Core {
	public static class AppSettings {
		public const string DefaultName = "^5Jedi Knight";
		public static bool FirstLaunch {
			get {
				bool firstLaunch = Get(true);
				if (firstLaunch) {
					Set(false);
				}
				return firstLaunch;
			}
			set => Set(value);
		}
		public static string PlayerName {
			get => Get(DefaultName);
			set {
				if (value == Get(DefaultName)) {
					return;
				}
				if (string.IsNullOrEmpty(value)) {
					value = AppSettings.DefaultName;
				} else if (value.Length > 31) {
					value = value.Substring(0, 31);
				}
				Set(value);
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Publish(new PlayerNameMessage(value));
			}
		}
		private static readonly Guid DefaultPlayerId = Guid.NewGuid();
		public static Guid PlayerId {
			get {
				if (!Exists()) {
					Set(DefaultPlayerId.ToString());
					return DefaultPlayerId;
				}
				return Guid.TryParse(Get(DefaultPlayerId.ToString()), out Guid playerId) ? playerId : DefaultPlayerId;
			}
			set => Set(value.ToString());
		}
		public static int EncodingId {
			get => Get(1);
			set => Set(value);
		}
		public static bool LocationUpdate {
			get => Get(false);
			set {
				Set(value);
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Publish(new LocationUpdateMessage(value));
			}
		}
		public static Filter Filter {
			get {
				string filter = Get(null);
				if (filter == null)
					return null;
				try {
					return JsonSerializer.Deserialize<Filter>(filter);
				} catch (Exception exception) {
					Debug.WriteLine(exception);
				}
				return null;
			}
			set {
				try {
					Set(JsonSerializer.Serialize(value));
				} catch (Exception exception) {
					Debug.WriteLine(exception);
				}
			}
		}

		private static bool Get(bool defaultValue, [CallerMemberName] string key = "") => Preferences.Get(key, defaultValue);
		private static void Set(bool value, [CallerMemberName] string key = "") => Preferences.Set(key, value);

		private static string Get(string defaultValue, [CallerMemberName] string key = "") => Preferences.Get(key, defaultValue);
		private static void Set(string value, [CallerMemberName] string key = "") => Preferences.Set(key, value);

		private static int Get(int defaultValue, [CallerMemberName] string key = "") => Preferences.Get(key, defaultValue);
		private static void Set(int value, [CallerMemberName] string key = "") => Preferences.Set(key, value);

		private static bool Exists([CallerMemberName] string key = "") => Preferences.ContainsKey(key);
	}
}