using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Services;

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
					value = value[..31];
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
			get => GetDeserialized<Filter>(null);
			set => SetSerialized(value);
		}
		private static CachedValue<bool> openJKColours;
		public static bool OpenJKColours {
			get => GetCached(false, ref openJKColours, Get);
			set => SetCached(value, ref openJKColours, Set);
		}
		public static AppTheme AppTheme {
			get => (AppTheme)Get((int)AppTheme.Dark);
			set {
				Set((int)value);
				Mvx.IoCProvider.Resolve<IAppService>().AppTheme = value;
			}
		}
		private static CachedValue<int> notificationOptions;
		public static NotificationOptions NotificationOptions {
			get => (NotificationOptions)GetCached((int)NotificationOptions.Default, ref notificationOptions, Get);
			set => SetCached((int)value, ref notificationOptions, Set);
		}
		private static CachedValue<string[]> notificationKeywords;
		public static string []NotificationKeywords {
			get => GetCached(null, ref notificationKeywords, GetDeserialized);
			set => SetCached(value, ref notificationKeywords, SetSerialized);
		}

		public static Dictionary<int, string> ServerMonitorServers {
			get => GetDeserialized(new Dictionary<int, string>());
			set => SetSerialized(value);
		}
		public static WidgetLink WidgetLink {
			get => (WidgetLink)Get((int)WidgetLink.ServerInfo);
			set => Set((int)value);
		}

		private static bool Get(bool defaultValue, [CallerMemberName] string key = "") => Preferences.Get(key, defaultValue);
		private static void Set(bool value, [CallerMemberName] string key = "") => Preferences.Set(key, value);

		private static string Get(string defaultValue, [CallerMemberName] string key = "") => Preferences.Get(key, defaultValue);
		private static void Set(string value, [CallerMemberName] string key = "") => Preferences.Set(key, value);

		private static int Get(int defaultValue, [CallerMemberName] string key = "") => Preferences.Get(key, defaultValue);
		private static void Set(int value, [CallerMemberName] string key = "") => Preferences.Set(key, value);

		private static T GetCached<T>(T defaultValue, ref CachedValue<T> storage, Func<T, string, T> getter, [CallerMemberName] string key = "") {
			if (!storage.Cached) {
				storage.Cached = true;
				storage.Value = getter(defaultValue, key);
			}
			return storage.Value;
		}
		private static void SetCached<T>(T value, ref CachedValue<T> storage, Action<T, string> setter, [CallerMemberName] string key = "") {
			setter(value, key);
			storage.Cached = true;
			storage.Value = value;
		}

		private static T GetDeserialized<T>(T defaultValue, [CallerMemberName] string key = "") {
			string json = Get(null, key);
			if (json == null)
				return defaultValue;
			return json.Deserialize(defaultValue);
		}
		private static void SetSerialized<T>(T value, [CallerMemberName] string key = "") {
			string json = value.Serialize();
			if (json != null)
				Set(json, key);
		}

		private static bool Exists([CallerMemberName] string key = "") => Preferences.ContainsKey(key);

		//cached value is accessed a lot
		private struct CachedValue<T> {
			public bool Cached { get; set; }
			public T Value { get; set; }
		}
	}
}