
using UIKit;

namespace JKChat.iOS {
	public static partial class Theme {
		public static class Font {
			public static UIFont ANewHope(float size) => UIFont.FromName("A New Hope", size);
			public static UIFont ErgoeRegular(float size) => UIFont.FromName("Ergoe", size);
			public static UIFont ErgoeMedium(float size) => UIFont.FromName("ErgoeMedium", size);
			public static UIFont ErgoeBold(float size) => UIFont.FromDescriptor(new UIFontDescriptor(new UIFontAttributes() { Name = "Ergoe" }).CreateWithTraits(UIFontDescriptorSymbolicTraits.Bold), size);
			public static UIFont Arial(float size) => UIFont.FromName("Arial", size);
			public static UIFont OCRAStd(float size) => UIFont.FromName("OCR A Std", size);
		}
	}
}