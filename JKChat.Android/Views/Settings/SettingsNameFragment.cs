using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Settings;

namespace JKChat.Android.Views.Settings {
	[PushFragmentPresentation]
	public class SettingsNameFragment : BaseFragment<SettingsNameViewModel> {
		public SettingsNameFragment() : base(Resource.Layout.settings_name_page, Resource.Menu.apply_item) {}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();
			var applyItem = Menu.FindItem(Resource.Id.apply_item);
			applyItem.SetClickAction(() => {
				ViewModel.ApplyCommand?.Execute();
			});
		}
	}
}