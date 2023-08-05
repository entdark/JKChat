using Android.OS;
using Android.Views;

using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Settings;

namespace JKChat.Android.Views.Settings {
	[TabFragmentPresentation("Settings", Resource.Drawable.ic_settings)]
	public class SettingsFragment : BaseFragment<SettingsViewModel> {
		public SettingsFragment() : base(Resource.Layout.settings_page) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			SetUpNavigation(false);
		}
	}
}