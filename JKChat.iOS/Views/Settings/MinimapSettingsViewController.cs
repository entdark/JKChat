using JKChat.Core.ViewModels.Settings;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;

using UIKit;

namespace JKChat.iOS.Views.Settings;

[MvxSplitViewPresentation(MasterDetailPosition.Detail, WrapInNavigationController = true)]
public partial class MinimapSettingsViewController() : BaseViewController<MinimapSettingsViewModel>(nameof(MinimapSettingsViewController), null) {
	public override void ViewDidLoad () {
		base.ViewDidLoad ();

		var source = new TableGroupedViewSource(MinimapSettingsTableView);

		using var set = this.CreateBindingSet();
		set.Bind(source).For(s => s.ItemsSource).To(vm => vm.Items);
		set.Bind(source).For(s => s.SelectionChangedCommand).To(vm => vm.ItemClickCommand);
	}

	public override void ViewWillAppear(bool animated) {
		base.ViewWillAppear(animated);

		NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Always;
		NavigationController.NavigationBar.PrefersLargeTitles = true;
	}
}