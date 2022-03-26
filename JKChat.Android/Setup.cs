using Android.Views;

using JKChat.Android.Services;
using JKChat.Android.TargetBindings;
using JKChat.Core;
using JKChat.Core.Services;
using JKChat.Core.ValueCombiners;

using MvvmCross;
using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Binding.Combiners;
using MvvmCross.Converters;
using MvvmCross.IoC;
using MvvmCross.Platforms.Android.Core;

namespace JKChat.Android {
	public class Setup : MvxAndroidSetup<App> {
		protected override void InitializeFirstChance() {
			Mvx.IoCProvider.RegisterSingleton<IDialogService>(() => new DialogService());
			base.InitializeFirstChance();
		}

		protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry) {
			base.FillTargetFactories(registry);
			var allMargins = new[] {
				FloatMarginTargetBinding.View_Margin,
				FloatMarginTargetBinding.View_MarginLeft,
				FloatMarginTargetBinding.View_MarginRight,
				FloatMarginTargetBinding.View_MarginTop,
				FloatMarginTargetBinding.View_MarginBottom,
				FloatMarginTargetBinding.View_MarginStart,
				FloatMarginTargetBinding.View_MarginEnd
			};
			foreach (var margin in allMargins) {
				registry.RegisterCustomBindingFactory<View>(
					margin, view => new FloatMarginTargetBinding(view, margin));
			}
		}

		protected override void FillValueConverters(IMvxValueConverterRegistry registry) {
			base.FillValueConverters(registry);
			Mvx.IoCProvider.CallbackWhenRegistered<IMvxValueCombinerRegistry>(registry2 => {
				registry2.AddOrOverwrite("ColourTextParameter", new ColourTextParameterValueCombiner());
			});
		}
	}
}