using UIKit;

namespace JKChat.iOS {
	public static partial class Theme {
		public static class Color {
			public static readonly UIColor Accent = UIColor.SystemMint;
			public static readonly UIColor Disconnected = UIColor.SecondaryLabel;
			public static readonly UIColor Connecting = UIColor.Orange;
			public static readonly UIColor Connected = UIColor.Green;
			public static readonly UIColor ChatInfoGradientStart = UIColor.FromRGBA(56, 30, 114, 255);
			public static readonly UIColor ChatInfoGradientEnd = UIColor.FromRGBA(77, 217, 228, 0);
		}
	}
}