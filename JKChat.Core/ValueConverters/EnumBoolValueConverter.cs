using System;
using System.Globalization;

using MvvmCross.Converters;

namespace JKChat.Core.ValueConverters {
	public class EnumBoolValueConverter : MvxValueConverter {
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return Enum.TryParse(value?.GetType(), parameter?.ToString(), out var result) && value.Equals(result);
		}
	}
}