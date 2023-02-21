using CoreGraphics;

using JKChat.Core.ViewModels.Settings;
using JKChat.iOS.Views.Base;

using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Views.Settings {
	[MvxTabPresentation(WrapInNavigationController = true, TabName = "Settings", TabIconName = "Settings", TabSelectedIconName = "SettingsSelected")]
	public partial class SettingsViewController : BaseViewController<SettingsViewModel> {
		public SettingsViewController() : base("SettingsViewController", null) {
			SetUpBackButton = false;
		}

		public override void LoadView() {
			base.LoadView();
		}

		public override void ViewDidLoad() {
			base.ViewDidLoad();
			PlayerNameHeaderLabel.Text = "Player Name".ToUpper();
			PlayerNameHeaderLabel.Font = Theme.Font.ErgoeBold(10.0f);

			PlayerNameLabel.Font = Theme.Font.ErgoeMedium(15.0f);

			LocationUpdateLabel.Text = "Location Updates";
			LocationUpdateLabel.Font = Theme.Font.ErgoeMedium(15.0f);
			UpdateViews(this.View.Frame.Size);

            using var set = this.CreateBindingSet();
			set.Bind(PlayerNameLabel).For(v => v.AttributedText).To(vm => vm.PlayerName).WithConversion("ColourText");
			set.Bind(PlayerNameButton).To(vm => vm.PlayerNameCommand);
			set.Bind(LocationUpdateSwitch).For(v => v.On).To(vm => vm.LocationUpdate);
			set.Bind(LocationUpdateButton).To(vm => vm.LocationUpdateCommand);
		}

		public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator) {
			base.ViewWillTransitionToSize(toSize, coordinator);
			UpdateViews(toSize);
		}

		public override MvxBasePresentationAttribute PresentationAttribute(MvxViewModelRequest request) {
			return null;
		}

		private void UpdateViews(CGSize toSize) {
			if (ContentLeftConstraint == null) {
				return;
			}
			if (toSize.Width <= 320.0f) {
				ContentLeftConstraint.Constant = 0.0f;
				ContentRightConstraint.Constant = 0.0f;
				PlayerNameBackgroundView.Layer.CornerRadius = 0.0f;
				LocationUpdateBackgroundView.Layer.CornerRadius = 0.0f;
			} else {
				ContentLeftConstraint.Constant = 20.0f;
				ContentRightConstraint.Constant = 20.0f;
				PlayerNameBackgroundView.Layer.CornerRadius = 14.0f;
				LocationUpdateBackgroundView.Layer.CornerRadius = 14.0f;
			}
			this.View.LayoutIfNeeded();
		}
	}
}

