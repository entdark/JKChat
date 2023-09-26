using System;
using System.Globalization;

using JKChat.Core.Models;

using MvvmCross.Converters;

namespace JKChat.Android.ValueConverters {
	public class ChatTypeIconValueConverter : MvxValueConverter<ChatType, int> {
		protected override int Convert(ChatType value, Type targetType, object parameter, CultureInfo culture) {
			return value switch {
				ChatType.Team => Resource.Drawable.ic_group,
				ChatType.Private => Resource.Drawable.ic_person,
				_ => Resource.Drawable.ic_groups
			};
		}
	}
}