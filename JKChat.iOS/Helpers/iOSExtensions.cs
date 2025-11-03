using System.Collections.Generic;
using System.Drawing;
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
				UIViewAnimationCurve.EaseOut => UIViewAnimationOptions.CurveEaseOut,
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

		public static void AddWithConstraintsTo(this UIView view, UIView parent, float leading = 0.0f, float top = 0.0f, float trailing = 0.0f, float bottom = 0.0f) {
			parent.AddSubview(view);
			view.ApplyConstraints(parent, leading, top, trailing, bottom);
		}

		public static void InsertWithConstraintsInto(this UIView view, UIView parent, nint index, float leading = 0.0f, float top = 0.0f, float trailing = 0.0f, float bottom = 0.0f) {
			parent.InsertSubview(view, index);
			view.ApplyConstraints(parent, leading, top, trailing, bottom);
		}

		public static void ApplyConstraints(this UIView view, UIView parent, float leading = 0.0f, float top = 0.0f, float trailing = 0.0f, float bottom = 0.0f) {
			view.TranslatesAutoresizingMaskIntoConstraints = false;
			view.LeadingAnchor.ConstraintEqualTo(parent.LeadingAnchor, leading).Active = true;
			view.TrailingAnchor.ConstraintEqualTo(parent.TrailingAnchor, trailing).Active = true;
			view.TopAnchor.ConstraintEqualTo(parent.TopAnchor, top).Active = true;
			view.BottomAnchor.ConstraintEqualTo(parent.BottomAnchor, bottom).Active = true;
		}

		public static void BringToFront(this UIView view) {
			view?.Superview?.BringSubviewToFront(view);
		}

		public static UIColor ToUIColor(this Color color) {
			return UIColor.FromRGBA(color.R, color.G, color.B, color.A);
		}

		public static UIImage Scale(this UIImage image, nfloat scale) {
			var size = new CGSize(image.Size.Width * scale, image.Size.Height * scale);
			var renderer = new UIGraphicsImageRenderer(size);
			return renderer.CreateImage(context => {
				image.Draw(new CGRect(CGPoint.Empty, size));
			});
		}
	}
}