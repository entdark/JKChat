using JKChat.Core.Services;
using JKChat.Core.ViewModels.Main;

using MvvmCross;
using MvvmCross.ViewModels;

namespace JKChat.Core {
	public class App : MvxApplication {
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
