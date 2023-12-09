using CoreGraphics;

using Foundation;

using UIKit;

namespace JKChat.iOS.Helpers {
	public static class iOSExtensions {
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