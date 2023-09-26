using System;
using System.Globalization;

using JKChat.Core.Models;

using MvvmCross.Converters;

namespace JKChat.Android.ValueConverters {
	public class ServerPreviewValueConverter : MvxValueConverter<Game, int> {
		protected override int Convert(Game value, Type targetType, object parameter, CultureInfo culture) {
			return value switch {
				Game.JediAcademy => Resource.Drawable.bg_ja_preview,
				Game.JediOutcast => Resource.Drawable.bg_jo_preview,
				Game.Quake3 => Resource.Drawable.bg_q3_preview,
				_ => -1
			};
		}
	}
}