using System.Collections.Generic;
using System.Linq;

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
			animationOptions = curve switch {
				UIViewAnimationCurve.Linear => UIViewAnimationOptions.CurveLinear,
				UIViewAnimationCurve.EaseIn => UIViewAnimationOptions.CurveEaseIn,
				UIViewAnimationCurve.EaseInOut => UIViewAnimationOptions.CurveEaseInOut,
				_ => UIViewAnimationOptions.CurveEaseOut,
			};
		}

		public static NSDictionary<NSString, NSString> ToNSDictionary(this IDictionary<string, string> dictionary) {
			return new NSDictionary<NSString, NSString>(dictionary.Keys.Select(k => new NSString(k)).ToArray(), dictionary.Values.Select(v => new NSString(v)).ToArray());
		}

		public static IDictionary<string, string> ToDictionary(this NSDictionary dictionary) {
			return dictionary.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());
		}
	}
}