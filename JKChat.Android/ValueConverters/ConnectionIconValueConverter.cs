using System;
using System.Globalization;

using JKChat.Core.Models;

using MvvmCross.Converters;

namespace JKChat.Android.ValueConverters {
	public class ConnectionIconValueConverter : MvxValueConverter<ConnectionStatus, int> {
		protected override int Convert(ConnectionStatus value, Type targetType, object parameter, CultureInfo culture) {
			return value switch {
				ConnectionStatus.Connecting => Resource.Drawable.connecting_background,
				ConnectionStatus.Connected => Resource.Drawable.connected_background,
				_ => Resource.Drawable.disconnected_background
			};
		}
	}
}