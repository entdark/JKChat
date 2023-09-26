using System.Threading.Tasks;

using JKChat.Core.ViewModels.AdminPanel;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Favourites;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.Settings;

namespace JKChat.Core.ViewModels.Main {
	public class MainViewModel : BaseViewModel {
		private bool initialNavigationDone = false;

		public MainViewModel() {}

		private async Task ShowInitialViewModelsExecute() {
			if (initialNavigationDone)
				return;
			initialNavigationDone = true;
			await Task.WhenAll(
				NavigationService.Navigate<ServerListViewModel>(),
				NavigationService.Navigate<FavouritesViewModel>(),
//				NavigationService.Navigate<AdminPanelViewModel>(),
				NavigationService.Navigate<SettingsViewModel>()
			);
		}

		public override void ViewAppearing() {
			base.ViewAppearing();
			_ = ShowInitialViewModelsExecute();
		}
	}
}