using System;
using System.Collections.Generic;
using System.Text;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Main;
using JKChat.Core.ViewModels.ServerList;

using MvvmCross;
using MvvmCross.Navigation.EventArguments;
using MvvmCross.ViewModels;

namespace JKChat.Core {
	public class App : MvxApplication {
		public override void Initialize() {
			Mvx.IoCProvider.RegisterSingleton<IGameClientsService>(() => new GameClientsService());
			Mvx.IoCProvider.RegisterSingleton<ICacheService>(() => new CacheService());
			RegisterAppStart<MainViewModel>();
		}
	}
}
