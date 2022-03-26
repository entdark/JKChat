using System;
using System.Globalization;

using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross.Converters;

namespace JKChat.Android.ValueConverters {
	public class ChatMessageMarginValueConverter : MvxValueConverter<Type, float> {
		protected override float Convert(Type value, Type targetType, object parameter, CultureInfo culture) {
			return value == typeof(ChatMessageItemVM) ? 7.5f : 15.0f;
		}
	}
}