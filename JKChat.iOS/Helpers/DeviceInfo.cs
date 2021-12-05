using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CoreGraphics;

using Foundation;

using UIKit;

namespace JKChat.iOS.Helpers {
	public static class DeviceInfo {
		public static CGRect ScreenBounds => UIScreen.MainScreen.Bounds;
		public static UIEdgeInsets SafeAreaInsets => UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
			&& UIApplication.SharedApplication.Windows?[0]?.SafeAreaInsets is UIEdgeInsets safeAreaInsets
			? safeAreaInsets
			: UIEdgeInsets.Zero;
		public static bool IsPortrait => UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.Portrait || UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.PortraitUpsideDown;
	}
}