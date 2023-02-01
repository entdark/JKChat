using Android.OS;
using Android.Views;

using Google.Android.Material.FloatingActionButton;

using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.AdminPanel;
using JKChat.Core.ViewModels.Main;

namespace JKChat.Android.Views.AdminPanel {
	[BottomNavigationViewPresentation(
		"Admin Panel",
		Resource.Id.content_viewpager,
		Resource.Id.navigationview,
		Resource.Drawable.ic_admin_panel,
		typeof(MainViewModel)
	)]
	public class AdminPanelFragment : BaseFragment<AdminPanelViewModel> {
		public AdminPanelFragment() : base(Resource.Layout.admin_panel_page) {
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			var addButton = view.FindViewById<FloatingActionButton>(Resource.Id.add_button);
			SetUpNavigation(false);
		}
	}
}