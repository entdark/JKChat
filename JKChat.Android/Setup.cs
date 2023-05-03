using Android.Views;

using JKChat.Android.Presenter;
using JKChat.Android.Services;
using JKChat.Core;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.Core.ValueCombiners;

using Microsoft.Extensions.Logging;

using MvvmCross;
using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Binding.Combiners;
using MvvmCross.Converters;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.Platforms.Android.Presenters;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using MvvmCross.Views;

using Serilog;
using Serilog.Extensions.Logging;

namespace JKChat.Android {
	public class Setup : MvxAndroidSetup<App> {
		protected override IMvxNavigationService CreateNavigationService(IMvxIoCProvider iocProvider) {
			iocProvider.LazyConstructAndRegisterSingleton<IMvxNavigationService, IMvxViewModelLoader, IMvxViewDispatcher, IMvxIoCProvider>(
				(loader, dispatcher, iocProvider) => new NavigationService(loader, dispatcher, iocProvider));
			var navigationService = iocProvider.Resolve<IMvxNavigationService>();
			iocProvider.RegisterSingleton(navigationService as INavigationService);
			return navigationService;
		}

		protected override IMvxAndroidViewPresenter CreateViewPresenter() {
			return new AndroidViewPresenter(AndroidViewAssemblies);
		}

		protected override void InitializeFirstChance(IMvxIoCProvider iocProvider) {
			iocProvider.RegisterSingleton<IDialogService>(() => new DialogService());
			base.InitializeFirstChance(iocProvider);
		}

		protected override void FillValueConverters(IMvxValueConverterRegistry registry) {
			base.FillValueConverters(registry);
			Mvx.IoCProvider.CallbackWhenRegistered<IMvxValueCombinerRegistry>(registry2 => {
				registry2.AddOrOverwrite("ColourTextParameter", new ColourTextParameterValueCombiner());
			});
		}

		public override void LoadPlugins(IMvxPluginManager pluginManager) {
			base.LoadPlugins(pluginManager);
			pluginManager.EnsurePluginLoaded<MvvmCross.Plugin.Visibility.Platforms.Android.Plugin>(true);
		}

		protected override ILoggerProvider CreateLogProvider() {
			return new SerilogLoggerProvider();
		}

		protected override ILoggerFactory CreateLogFactory() {
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				// add more sinks here
				.WriteTo.AndroidLog()
				.CreateLogger();

			return new SerilogLoggerFactory();
		}
	}
}