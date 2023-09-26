using UIKit;

namespace JKChat.iOS {
	public static partial class Theme {
		public static class Color {
			public static readonly UIColor Accent = UIColor.SystemMint;
			public static readonly UIColor Background = UIColor.FromRGB(34, 45, 102);
			public static readonly UIColor Bar = UIColor.FromRGB(50, 53, 68);
			public static readonly UIColor NavigationBar = Bar;
			public static readonly UIColor NavigationBarButton = UIColor.FromRGB(200, 200, 200);
			public static readonly UIColor Title = Accent;
			public static readonly UIColor Subtitle = UIColor.FromRGB(130, 130, 130);
			public static readonly UIColor TabBar = Bar;
			public static readonly UIColor TabBarItemUnselected = UIColor.FromRGB(161, 161, 161);
			public static readonly UIColor TabBarItemSelected = Accent;
			public static readonly UIColor LoadingBackground = UIColor.FromRGBA(34, 45, 102, 64);
			public static readonly UIColor Disconnected = UIColor.SecondaryLabel;
			public static readonly UIColor Connecting = UIColor.Orange;
			public static readonly UIColor Connected = UIColor.Green;
			public static readonly UIColor Placeholder = UIColor.FromRGB(130, 130, 130);
			public static readonly UIColor DialogSelection = UIColor.FromRGBA(255, 173, 0, 140);
			public static readonly UIColor DialogSeparator = Accent.ColorWithAlpha(0.0f);
			public static readonly UIColor ChatInfoGradientStart = UIColor.FromRGBA(56, 30, 114, 255);
			public static readonly UIColor ChatInfoGradientEnd = UIColor.FromRGBA(77, 217, 228, 0);
			public static readonly UIColor Toolbar = Bar;
		}
	}
}