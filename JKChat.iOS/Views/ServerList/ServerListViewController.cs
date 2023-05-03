using System;
using System.Collections.Specialized;

using Foundation;

using JKChat.Core.ViewModels.ServerList;
using JKChat.iOS.Views.Base;
using JKChat.iOS.Views.ServerList.Cells;

using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Views.ServerList {
	[MvxTabPresentation(WrapInNavigationController = true, TabName = "Server List", TabIconName = "ServerList", TabSelectedIconName = "ServerListSelected")]
	public partial class ServerListViewController : BaseViewController<ServerListViewModel> {
		private UISearchBar searchBar;

		public ServerListViewController() : base("ServerListViewController", null) {
			SetUpBackButton = false;
		}

		public override void DidReceiveMemoryWarning() {
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();

			// Release any cached data, images, etc that aren't in use.
		}

		public override void LoadView() {
			base.LoadView();
			ServerListTableView.RegisterNibForCellReuse(ServerListViewCell.Nib, ServerListViewCell.Key);
			ServerListTableView.ContentInset = new UIEdgeInsets(15.0f, 0.0f, 15.0f, 0.0f);
			ServerListTableView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;
			ServerListTableView.AutomaticallyAdjustsScrollIndicatorInsets = false;

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
			var refreshControl = new MvxUIRefreshControl() {
				TintColor = Theme.Color.Accent
			};
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

			NavigationItem.RightBarButtonItem = addButtomItem;
		}

		public override void ViewDidAppear(bool animated) {
			base.ViewDidAppear(animated);
		}

		public override void ViewWillDisappear(bool animated) {
			base.ViewWillDisappear(animated);
		}

		public override void ViewDidDisappear(bool animated) {
			base.ViewDidDisappear(animated);
		}

		#endregion

		public override MvxBasePresentationAttribute PresentationAttribute(MvxViewModelRequest request) {
			return null;
		}

		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if (searchBar != null) {
				searchBar.SearchButtonClicked += SearchButtonClicked;
				searchBar.CancelButtonClicked += CancelButtonClicked;
				searchBar = null;
			}
		}
	}

	public class ServerListTableViewSource : MvxStandardTableViewSource {
		public ServerListTableViewSource(UITableView tableView) : base(tableView, ServerListViewCell.Key) {
			tableView.Source = this;
			this.UseAnimations = true;
			this.AddAnimation = UITableViewRowAnimation.Top;
			this.RemoveAnimation = UITableViewRowAnimation.Bottom;
		}

		protected override void CollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args){
			base.CollectionChangedOnCollectionChanged(sender, args);
			if (args.Action == NotifyCollectionChangedAction.Add) {
				TableView.ScrollToRow(NSIndexPath.FromRowSection(0, 0), UITableViewScrollPosition.Top, true);
			}
		}
	}
}