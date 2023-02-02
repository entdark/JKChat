using System.Threading.Tasks;

using JKChat.Core.ViewModels.AdminPanel;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.Settings;

using MvvmCross.Commands;

namespace JKChat.Core.ViewModels.Main {
	public class MainViewModel : BaseViewModel {
		public IMvxCommand ShowInitialViewModelsCommand { get; init; }
		public MainViewModel() {
			ShowInitialViewModelsCommand = new MvxAsyncCommand(ShowInitialViewModelsExecute);
		}

		private async Task ShowInitialViewModelsExecute() {
			await Task.WhenAll(
				NavigationService.Navigate<ServerListViewModel>(),
//				NavigationService.Navigate<AdminPanelViewModel>(),
				NavigationService.Navigate<SettingsViewModel>()
			);
		}
	}
}
