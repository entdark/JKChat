using System;
using System.Collections.Generic;
using System.Globalization;

using Foundation;

using JKChat.Core.Helpers;

using MvvmCross.Converters;

using UIKit;

namespace JKChat.iOS.ValueConverters {
	public class ColourTextValueConverter : MvxValueConverter<string, NSAttributedString> {
		protected override NSAttributedString Convert(string value, Type targetType, object parameter, CultureInfo culture) {
			if (string.IsNullOrEmpty(value)) {
				return new NSAttributedString(string.Empty);
			}
			bool parseUri = parameter is bool b && b;
			var colorAttributes = new List<AttributeData<int>>();
			List<AttributeData<Uri>> uriAttributes = null;
			if (parseUri) {
				uriAttributes = new List<AttributeData<Uri>>();
			}

			string cleanStr = ColourTextHelper.CleanString(value, colorAttributes, uriAttributes);
			var attributedString = new NSMutableAttributedString(cleanStr);

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
	}
}