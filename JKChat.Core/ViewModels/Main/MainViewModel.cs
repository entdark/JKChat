using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.Settings;

using MvvmCross.Commands;

namespace JKChat.Core.ViewModels.Main {
	public class MainViewModel : BaseViewModel {
		public IMvxCommand ShowInitialViewModelsCommand { get; private set; }
		public MainViewModel() {
			ShowInitialViewModelsCommand = new MvxAsyncCommand(ShowInitialViewModelsExecute);
		}

		private async Task ShowInitialViewModelsExecute() {
			await Task.WhenAll(NavigationService.Navigate<ServerListViewModel>(),
				NavigationService.Navigate<SettingsViewModel>());
		}
	}
}
