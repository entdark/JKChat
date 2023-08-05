using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Main;

namespace JKChat.Android.Views.Main {
	[RootFragmentPresentation(RegisterBackPressedCallback = true)]
	public class MainFragment : TabsFragment<MainViewModel> {
		public MainFragment() : base(Resource.Layout.main_page, Resource.Id.tabs_viewpager, Resource.Id.tabs_navigationview) {}
	}
}