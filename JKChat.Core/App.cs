using JKChat.Core.Services;
using JKChat.Core.ViewModels.Main;

using MvvmCross;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;

namespace JKChat.Core {
	public class App : MvxApplication {
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
			RegisterAppStart<MainViewModel>();
		}
	}
}
