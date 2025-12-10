using System;

using Android.Views;

using AndroidX.Core.View;

using Google.Android.Material.Internal;

using JKChat.Android.Views.Base;

using MvvmCross;
using MvvmCross.Platforms.Android;

namespace JKChat.Android.Controls.Listeners;

public class OnApplyWindowInsetsListener : Java.Lang.Object, ViewUtils.IOnApplyWindowInsetsListener {
	private readonly WindowInsetsFlags flags;

	private ViewGroup.MarginLayoutParams initialLayoutParameters;

	public OnApplyWindowInsetsListener(WindowInsetsFlags flags) {
		this.flags = flags;
	}

	public WindowInsetsCompat OnApplyWindowInsets(View view, WindowInsetsCompat insetsCompat, ViewUtils.RelativePadding initialPadding) {
		bool isExpanded = (Mvx.IoCProvider.Resolve<IMvxAndroidCurrentTopActivity>().Activity as IBaseActivity)?.ExpandedWindow ?? false;
		bool paddingTop = flags.HasFlag(WindowInsetsFlags.PaddingTop) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingTopButExpanded)) || (isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingTopWhenExpanded));
		bool paddingBottom = flags.HasFlag(WindowInsetsFlags.PaddingBottom) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingBottomButExpanded)) || (isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingBottomWhenExpanded));
		bool paddingLeft = flags.HasFlag(WindowInsetsFlags.PaddingLeft) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingLeftButExpanded)) || (isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingLeftWhenExpanded));
		bool paddingRight = flags.HasFlag(WindowInsetsFlags.PaddingRight) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingRightButExpanded)) || (isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingRightWhenExpanded));

		bool isRtl = view.LayoutDirection == LayoutDirection.Rtl;
		var insets = insetsCompat.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());
		int insetTop = insets.Top;
		int insetBottom = insets.Bottom;
		int insetLeft = insets.Left;
		int insetRight = insets.Right;

		initialPadding.Top += paddingTop ? insetTop : 0;
		initialPadding.Bottom += paddingBottom ? insetBottom : 0;
		int systemWindowInsetLeft = paddingLeft ? insetLeft : 0;
		int systemWindowInsetRight = paddingRight ? insetRight : 0;
		initialPadding.Start += isRtl ? systemWindowInsetRight : systemWindowInsetLeft;
		initialPadding.End += isRtl ? systemWindowInsetLeft : systemWindowInsetRight;
		initialPadding.ApplyToView(view);

		if (view.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters && initialLayoutParameters == null) {
			initialLayoutParameters = new ViewGroup.MarginLayoutParams(marginLayoutParameters);
		}

		if (initialLayoutParameters != null) {
			bool marginTop = flags.HasFlag(WindowInsetsFlags.MarginTop);
			bool marginBottom = flags.HasFlag(WindowInsetsFlags.MarginBottom);
			bool marginLeft = flags.HasFlag(WindowInsetsFlags.MarginLeft);
			bool marginRight = flags.HasFlag(WindowInsetsFlags.MarginRight);

			if (marginTop || marginBottom || marginLeft || marginRight) {
				var newLayoutParameters = view.LayoutParameters as ViewGroup.MarginLayoutParams;
				newLayoutParameters.TopMargin = initialLayoutParameters.TopMargin + (marginTop ? insetTop : 0);
				newLayoutParameters.BottomMargin = initialLayoutParameters.BottomMargin + (marginBottom ? insetBottom : 0);
				newLayoutParameters.LeftMargin = initialLayoutParameters.LeftMargin + (marginLeft ? insetLeft : 0);
				newLayoutParameters.RightMargin = initialLayoutParameters.RightMargin + (marginRight ? insetRight : 0);
				view.LayoutParameters = newLayoutParameters;
			}
		}

		return insetsCompat;
	}
}

[Flags]
public enum WindowInsetsFlags {
	None						= 0,

	PaddingLeft					= 1 << 0,
	PaddingRight				= 1 << 1,
	PaddingTop					= 1 << 2,
	PaddingBottom				= 1 << 3,

	PaddingLeftButExpanded		= 1 << 4,
	PaddingRightButExpanded		= 1 << 5,
	PaddingTopButExpanded		= 1 << 6,
	PaddingBottomButExpanded	= 1 << 7,

	PaddingLeftWhenExpanded		= 1 << 8,
	PaddingRightWhenExpanded	= 1 << 9,
	PaddingTopWhenExpanded		= 1 << 10,
	PaddingBottomWhenExpanded	= 1 << 11,

	MarginLeft					= 1 << 12,
	MarginRight					= 1 << 13,
	MarginTop					= 1 << 14,
	MarginBottom				= 1 << 15
}