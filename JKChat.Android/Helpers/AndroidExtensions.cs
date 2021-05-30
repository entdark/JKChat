using Android.App;
using Android.Util;

namespace JKChat.Android.Helpers {
	public static class AndroidExtensions {
		public static int DpToPx(this int dp) {
			return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Application.Context.Resources.DisplayMetrics);
		}
		public static int DpToPx(this float dp) {
			return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Application.Context.Resources.DisplayMetrics);
		}
	}
}