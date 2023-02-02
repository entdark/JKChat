using UIKit;

namespace JKChat.iOS {
	public static partial class Theme {
		public static class Color {
			public static readonly UIColor Accent = UIColor.FromRGB(148, 236, 233);
			public static readonly UIColor Background = UIColor.FromRGB(34, 45, 102);
			public static readonly UIColor NavigationBar = UIColor.FromRGB(50, 53, 68);
            public static readonly UIColor NavigationBarButton = UIColor.FromRGB(200, 200, 200);
            public static readonly UIColor Title = Accent;
			public static readonly UIColor Subtitle = UIColor.FromRGB(130, 130, 130);
			public static readonly UIColor TabBar = UIColor.FromRGB(50, 53, 68);
			public static readonly UIColor TabBarItemUnselected = UIColor.FromRGB(161, 161, 161);
			public static readonly UIColor TabBarItemSelected = Accent;
			public static readonly UIColor LoadingBackground = UIColor.FromRGBA(34, 45, 102, 64);
			public static readonly UIColor Disconnected = UIColor.FromRGB(181, 37, 37);
			public static readonly UIColor Connecting = UIColor.FromRGB(236, 163, 20);
			public static readonly UIColor Connected = UIColor.FromRGB(149, 220, 33);
			public static readonly UIColor Placeholder = UIColor.FromRGB(130, 130, 130);
			public static readonly UIColor DialogSelection = UIColor.FromRGBA(255, 173, 0, 140);
			public static readonly UIColor DialogSeparator = Accent.ColorWithAlpha(0.0f);
			public static readonly UIColor ChatInfoGradientStart = UIColor.FromRGBA(0, 255, 255, 66);
			public static readonly UIColor ChatInfoGradientEnd = UIColor.FromRGBA(0, 255, 255, 0);
		}
	}
}