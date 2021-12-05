using CoreGraphics;

using Foundation;

using UIKit;

namespace JKChat.iOS.Helpers {
	public static class iOSExtensions {
		public static T iPhoneX<T>(this T v, T v2) {
			return DeviceInfo.iPhoneX ? v2 : v;
		}

		public static TView FindView<TView>(this UIView view) where TView : UIView {
			if (view == null) {
				return null;
			}
			var subviews = view.Subviews;
			if (subviews == null) {
				return null;
			}
			foreach (var subview in subviews) {
				if (subview is TView tview) {
					return tview;
				} else {
					var subsubview = subview.FindView<TView>();
					if (subsubview != null) {
						return subsubview;
					}
				}
			}
			return null;
		}

		public static void GetKeyboardUserInfo(this NSNotification notification, out double duration, out UIViewAnimationOptions animationOptions,
												out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame) {
			var userInfo = notification.UserInfo;
			duration = (userInfo?.ObjectForKey(UIKeyboard.AnimationDurationUserInfoKey) as NSNumber)?.DoubleValue ?? 0.0;
			UIViewAnimationCurve curve = (UIViewAnimationCurve)(int)((userInfo?.ObjectForKey(UIKeyboard.AnimationCurveUserInfoKey) as NSNumber)?.NIntValue ?? 0);
			endKeyboardFrame = (userInfo?.ObjectForKey(UIKeyboard.FrameEndUserInfoKey) as NSValue)?.CGRectValue ?? CGRect.Empty;
			beginKeyboardFrame = (userInfo?.ObjectForKey(UIKeyboard.FrameBeginUserInfoKey) as NSValue)?.CGRectValue ?? CGRect.Empty;
			switch (curve) {
			case UIViewAnimationCurve.Linear:
				animationOptions = UIViewAnimationOptions.CurveLinear;
				break;
			case UIViewAnimationCurve.EaseIn:
				animationOptions = UIViewAnimationOptions.CurveEaseIn;
				break;
			case UIViewAnimationCurve.EaseInOut:
				animationOptions = UIViewAnimationOptions.CurveEaseInOut;
				break;
			default:
			case UIViewAnimationCurve.EaseOut:
				animationOptions = UIViewAnimationOptions.CurveEaseOut;
				break;
			}
		}
	}
}