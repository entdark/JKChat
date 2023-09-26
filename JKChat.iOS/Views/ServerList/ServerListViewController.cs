using System;

using JKChat.Core.ViewModels.ServerList;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Views.ServerList {
	[MvxTabPresentation(WrapInNavigationController = true, TabName = "Server List", TabIconName = "server.rack")]
	public partial class ServerListViewController : BaseViewController<ServerListViewModel> {
		private UISearchBar searchBar;

		public ServerListViewController() : base("ServerListViewController", null) {}

		public override void LoadView() {
			base.LoadView();
			ServerListTableView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;
			ServerListTableView.ContentInset = new UIEdgeInsets(8.0f, 0.0f, 8.0f, 0.0f);

			var searchController = new UISearchController() {
				DimsBackgroundDuringPresentation = false,
				ObscuresBackgroundDuringPresentation = false
			};
			searchBar = searchController.SearchBar;
			searchBar.AutocapitalizationType = UITextAutocapitalizationType.None;
			searchBar.SearchButtonClicked += SearchButtonClicked;
			searchBar.CancelButtonClicked += CancelButtonClicked;
			if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0)) {
				NavigationItem.SearchController = searchController;
			} else {
				ServerListTableView.TableHeaderView = searchBar;
			}
		}

		#region View lifecycle

		public override void ViewDidLoad() {
			base.ViewDidLoad();
			var refreshControl = new MvxUIRefreshControl();
			ServerListTableView.RefreshControl = refreshControl;

			var source = new ServerListTableViewSource(ServerListTableView);

			using var set = this.CreateBindingSet();
			set.Bind(source).For(s => s.ItemsSource).To(vm => vm.Items);
			set.Bind(source).For(s => s.SelectionChangedCommand).To(vm => vm.ItemClickCommand);
			set.Bind(refreshControl).For(r => r.IsRefreshing).To(vm => vm.IsRefreshing);
			set.Bind(refreshControl).For(r => r.RefreshCommand).To(vm => vm.RefreshCommand);
			set.Bind(searchBar).To(vm => vm.SearchText);
		}

		private void CancelButtonClicked(object sender, EventArgs ev) {
			ViewModel.SearchText = string.Empty;
		}

		private void SearchButtonClicked(object sender, EventArgs ev) {
			ResignFirstResponder();
		}

		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);

			var addButtomItem = new UIBarButtonItem(UIBarButtonSystemItem.Add, (sender, ev) => {
				ViewModel.AddServerCommand?.Execute();
			});
			var filterButtomItem = new UIBarButtonItem(UIImage.GetSystemImage("line.3.horizontal.decrease.circle"), UIBarButtonItemStyle.Plain, (sender, ev) => {
				ViewModel.FilterCommand?.Execute();
			});

			NavigationItem.RightBarButtonItems = new []{ addButtomItem, filterButtomItem };
			NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Always;
			NavigationController.NavigationBar.PrefersLargeTitles = true;
		}

		#endregion

		public override MvxBasePresentationAttribute PresentationAttribute(MvxViewModelRequest request) {
			return null;
		}

		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if (searchBar != null) {
				searchBar.SearchButtonClicked -= SearchButtonClicked;
				searchBar.CancelButtonClicked -= CancelButtonClicked;
				searchBar = null;
			}
		}
	}
}