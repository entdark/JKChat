using System.Linq;

using Foundation;

using JKChat.Core.ViewModels.Main;
using JKChat.iOS.Presenter;

using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Views.Main {
	[MvxRootPresentation]
	public class MainViewController : MvxSplitViewController<MainViewModel>, IUISplitViewControllerDelegate {
		public MainViewController() {
			CollapseSecondViewController = (split, secondary, primary) => {
				var vcs = split.ViewControllers;
				bool collapse = false;
				if (secondary is UINavigationController navigation2) {
					collapse = navigation2.ViewControllers?.LastOrDefault() is WrapperDetailViewController;
					if (!collapse) {
						navigation2.NavigationBarHidden = false;
					}
				}
				if (primary is UINavigationController navigation) {
					navigation.NavigationBarHidden = collapse;
				}
				return collapse;
			};
			PresentsWithGesture = true;
		}

		private void MainViewControllerWillChangeDisplayMode(object sender, UISplitViewControllerDisplayModeEventArgs ev) {
		}

		[Export("splitViewController:willChangeToDisplayMode:")]
		public new void WillChangeDisplayMode(UISplitViewController svc, UISplitViewControllerDisplayMode displayMode) {
		}

		public override void ShowDetailViewController(UIViewController vc, NSObject sender) {
			delayedShowWrapper = false;
			base.ShowDetailViewController(vc, sender);
		}

		public override void ViewDidLoad() {
			base.ViewDidLoad();
			var observer = NSNotificationCenter.DefaultCenter.AddObserver(ShowDetailTargetDidChangeNotification, DetailTargetNotification);
			View.BackgroundColor = UIColor.SystemBackground;
			if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0)) {
//				PreferredSplitBehavior = UISplitViewControllerSplitBehavior.Tile;
				PreferredDisplayMode = UISplitViewControllerDisplayMode.OneBesideSecondary;
			}
		}

		private void DetailTargetNotification(NSNotification notification) {
			if (!Collapsed) {
				if (delayedShowWrapper) {
					ShowWrapperActually();
				}
			}
		}

		private bool delayedShowWrapper = false;
		public void ShowWrapper() {
			if (Collapsed && ViewControllers?[0] is UINavigationController navigationController) {
				navigationController.PopToRootViewController(true);
				delayedShowWrapper = true;
			} else {
				ShowWrapperActually();
			}
		}

		private void ShowWrapperActually() {
			var detailViewModelRequest = new MvxViewModelRequest<WrapperDetailViewModel>();
			var detailViewController = (UIViewController)this.CreateViewControllerFor(detailViewModelRequest);
			var detailPresentationAttribute = new MvxSplitViewPresentationAttribute(MasterDetailPosition.Detail) {
				WrapInNavigationController = true
			};
			ShowDetailView(detailViewController, detailPresentationAttribute);
		}
	}
}