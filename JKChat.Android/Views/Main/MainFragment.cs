using Android.OS;
using Android.Views;
using Android.Views.Animations;

using AndroidX.ViewPager.Widget;

using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Main;

namespace JKChat.Android.Views.Main {
	[RootFragmentPresentation(RegisterBackPressedCallback = true)]
	public class MainFragment() : TabsFragment<MainViewModel>(Resource.Layout.main_page, Resource.Id.tabs_viewpager, Resource.Id.tabs_navigationview, 3, Resource.Id.tabs_navigationrailview) {
		private int tabChanged = 0;

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			
			BottomNavigationView.HandleItemSelection = (position) => {
				if (position != CurrentTab) {
					var animation = AnimationUtils.LoadAnimation(this.Context, position > CurrentTab ? Resource.Animation.fragment_tab_left_exit : Resource.Animation.fragment_tab_right_exit);
					CurrentTabFragment.View.StartAnimation(animation);
					tabChanged = position > CurrentTab ? 1 : -1;
				}
				return true;
			};

			NavigationRailView.HandleItemSelection = (position) => {
				if (position != CurrentTab) {
					var animation = AnimationUtils.LoadAnimation(this.Context, position > CurrentTab ? Resource.Animation.fragment_rail_top_exit : Resource.Animation.fragment_rail_bottom_exit);
					CurrentTabFragment.View.StartAnimation(animation);
					tabChanged = position > CurrentTab ? 1 : -1;
				}
				return true;
			};
		}

		protected override void TabPageSelected(object sender, ViewPager.PageSelectedEventArgs ev) {
			base.TabPageSelected(sender, ev);
			if (tabChanged != 0) {
				Animation animation;
				if (BaseActivity is { ExpandedWindow: true } activity) {
					animation = AnimationUtils.LoadAnimation(this.Context, tabChanged > 0 ? Resource.Animation.fragment_rail_bottom_enter : Resource.Animation.fragment_rail_top_enter);
				} else {
					animation = AnimationUtils.LoadAnimation(this.Context, tabChanged > 0 ? Resource.Animation.fragment_tab_right_enter : Resource.Animation.fragment_tab_left_enter);
				}
				CurrentTabFragment.View.StartAnimation(animation);
				tabChanged = 0;
			}
		}
	}
}