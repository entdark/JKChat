using System;
using System.Globalization;
using JKChat.Core.Models;
using MvvmCross.Converters;

namespace JKChat.Android.ValueConverters {
	public class ConnectionIconValueConverter : MvxValueConverter<ConnectionStatus, int> {
		protected override int Convert(ConnectionStatus value, Type targetType, object parameter, CultureInfo culture) {
			switch (value) {
			default:
			case ConnectionStatus.Disconnected:
				return Resource.Drawable.disconnected_background;
			case ConnectionStatus.Connecting:
				return Resource.Drawable.connecting_background;
			case ConnectionStatus.Connected:
				return Resource.Drawable.connected_background;
			}
		}
	}
}