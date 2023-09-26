using JKChat.Core.ViewModels.Settings;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Views.Settings {
	[MvxTabPresentation(WrapInNavigationController = true, TabName = "Settings", TabIconName = "gear")]
	public partial class SettingsViewController : BaseViewController<SettingsViewModel> {
		public SettingsViewController() : base("SettingsViewController", null) {}

		public override void LoadView() {
			base.LoadView();

			SettingsTableView.ContentInset = new UIEdgeInsets(18.0f, 0.0f, 18.0f, 0.0f);
		}

		public override void ViewDidLoad() {
			base.ViewDidLoad();

			var source = new TableGroupedViewSource(SettingsTableView);

			using var set = this.CreateBindingSet();
			set.Bind(source).For(s => s.ItemsSource).To(vm => vm.Items);
			set.Bind(source).For(s => s.SelectionChangedCommand).To(vm => vm.ItemClickCommand);
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
}