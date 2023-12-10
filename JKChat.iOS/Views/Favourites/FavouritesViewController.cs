using JKChat.Core.ViewModels.Favourites;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Views.Favourites;

[MvxTabPresentation(WrapInNavigationController = true, TabName = "Favourites", TabIconName = "star.fill")]
public partial class FavouritesViewController : BaseViewController<FavouritesViewModel> {
	public FavouritesViewController() : base(nameof(FavouritesViewController), null) {
	}

	public override void LoadView() {
		base.LoadView();

		FavouritesTableView.ContentInset = new UIEdgeInsets(8.0f, 0.0f, 8.0f, 0.0f);
	}

	public override void ViewDidLoad() {
		base.ViewDidLoad();
		var refreshControl = new MvxUIRefreshControl();
		FavouritesTableView.RefreshControl = refreshControl;

		var source = new ServerListTableViewSource(FavouritesTableView);

		using var set = this.CreateBindingSet();
		set.Bind(source).For(s => s.ItemsSource).To(vm => vm.Items);
		set.Bind(source).For(s => s.SelectionChangedCommand).To(vm => vm.ItemClickCommand);
		set.Bind(refreshControl).For(r => r.IsRefreshing).To(vm => vm.IsRefreshing);
		set.Bind(refreshControl).For(r => r.RefreshCommand).To(vm => vm.RefreshCommand);
	}

	public override void ViewWillAppear(bool animated) {
		base.ViewWillAppear(animated);

		NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Always;
		NavigationController.NavigationBar.PrefersLargeTitles = true;
	}

	public override MvxBasePresentationAttribute PresentationAttribute(MvxViewModelRequest request) {
		return null;
	}
}