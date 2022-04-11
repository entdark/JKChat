using System;

using Android.App;
using Android.Util;
using Android.Views;

using JKChat.Android.Controls.Listeners;
using JKChat.Android.Controls.Toolbar;

namespace JKChat.Android.Helpers {
	public static class AndroidExtensions {
		public static int DpToPx(this int dp) {
			return (int)Math.Ceiling(TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Application.Context.Resources.DisplayMetrics));
		}
		public static int DpToPx(this float dp) {
			return (int)Math.Ceiling(TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Application.Context.Resources.DisplayMetrics));
		}
		public static float DpToPxF(this float dp) {
			return TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Application.Context.Resources.DisplayMetrics);
		}
		public static float PxToDp(this int px) {
			return TypedValue.ApplyDimension(ComplexUnitType.Px, px, Application.Context.Resources.DisplayMetrics);
		}
		public static float PxToDp(this float px) {
			return TypedValue.ApplyDimension(ComplexUnitType.Px, px, Application.Context.Resources.DisplayMetrics);
		}

		public static void SetClickAction(this IMenuItem item, Action action) {
			if (item?.ActionView is FadingImageView imageView) {
				imageView.Action = action;
			} else {
				item?.SetOnMenuItemClickListener(new MenuItemClickListener() {
					Click = () => {
						action?.Invoke();
						return true;
					}
				});
			}
		}
		public static void SetVisible(this IMenuItem item, bool visible, bool animated) {
			if (visible || !animated) {
				item?.SetVisible(visible);
			}
			if (item?.ActionView is FadingImageView imageView) {
				imageView.HideShow(visible, animated, () => {
					if (!visible && animated) {
						item?.SetVisible(visible);
					}
				});
			}
		}
	}
}