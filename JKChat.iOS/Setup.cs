using JKChat.Core;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.Core.ValueCombiners;
using JKChat.iOS.Presenter;
using JKChat.iOS.Services;

//using Microsoft.Extensions.Logging;

using MvvmCross;
using MvvmCross.Binding.Combiners;
using MvvmCross.Converters;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Platforms.Ios.Presenters;
using MvvmCross.ViewModels;
using MvvmCross.Views;

//using Serilog;
//using Serilog.Extensions.Logging;

namespace JKChat.iOS {
	public class Setup : MvxIosSetup<App> {
		protected override IMvxNavigationService CreateNavigationService(/*IMvxIoCProvider iocProvider*/) {
			var iocProvider = Mvx.IoCProvider;
			iocProvider.LazyConstructAndRegisterSingleton<IMvxNavigationService, IMvxViewModelLoader, IMvxViewDispatcher, IMvxIoCProvider>(
				(loader, dispatcher, iocProvider) => new NavigationService(loader/*, dispatcher, iocProvider*/));
			var navigationService = iocProvider.Resolve<IMvxNavigationService>();
			iocProvider.RegisterSingleton(navigationService as INavigationService);
			return navigationService;
		}

		protected override IMvxIosViewPresenter CreateViewPresenter() {
			return new iOSViewPresenter(ApplicationDelegate, Window);
		}

		protected override void RegisterPresenter(/*IMvxIoCProvider iocProvider*/) {
			var iocProvider = Mvx.IoCProvider;
			base.RegisterPresenter(/*iocProvider*/);
			iocProvider.RegisterSingleton(iocProvider.Resolve<IMvxIosViewPresenter>() as IViewPresenter);
		}

		protected override void InitializeFirstChance(/*IMvxIoCProvider iocProvider*/) {
			var iocProvider = Mvx.IoCProvider;
			iocProvider.RegisterSingleton<IDialogService>(() => new DialogService());
			base.InitializeFirstChance(/*iocProvider*/);
		}

		protected override void FillValueConverters(IMvxValueConverterRegistry registry) {
			base.FillValueConverters(registry);
			Mvx.IoCProvider.CallbackWhenRegistered<IMvxValueCombinerRegistry>(registry2 => {
				registry2.AddOrOverwrite("ColourTextParameter", new ColourTextParameterValueCombiner());
			});
		}

		/*protected override ILoggerProvider CreateLogProvider() {
			return new SerilogLoggerProvider();
		}

		protected override ILoggerFactory CreateLogFactory() {
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				// add more sinks here
				.WriteTo.NSLog()
				.CreateLogger();

			return new SerilogLoggerFactory();
		}*/
	}
}