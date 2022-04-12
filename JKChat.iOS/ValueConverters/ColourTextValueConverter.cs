using System;
using System.Collections.Generic;
using System.Globalization;

using Foundation;

using JKChat.Core.Helpers;
using JKChat.Core.ValueCombiners;

using MvvmCross.Converters;

using UIKit;

namespace JKChat.iOS.ValueConverters {
	public class ColourTextValueConverter : MvxValueConverter<string, NSAttributedString> {
		private static readonly UIColor ShadowColor = UIColor.FromRGB(38, 38, 38);
		protected override NSAttributedString Convert(string value, Type targetType, object parameter, CultureInfo culture) {
			if (string.IsNullOrEmpty(value)) {
				return new NSAttributedString(string.Empty);
			}
			bool parseUri = false, parseShadow = false;
			if (parameter is bool b) {
				parseUri = b;
			} else if (parameter is ColourTextParameter ct) {
				parseUri = ct.ParseUri;
				parseShadow = ct.ParseShadow;
			}
			var colorAttributes = new List<AttributeData<int>>();
			List<AttributeData<Uri>> uriAttributes = null;
			if (parseUri) {
				uriAttributes = new List<AttributeData<Uri>>();
			}

			string cleanStr = value.CleanString(colorAttributes, uriAttributes);
			var attributedString = new NSMutableAttributedString(cleanStr);

			//attributedString.AddAttribute(UIStringAttributeKey.Shadow, GetShadow(), new NSRange(0, cleanStr.Length));
			if (parseShadow) {
				var shadowColorAttributes = new List<AttributeData<int>>();
				value.CleanString(shadowColorAttributes, shadow: parseShadow);
				attributedString.AddAttribute(UIStringAttributeKey.Shadow, GetShadow(), new NSRange(0, cleanStr.Length));
				foreach (var shadowColorAttribute in shadowColorAttributes) {
					attributedString.AddAttribute(UIStringAttributeKey.Shadow, GetShadow(shadowColorAttribute.Value), new NSRange(shadowColorAttribute.Start, cleanStr.Length-shadowColorAttribute.Start));
				}
			}
			foreach (var colorAttribute in colorAttributes) {
				attributedString.AddAttribute(UIStringAttributeKey.ForegroundColor, GetColor(colorAttribute.Value), new NSRange(colorAttribute.Start, cleanStr.Length-colorAttribute.Start));
			}
			if (parseUri) {
				foreach (var uriAttribute in uriAttributes) {
					attributedString.AddAttribute(UIStringAttributeKey.UnderlineStyle, NSNumber.FromInt64((long)NSUnderlineStyle.Single), new NSRange(uriAttribute.Start, uriAttribute.Length));
				}
			}
			return attributedString;
		}

		private static UIColor GetColor(int code) {
			switch (code) {
			case 0:
			case 8:
				return UIColor.FromRGB(0, 0, 0);
			case 1:
			case 9:
				return UIColor.FromRGB(255, 0, 0);
			case 2:
				return UIColor.FromRGB(0, 255, 0);
			case 3:
				return UIColor.FromRGB(255, 255, 0);
			case 4:
				return UIColor.FromRGB(0, 0, 255);
			case 5:
				return UIColor.FromRGB(0, 255, 255);
			case 6:
				return UIColor.FromRGB(255, 0, 255);
			default:
			case 7:
				return UIColor.FromRGB(255, 255, 255);
			}
		}
		private static NSShadow GetShadow(int code = -1) {
			return new NSShadow() {
				ShadowColor = code >= 0 ? GetColor(code) : ShadowColor,
				ShadowBlurRadius = float.Epsilon,
				ShadowOffset = new CoreGraphics.CGSize(1.337f, 1.337f)
			};
		}
	}
}