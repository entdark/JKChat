using System.Globalization;

using MvvmCross.Plugin.Visibility;
using MvvmCross.UI;

namespace JKChat.Core.ValueConverters {
	public class EnumVisibilityValueConverter : MvxVisibilityValueConverter {
		private static readonly EnumBoolValueConverter boolConverter = new();

		protected override MvxVisibility Convert(object value, object parameter, CultureInfo culture) {
			bool result = boolConverter.Convert(value, null, parameter, culture) is bool b && b;
			return result switch {
				true => MvxVisibility.Visible,
				_ => MvxVisibility.Collapsed
			};
		}
	}

	public class EnumInvertedVisibilityValueConverter : EnumVisibilityValueConverter {
		protected override MvxVisibility Convert(object value, object parameter, CultureInfo culture) {
			var result = base.Convert(value, parameter, culture);
			return result switch {
				MvxVisibility.Visible => MvxVisibility.Collapsed,
				_ => MvxVisibility.Visible
			};
		}
	}
}