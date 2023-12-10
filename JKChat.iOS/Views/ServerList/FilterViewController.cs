using JKChat.Core.ViewModels.ServerList;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;

using UIKit;

namespace JKChat.iOS.Views.ServerList;

[MvxSplitViewPresentation(MasterDetailPosition.Detail, WrapInNavigationController = true)]
public partial class FilterViewController : BaseViewController<FilterViewModel> {
	private UIBarButtonItem resetButtonItem;

	public FilterViewController() : base(nameof(FilterViewController), null) {}

	public override void LoadView() {
		base.LoadView();

		resetButtonItem = new UIBarButtonItem("Reset", UIBarButtonItemStyle.Plain, (sender, ev) => {});
	}

	public override void ViewDidLoad() {
		base.ViewDidLoad();

		var source = new TableGroupedViewSource(FilterTableView);

		using var set = this.CreateBindingSet();
		set.Bind(source).For(s => s.ItemsSource).To(vm => vm.Items);
		set.Bind(source).For(s => s.SelectionChangedCommand).To(vm => vm.ItemClickCommand);
		set.Bind(resetButtonItem).For("Clicked").To(vm => vm.ResetCommand);
	}

	public override void ViewWillAppear(bool animated) {
		base.ViewWillAppear(animated);

		NavigationItem.RightBarButtonItem = resetButtonItem;
		NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Always;
		NavigationController.NavigationBar.PrefersLargeTitles = true;
	}
}