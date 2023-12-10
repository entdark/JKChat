using System.Threading.Tasks;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Main;

using MvvmCross;
using MvvmCross.Navigation;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;

namespace JKChat.Core {
	public class App : MvxApplication {
		public static bool AllowReset { get; set; } = true;

		public override void LoadPlugins(IMvxPluginManager pluginManager) {
			base.LoadPlugins(pluginManager);
			pluginManager.EnsurePluginLoaded<MvvmCross.Plugin.Messenger.Plugin>(true);
		}

		public override void Initialize() {
			Mvx.IoCProvider.RegisterSingleton<IServerListService>(() => new ServerListService());
			Mvx.IoCProvider.RegisterSingleton<IGameClientsService>(() => new GameClientsService());
			Mvx.IoCProvider.RegisterSingleton<ICacheService>(() => new CacheService());
			Mvx.IoCProvider.RegisterSingleton<IJKClientService>(() => new JKClientService());
			Mvx.IoCProvider.Resolve<IJKClientService>().SetEncodingById(AppSettings.EncodingId);
			RegisterCustomAppStart<AppStart>();
		}

		private class AppStart : MvxAppStart<MainViewModel> {
			public AppStart(IMvxApplication application, IMvxNavigationService navigationService) : base(application, navigationService) {
			}

			protected override Task NavigateToFirstViewModel(object hint = null) {
				if (hint != null)
					return Task.CompletedTask;
				return base.NavigateToFirstViewModel(hint);
			}

			public override void ResetStart() {
				if (AllowReset)
					base.ResetStart();
			}
		}
	}
}