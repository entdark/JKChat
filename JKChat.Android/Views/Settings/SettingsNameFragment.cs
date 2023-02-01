using Android.OS;
using Android.Views;

using Google.Android.Material.AppBar;

using Java.Lang;

using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Settings;

namespace JKChat.Android.Views.Settings {
	[PushFragmentPresentation]
	public class SettingsNameFragment : BaseFragment<SettingsNameViewModel> {
		public SettingsNameFragment() : base(Resource.Layout.settings_name_page, Resource.Menu.apply_item) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			var appBarLayout = view.FindViewById<AppBarLayout>(Resource.Id.appbarlayout);
			appBarLayout.OffsetChanged += (sender, ev) => {
				int toolBarHeight = Toolbar.MeasuredHeight;
				int appBarHeight = appBarLayout.MeasuredHeight;
				int verticalOffset = ev.VerticalOffset;
				float f = (float)(appBarHeight - toolBarHeight + verticalOffset) / (appBarHeight - toolBarHeight);
				Toolbar.Alpha = f;
			};
		}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();
			/*var applyItem = Menu.FindItem(Resource.Id.apply_item);
			applyItem.SetClickAction(() => {
				ViewModel.ApplyCommand?.Execute();
			});*/
		}
	}
}