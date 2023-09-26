using System;
using System.Globalization;

using JKChat.Core.Models;

using MvvmCross.Converters;

using UIKit;

namespace JKChat.iOS.ValueConverters {
	public class ConnectionColorValueConverter : MvxValueConverter<ConnectionStatus, UIColor> {
		protected override UIColor Convert(ConnectionStatus value, Type targetType, object parameter, CultureInfo culture) {
			return value switch {
				ConnectionStatus.Connecting => Theme.Color.Connecting,
				ConnectionStatus.Connected => Theme.Color.Connected,
				_ => Theme.Color.Disconnected,
			};
		}
	}
}