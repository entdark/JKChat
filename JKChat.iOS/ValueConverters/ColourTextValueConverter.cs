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
			var ct = parameter as ColourTextParameter;
			bool parseUri = ct != null && ct.ParseUri;
			var colorAttributes = new List<AttributeData<int>>();
			List<AttributeData<Uri>> uriAttributes = null;
			if (parseUri) {
				uriAttributes = new List<AttributeData<Uri>>();
			}

			string cleanStr = ColourTextHelper.CleanString(value, uriAttributes, colorAttributes);
			var attributedString = new NSMutableAttributedString(cleanStr);

			if (ct != null && ct.Font != null) {
				attributedString.AddAttribute(UIStringAttributeKey.Font, ct.Font, new NSRange(0, cleanStr.Length));
			}

			int index = value.IndexOf("^؉", StringComparison.Ordinal);
			int index2 = value.IndexOf("^؊", StringComparison.Ordinal);
			if (index == 0 && index2 > 0) {
				UIFont font = Theme.Font.OCRAStd(8.0f);
				attributedString.AddAttribute(UIStringAttributeKey.Font, font, new NSRange(0, index2 - 2));
			}

			index = value.IndexOf("^֎", StringComparison.Ordinal);
			if (index >= 0) {
				UIFont font = Theme.Font.OCRAStd(10.0f);
				index2 = value.IndexOf("^֎^", StringComparison.Ordinal);
				if (index2 < 0) {
					var fontDescriptor = font.FontDescriptor;
					fontDescriptor = fontDescriptor.CreateWithTraits(UIFontDescriptorSymbolicTraits.Italic);
					if (fontDescriptor == null) {
						fontDescriptor = UIFont.SystemFontOfSize(10.0f).FontDescriptor.CreateWithTraits(UIFontDescriptorSymbolicTraits.Italic);
					}
					font = UIFont.FromDescriptor(fontDescriptor, 0.0f);
				}
				if (value.StartsWith("^؉", StringComparison.Ordinal)) {
					index -= 4;
				}
				attributedString.AddAttribute(UIStringAttributeKey.Font, font, new NSRange(index, cleanStr.Length - index));
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
			switch (code) {
			case '֎':
				return UIColor.FromRGB(128, 128, 128);
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

	public class ColourTextParameter {
		public UIFont Font { get; set; }
		public bool ParseUri { get; set; }
	}
}