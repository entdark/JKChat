using JKChat.Core.Models;
using JKChat.Core.Services;
using JKChat.iOS.Helpers;

using UIKit;

namespace JKChat.iOS.Services {
	public class AppService : IAppService {
		public AppTheme AppTheme {
			get {
				if (DeviceInfo.KeyWindow is not {} window)
					return AppTheme.System;
				return window.OverrideUserInterfaceStyle switch {
					UIUserInterfaceStyle.Light => AppTheme.Light,
					UIUserInterfaceStyle.Dark => AppTheme.Dark,
					_ => AppTheme.System
				};
			}
			set {
				if (DeviceInfo.Windows is not {} windows)
					return;
				foreach (var window in windows) {
					window.OverrideUserInterfaceStyle = value switch {
						AppTheme.Light => UIUserInterfaceStyle.Light,
						AppTheme.Dark => UIUserInterfaceStyle.Dark,
						_ => UIUserInterfaceStyle.Unspecified
					};
				}
			}
		}
	}
}