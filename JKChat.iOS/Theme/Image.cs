using UIKit;

namespace JKChat.iOS {
	public static partial class Theme {
		public static class Image {
			public static readonly UIImage SquareAndArrowUp = UIImage.GetSystemImage("square.and.arrow.up");
			public static readonly UIImage InfoCircle = UIImage.GetSystemImage("info.circle");
			public static readonly UIImage StarFill = UIImage.GetSystemImage("star.fill");
			public static readonly UIImage Star = UIImage.GetSystemImage("star");
			public static readonly UIImage DoorLeftHandOpen = UIImage.GetSystemImage("door.left.hand.open");
			public static readonly UIImage CircleFill_Caption1Small = UIImage.GetSystemImage("circle.fill", UIImageSymbolConfiguration.Create(UIFontTextStyle.Caption1, UIImageSymbolScale.Small));
			public static readonly UIImage Person3Fill_Small = UIImage.GetSystemImage("person.3.fill", UIImageSymbolConfiguration.Create(UIImageSymbolScale.Small));
			public static readonly UIImage Person2Fill_Small = UIImage.GetSystemImage("person.2.fill", UIImageSymbolConfiguration.Create(UIImageSymbolScale.Small));
			public static readonly UIImage PersonFill_Small = UIImage.GetSystemImage("person.fill", UIImageSymbolConfiguration.Create(UIImageSymbolScale.Small));
			public static readonly UIImage Lock_Medium = UIImage.GetSystemImage("lock", UIImageSymbolConfiguration.Create(UIImageSymbolScale.Medium));
			public static readonly UIImage JAPreviewBackground = UIImage.FromBundle("JAPreviewBackground");
			public static readonly UIImage JOPreviewBackground = UIImage.FromBundle("JOPreviewBackground");
			public static readonly UIImage Q3PreviewBackground = UIImage.FromBundle("Q3PreviewBackground");
			public static readonly UIImage EllipsisCircle = UIImage.GetSystemImage("ellipsis.circle");
			public static readonly UIImage Line3HorizontalDecreaseCircle = UIImage.GetSystemImage("line.3.horizontal.decrease.circle");
			public static readonly UIImage Line3HorizontalDecreaseCircleFill = UIImage.GetSystemImage("line.3.horizontal.decrease.circle.fill");
			public static readonly UIImage PlusCircle = UIImage.GetSystemImage("plus.circle");
		}
	}
}