using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Views;

using JKChat.Core.Helpers;

using MvvmCross.Converters;

using Xamarin.Essentials;

namespace JKChat.Android.ValueConverters {
	public class ColourTextValueConverter : MvxValueConverter<string, ISpannable> {
		protected override ISpannable Convert(string value, Type targetType, object parameter, CultureInfo culture) {
			if (string.IsNullOrEmpty(value)) {
				return new SpannableString(string.Empty);
			}
			bool parseUri = parameter is bool b && b;
			var colorAttributes = new List<AttributeData<int>>();
			List<AttributeData<Uri>> uriAttributes = null;
			if (parseUri) {
				uriAttributes = new List<AttributeData<Uri>>();
			}

			string cleanStr = ColourTextHelper.CleanString(value, colorAttributes, uriAttributes);
			var spannable = new SpannableString(cleanStr);

			foreach (var colorAttribute in colorAttributes) {
				spannable.SetSpan(new ForegroundColorSpan(GetColor(colorAttribute.Value)), colorAttribute.Start, colorAttribute.Start+colorAttribute.Length, SpanTypes.ExclusiveExclusive);
			}
			if (parseUri) {
				foreach (var uriAttribute in uriAttributes) {
					spannable.SetSpan(new LinkClickableSpan(uriAttribute.Value), uriAttribute.Start, uriAttribute.Start+uriAttribute.Length, SpanTypes.ExclusiveExclusive);
					var color = colorAttributes.LastOrDefault((colorAttribute) => uriAttribute.Start >= colorAttribute.Start)?.Value ?? 7;
					spannable.SetSpan(new ForegroundColorSpan(GetColor(color)), uriAttribute.Start, uriAttribute.Start+uriAttribute.Length, SpanTypes.ExclusiveExclusive);
					spannable.SetSpan(new UnderlineSpan(), uriAttribute.Start, uriAttribute.Start+uriAttribute.Length, SpanTypes.ExclusiveExclusive);
				}
			}
			return spannable;
		}

		private static Color GetColor(int code) {
			switch (code) {
			case 0:
			case 8:
				return new Color(0, 0, 0);
			case 1:
			case 9:
				return new Color(255, 0, 0);
			case 2:
				return new Color(0, 255, 0);
			case 3:
				return new Color(255, 255, 0);
			case 4:
				return new Color(0, 0, 255);
			case 5:
				return new Color(0, 255, 255);
			case 6:
				return new Color(255, 0, 255);
			default:
			case 7:
				return new Color(255, 255, 255);
			}
		}

		public class LinkClickableSpan : ClickableSpan {
			private readonly Uri uri;
			public LinkClickableSpan(Uri uri) {
				this.uri = uri;
			}
			public override void OnClick(View widget) {
				try {
					if (string.Compare(uri.Scheme, "http", true) != 0 || string.Compare(uri.Scheme, "https", true) != 0 || string.Compare(uri.Scheme, "ftp", true) != 0) {
						throw new Exception();
					}
					Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
				} catch (Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
					Launcher.TryOpenAsync(uri);
				}
			}
		}
	}
}