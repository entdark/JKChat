using System.Linq;

using CoreGraphics;

using Foundation;

using JKChat.iOS.Presenter;

using MvvmCross;

using UIKit;

namespace JKChat.iOS.Helpers {
	public static class DeviceInfo {
		public static UIWindow KeyWindow => UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
			? UIApplication.SharedApplication.ConnectedScenes?.OfType<UIWindowScene>().FirstOrDefault(s => s.KeyWindow != null)?.KeyWindow
			: UIApplication.SharedApplication.KeyWindow;
		public static UIWindow []Windows => UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
			? UIApplication.SharedApplication.ConnectedScenes?.OfType<UIWindowScene>().SelectMany(s => s.Windows).ToArray()
			: UIApplication.SharedApplication.Windows;
		public static CGRect ScreenBounds => KeyWindow?.Bounds ?? UIScreen.MainScreen.Bounds;
		public static UIEdgeInsets SafeAreaInsets => UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			&& KeyWindow?.SafeAreaInsets is UIEdgeInsets safeAreaInsets
			? safeAreaInsets
			: UIEdgeInsets.Zero;
		public static bool IsCollapsed => Mvx.IoCProvider.Resolve<IViewPresenter>().IsCollapsed;
		public static bool IsRunningOnMacOS => (UIDevice.CurrentDevice.CheckSystemVersion(14, 0) && NSProcessInfo.ProcessInfo.IsiOSApplicationOnMac) || NSProcessInfo.ProcessInfo.IsMacCatalystApplication;
	}
}