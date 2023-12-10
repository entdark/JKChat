using System;
using System.Collections.Generic;
using System.Globalization;

using Foundation;

using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.ValueCombiners;

using MvvmCross.Converters;

using UIKit;

namespace JKChat.iOS.ValueConverters {
	public class ColourTextValueConverter : MvxValueConverter<string, NSAttributedString> {
		private static readonly UIColor ShadowColor = UIColor.FromRGB(38, 38, 38);

		protected override NSAttributedString Convert(string value, Type targetType, object parameter, CultureInfo culture) {
			return Convert(value, parameter);
		}

		public static NSAttributedString Convert(string value, object parameter = null) {
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
			List<AttributeData<Uri>> uriAttributes = parseUri ? new() : null;

			string cleanStr = value.CleanString(colorAttributes, uriAttributes);
			var attributedString = new NSMutableAttributedString(cleanStr);

			//attributedString.AddAttribute(UIStringAttributeKey.Shadow, GetShadow(), new NSRange(0, cleanStr.Length));
			if (parseShadow) {
				var shadowColorAttributes = new List<AttributeData<int>>();
				value.CleanString(shadowColorAttributes, shadow: parseShadow);
				attributedString.AddAttribute(UIStringAttributeKey.Shadow, GetShadow(), new NSRange(0, cleanStr.Length));
				foreach (var shadowColorAttribute in shadowColorAttributes) {
					attributedString.AddAttribute(UIStringAttributeKey.Shadow, GetShadow(shadowColorAttribute.Value), new NSRange(shadowColorAttribute.Start, shadowColorAttribute.Length));
				}
			}
			foreach (var colorAttribute in colorAttributes) {
				attributedString.AddAttribute(UIStringAttributeKey.ForegroundColor, GetColor(colorAttribute.Value), new NSRange(colorAttribute.Start, colorAttribute.Length));
			}
			if (parseUri) {
				foreach (var uriAttribute in uriAttributes) {
					attributedString.AddAttribute(UIStringAttributeKey.UnderlineStyle, NSNumber.FromInt64((long)NSUnderlineStyle.Single), new NSRange(uriAttribute.Start, uriAttribute.Length));
				}
			}
			return attributedString;
		}

		private static UIColor GetColor(int code) {
			return code switch {
				8 when AppSettings.OpenJKColours => UIColor.FromRGB(255, 127, 0),
				9 when AppSettings.OpenJKColours => UIColor.FromRGB(127, 127, 127),
				0 or 8 => UIColor.FromRGB(0, 0, 0),
				1 or 9 => UIColor.FromRGB(255, 0, 0),
				2 => UIColor.FromRGB(0, 255, 0),
				3 => UIColor.FromRGB(255, 255, 0),
				4 => UIColor.FromRGB(0, 0, 255),
				5 => UIColor.FromRGB(0, 255, 255),
				6 => UIColor.FromRGB(255, 0, 255),
				_ => UIColor.FromRGB(255, 255, 255),
			};
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