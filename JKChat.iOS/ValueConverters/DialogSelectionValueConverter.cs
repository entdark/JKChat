using System;
using System.Globalization;

using MvvmCross.Converters;

using UIKit;

namespace JKChat.iOS.ValueConverters {
	public class DialogSelectionValueConverter : MvxValueConverter<bool, UIColor> {
		protected override UIColor Convert(bool value, Type targetType, object parameter, CultureInfo culture) {
			return value ? Theme.Color.DialogSelection : UIColor.Clear;
		}
	}
}