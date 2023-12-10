using System.Threading.Tasks;

using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Favourites;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.Settings;

using MvvmCross.ViewModels;

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
				NavigationService.Navigate<SettingsViewModel>()
			);
		}

		public override void ViewCreated() {
			base.ViewCreated();
			NavigationService.RootIsInitialized = true;
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				NavigationService.RootIsInitialized = false;
			}
			base.ViewDestroy(viewFinishing);
		}

		public override void ViewAppearing() {
			base.ViewAppearing();
			_ = ShowInitialViewModelsExecute();
		}

		protected override void SaveStateToBundle(IMvxBundle bundle) {
			base.SaveStateToBundle(bundle);
			bundle.Data[nameof(initialNavigationDone)] = initialNavigationDone.ToString();
		}

		protected override void ReloadFromBundle(IMvxBundle state) {
			base.ReloadFromBundle(state);
			if (state.Data.TryGetValue(nameof(initialNavigationDone), out string initDone))
				_ = bool.TryParse(initDone, out initialNavigationDone);
		}
	}
}