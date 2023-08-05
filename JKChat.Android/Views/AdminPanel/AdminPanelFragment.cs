using Android.OS;
using Android.Views;

using Google.Android.Material.FloatingActionButton;

using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.AdminPanel;

namespace JKChat.Android.Views.AdminPanel {
	[TabFragmentPresentation("Admin Panel", Resource.Drawable.ic_admin_panel)]
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