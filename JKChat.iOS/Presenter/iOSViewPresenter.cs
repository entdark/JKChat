using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Navigation.Hints;
using JKChat.iOS.Views.Main;
using MvvmCross.Platforms.Ios.Presenters;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Presenter {
	public class iOSViewPresenter : MvxIosViewPresenter, IViewPresenter {
		public bool IsCollapsed => (SplitViewController as UISplitViewController)?.Collapsed ?? true;

		public iOSViewPresenter(IUIApplicationDelegate applicationDelegate, UIWindow window) : base(applicationDelegate, window) {
		}

		protected override async Task<bool> ShowTabViewController(UIViewController viewController, MvxTabPresentationAttribute attribute, MvxViewModelRequest request) {
			if (TabBarViewController == null) {
				var splitViewModelRequest = new MvxViewModelRequest<WrapperTabBarViewModel>();
				TabBarViewController = (IMvxTabBarViewController)this.CreateViewControllerFor(splitViewModelRequest);
				var splitViewPresentationAttribute = new MvxSplitViewPresentationAttribute(MasterDetailPosition.Master) {
					WrapInNavigationController = true
				};
				await ShowMasterSplitViewController(TabBarViewController as UIViewController, splitViewPresentationAttribute, splitViewModelRequest);
				(TabBarViewController as UIViewController).NavigationController.NavigationBarHidden = true;

				if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0)) {
					var detailViewModelRequest = new MvxViewModelRequest<WrapperDetailViewModel>();
					var detailViewController = (UIViewController)this.CreateViewControllerFor(detailViewModelRequest);
					var detailPresentationAttribute = new MvxSplitViewPresentationAttribute(MasterDetailPosition.Detail) {
						WrapInNavigationController = true
					};
					await ShowDetailSplitViewController(detailViewController, detailPresentationAttribute, detailViewModelRequest);
				}
			}

			return await base.ShowTabViewController(viewController, attribute, request);
		}

		public override async Task<bool> ChangePresentation(MvxPresentationHint hint) {
			if (hint is PopToRootPresentationHint popToRootHint) {
				var splitViewController = SplitViewController as MvxSplitViewController;
				if (popToRootHint.ViewModelType != null && splitViewController?.ViewControllers?.Length >= 2) {
					var request = new MvxViewModelInstanceRequest(popToRootHint.ViewModelType);
					var attributeAction = GetPresentationAttributeAction(request, out var attribute);
					if (attribute is MvxSplitViewPresentationAttribute splitViewAttribute && splitViewAttribute.Position == MasterDetailPosition.Detail)
//					if (attribute is MvxChildPresentationAttribute)
					{
						var navigationController = splitViewController?.ViewControllers?.OfType<MvxNavigationController>()?.LastOrDefault();
//						foreach (var navigationController in splitViewController?.ViewControllers?.OfType<MvxNavigationController>())
						{
							if (navigationController.ViewControllers?.Length > 0)
//							while (navigationController.ViewControllers?.Length > 1)
							{
								var closeViewController = navigationController.ViewControllers.LastOrDefault();
								if ((closeViewController as IMvxIosView)?.ViewModel is IMvxViewModel viewModel && (popToRootHint.Condition?.Invoke(viewModel) ?? true)) {
									await Close(viewModel);
								} else {
									popToRootHint.PoppedToRoot = false;
									return false;
								}
							}
						}
					}
				}
				popToRootHint.PoppedToRoot = true;
				return true;
			}
			return await base.ChangePresentation(hint);
		}

		protected override Task<bool> ShowChildViewController(UIViewController viewController, MvxChildPresentationAttribute attribute, MvxViewModelRequest request) {
			var splitViewController = SplitViewController as MvxSplitViewController;
			foreach (var navigationController in splitViewController?.ViewControllers?.OfType<MvxNavigationController>()) {
				PushViewControllerIntoStack(navigationController, viewController, attribute);
				return Task.FromResult(true);
			}
			return base.ShowChildViewController(viewController, attribute, request);
		}

		protected override Task<bool> CloseChildViewController(IMvxViewModel viewModel, MvxChildPresentationAttribute attribute) {
			var splitViewController = SplitViewController as MvxSplitViewController;
			foreach (var navigationController in splitViewController?.ViewControllers?.OfType<MvxNavigationController>()) {
				if (TryCloseViewControllerInsideStack(navigationController, viewModel, attribute)) {
					return Task.FromResult(true);
				}
			}
			return base.CloseChildViewController(viewModel, attribute);
		}

		protected override async Task<bool> CloseDetailSplitViewController(IMvxViewModel viewModel, MvxSplitViewPresentationAttribute attribute) {
			bool close = await base.CloseDetailSplitViewController(viewModel, attribute);
			if (SplitViewController is MainViewController splitViewController) {
				splitViewController.ShowWrapper();
			}
			return close;
		}
	}

	public class WrapperTabBarViewModel : MvxViewModel { }
	public class WrapperTabBarViewController : MvxTabBarViewController<WrapperTabBarViewModel> {
		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
			NavigationController.NavigationBarHidden = true;
		}
	}

	public class WrapperDetailViewModel : MvxViewModel { }
	public class WrapperDetailViewController : MvxViewController<WrapperDetailViewModel> {
		public override void ViewDidLoad() {
			base.ViewDidLoad();
			View.BackgroundColor = Theme.Color.Background;
		}
	}
}
