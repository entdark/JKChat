using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Views;

using JKChat.Android.Helpers;
using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.ValueCombiners;

using MvvmCross.Converters;

using Microsoft.Maui.ApplicationModel;

namespace JKChat.Android.ValueConverters {
	public class ColourTextValueConverter : MvxValueConverter<string, ISpannable> {
		private static readonly Color ShadowColor = new(38, 38, 38);

		public static ISpannable Convert(string value, object parameter = null) {
			if (string.IsNullOrEmpty(value)) {
				if (value == null)
					return null;
				else
					return new SpannableString(string.Empty);
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
			var spannable = new SpannableString(cleanStr);

			//spannable.SetSpan(new ShadowSpan(Color.White), 0, cleanStr.Length, SpanTypes.ExclusiveInclusive);
			if (parseShadow) {
				var shadowColorAttributes = new List<AttributeData<int>>();
				value.CleanString(shadowColorAttributes, shadow: parseShadow);
				spannable.SetSpan(new ShadowSpan(ShadowColor), 0, cleanStr.Length, SpanTypes.ExclusiveExclusive);
				foreach (var shadowColorAttribute in shadowColorAttributes) {
					spannable.SetSpan(new ShadowSpan(GetColor(shadowColorAttribute.Value)), shadowColorAttribute.Start, shadowColorAttribute.Start+shadowColorAttribute.Length, SpanTypes.ExclusiveExclusive);
				}
			}
			foreach (var colorAttribute in colorAttributes) {
				spannable.SetSpan(new ForegroundColorCodeSpan(colorAttribute.Value), colorAttribute.Start, colorAttribute.Start+colorAttribute.Length, SpanTypes.ExclusiveExclusive);
			}
			if (parseUri) {
				foreach (var uriAttribute in uriAttributes) {
					spannable.SetSpan(new LinkClickableSpan(uriAttribute.Value), uriAttribute.Start, uriAttribute.Start+uriAttribute.Length, SpanTypes.ExclusiveExclusive);
					int color = colorAttributes.LastOrDefault((colorAttribute) => uriAttribute.Start >= colorAttribute.Start)?.Value ?? 7;
					spannable.SetSpan(new ForegroundColorCodeSpan(color), uriAttribute.Start, uriAttribute.Start+uriAttribute.Length, SpanTypes.ExclusiveExclusive);
					spannable.SetSpan(new UnderlineSpan(), uriAttribute.Start, uriAttribute.Start+uriAttribute.Length, SpanTypes.ExclusiveExclusive);
				}
			}
			return spannable;
		}

		protected override ISpannable Convert(string value, Type targetType, object parameter, CultureInfo culture) {
			return Convert(value, parameter);
		}

		protected override string ConvertBack(ISpannable value, Type targetType, object parameter, CultureInfo culture) {
			if (value == null) {
				return null;
			}
			var stringBuilder = new StringBuilder(value.ToString());
			var spans = value.GetSpans(0, value.Length(), Java.Lang.Class.FromType(typeof(ForegroundColorCodeSpan)));
			if (spans == null || spans.Length <= 0) {
				return stringBuilder.ToString();
			}
			for (int i = spans.Length-1; i >= 0; i--) {
				if (spans[i] is ForegroundColorCodeSpan colorSpan) {
					int start = value.GetSpanStart(colorSpan);
					stringBuilder.Insert(start, colorSpan.ColorCode);
					stringBuilder.Insert(start, '^');
				}
			}
			return stringBuilder.ToString();
		}

		private static Color GetColor(int code) {
			return code switch {
				8 when AppSettings.OpenJKColours => new(255, 127, 0),
				9 when AppSettings.OpenJKColours => new(127, 127, 127),
				0 or 8 => new Color(0, 0, 0),
				1 or 9 => new Color(255, 0, 0),
				2 => new Color(0, 255, 0),
				3 => new Color(255, 255, 0),
				4 => new Color(0, 0, 255),
				5 => new Color(0, 255, 255),
				6 => new Color(255, 0, 255),
				_ => new Color(255, 255, 255),
			};
		}

		public class ForegroundColorCodeSpan : ForegroundColorSpan {
			public int ColorCode { get; init; }
			public ForegroundColorCodeSpan(int code) : base(GetColor(code)) {
				ColorCode = code;
			}
		}

		public class LinkClickableSpan : ClickableSpan {
			private readonly Uri uri;
			public LinkClickableSpan(Uri uri) {
				this.uri = uri;
			}
			public override void OnClick(View widget) {
				try {
					if (string.Compare(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) != 0
						&& string.Compare(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) != 0
						&& string.Compare(uri.Scheme, "ftp", StringComparison.OrdinalIgnoreCase) != 0) {
						throw new Exception();
					}
					Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
				} catch (Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
					Launcher.TryOpenAsync(uri);
				}
			}
		}

		public class ShadowSpan : MetricAffectingSpan {
			public Color ShadowColor { get; init; }
			public ShadowSpan(Color color) {
				ShadowColor = color;
			}
			public override void UpdateDrawState(TextPaint tp) {
				UpdateMeasureState(tp);
				tp.SetShadowLayer(float.Epsilon, 1.337f.DpToPxF(), 1.337f.DpToPxF(), ShadowColor);
			}
			public override void UpdateMeasureState(TextPaint textPaint) {
			}
		}
	}
}