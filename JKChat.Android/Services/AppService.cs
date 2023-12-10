using AndroidX.AppCompat.App;

using JKChat.Core.Models;
using JKChat.Core.Services;

namespace JKChat.Android.Services {
	public class AppService : IAppService {
		public AppTheme AppTheme {
			get {
				return AppCompatDelegate.DefaultNightMode switch {
					AppCompatDelegate.ModeNightNo => AppTheme.Light,
					AppCompatDelegate.ModeNightYes => AppTheme.Dark,
					_ => AppTheme.System
				};
			}
			set {
				AppCompatDelegate.DefaultNightMode = value switch {
					AppTheme.Light => AppCompatDelegate.ModeNightNo,
					AppTheme.Dark => AppCompatDelegate.ModeNightYes,
					_ => AppCompatDelegate.ModeNightFollowSystem
				};
			}
		}
	}
}