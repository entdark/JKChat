using System;
using System.Globalization;
using JKChat.Core.Models;
using MvvmCross.Converters;

namespace JKChat.Android.ValueConverters {
	public class ChatTypeIconValueConverter : MvxValueConverter<ChatType, int> {
		protected override int Convert(ChatType value, Type targetType, object parameter, CultureInfo culture) {
			switch (value) {
			default:
			case ChatType.Common:
				return Resource.Drawable.chat_type_common;
			case ChatType.Team:
				return Resource.Drawable.chat_type_team;
			case ChatType.Private:
				return Resource.Drawable.chat_type_private;
			}
		}
	}
}