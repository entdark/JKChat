using System;
using System.Globalization;

using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross.Converters;

namespace JKChat.Android.ValueConverters {
	public class ChatMessageMarginValueConverter : MvxValueConverter<Type, int> {
		protected override int Convert(Type value, Type targetType, object parameter, CultureInfo culture) {
			return value == typeof(ChatMessageItemVM) ? 0 : 15;
		}
	}
}