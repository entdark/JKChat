using System;
using System.Globalization;

using JKChat.Core.Models;

using MvvmCross.Converters;

using UIKit;

namespace JKChat.iOS.ValueConverters {
	public class ConnectionColorValueConverter : MvxValueConverter<ConnectionStatus, UIColor> {
		protected override UIColor Convert(ConnectionStatus value, Type targetType, object parameter, CultureInfo culture) {
			switch (value) {
			default:
			case ConnectionStatus.Disconnected:
				return Theme.Color.Disconnected;
			case ConnectionStatus.Connecting:
				return Theme.Color.Connecting;
			case ConnectionStatus.Connected:
				return Theme.Color.Connected;
			}
		}
	}
}