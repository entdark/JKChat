using JKChat.Core;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.Core.ValueCombiners;
using JKChat.iOS.Presenter;
using JKChat.iOS.Services;
using JKChat.iOS.TargetBindings;

using Microsoft.Extensions.Logging;

using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Binding.Combiners;
using MvvmCross.Converters;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Platforms.Ios.Presenters;
using MvvmCross.Plugin.Visibility;
using MvvmCross.UI;
using MvvmCross.ViewModels;
using MvvmCross.Views;

using Serilog;
using Serilog.Extensions.Logging;

using UIKit;

namespace JKChat.iOS {
	public class Setup : MvxIosSetup<App> {
		protected override IMvxNavigationService CreateNavigationService(IMvxIoCProvider iocProvider) {
			iocProvider.LazyConstructAndRegisterSingleton<IMvxNavigationService, IMvxViewModelLoader, IMvxViewDispatcher, IMvxIoCProvider>(
				(loader, dispatcher, iocProvider) => new NavigationService(loader, dispatcher, iocProvider));
			var navigationService = iocProvider.Resolve<IMvxNavigationService>();
			iocProvider.RegisterSingleton(navigationService as INavigationService);
			return navigationService;
		}

		protected override IMvxIosViewPresenter CreateViewPresenter() {
			return new iOSViewPresenter(ApplicationDelegate, Window);
		}

		protected override void RegisterPresenter(IMvxIoCProvider iocProvider) {
			base.RegisterPresenter(iocProvider);
			iocProvider.RegisterSingleton(iocProvider.Resolve<IMvxIosViewPresenter>() as IViewPresenter);
		}

		protected override void InitializeFirstChance(IMvxIoCProvider iocProvider) {
			iocProvider.RegisterSingleton<IDialogService>(() => new DialogService());
			iocProvider.RegisterSingleton<IAppService>(() => new AppService());
			iocProvider.RegisterSingleton<INotificationsService>(() => new NotificationsService());
			base.InitializeFirstChance(iocProvider);
		}

		protected override void FillValueConverters(IMvxValueConverterRegistry registry) {
			base.FillValueConverters(registry);
			registry.AddOrOverwrite("Visibility", new MvxVisibilityValueConverter());
		}

		protected override void FillValueCombiners(IMvxValueCombinerRegistry registry) {
			base.FillValueCombiners(registry);
			registry.AddOrOverwrite("ColourTextParameter", new ColourTextParameterValueCombiner());
		}

		protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry) {
			base.FillTargetFactories(registry);
			registry.RegisterCustomBindingFactory<UIControl>("Selected",
				view => new UIControlSelectedTargetBinding(view));
		}

		protected override ILoggerProvider CreateLogProvider() {
			return new SerilogLoggerProvider();
		}

		protected override ILoggerFactory CreateLogFactory() {
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				// add more sinks here
				.WriteTo.NSLog()
				.CreateLogger();

			return new SerilogLoggerFactory();
		}
	}
}

#if __MACCATALYST__
namespace MvvmCross.Plugin.Visibility.Platforms.Ios {
	[MvxPlugin]
	[Preserve(AllMembers = true)]
	public class Plugin : BasePlugin {
		public override void Load() {
			base.Load();
			Mvx.IoCProvider?.RegisterSingleton<IMvxNativeVisibility>(new MvxIosVisibility());
		}
	}
}

namespace MvvmCross.Plugin.Visibility.Platforms.Ios {
	[Preserve(AllMembers = true)]
	public class MvxIosVisibility : IMvxNativeVisibility {
		public object ToNative(MvxVisibility visibility) {
			return visibility;
		}
	}
}
#endif