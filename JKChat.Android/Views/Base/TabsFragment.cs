using Android.OS;
using Android.Views;

using AndroidX.Fragment.App;
using AndroidX.ViewPager.Widget;

using JKChat.Android.Controls;
using JKChat.Core.ViewModels.Base;

namespace JKChat.Android.Views.Base {
	public abstract class TabsFragment<TViewModel>(int layoutId, int viewPagerId, int bottomNavigationViewId, int tabsCount, int navigationRailViewId = int.MinValue) : BaseFragment<TViewModel>(layoutId), ITabsView where TViewModel : class, IBaseViewModel {
		protected TabsViewPager ViewPager { get; private set; }
		protected TabsBottomNavigationView BottomNavigationView { get; private set; }
		protected TabsNavigationRailView NavigationRailView { get; private set; }

		public Fragment CurrentTabFragment => ViewPager?.CurrentFragment;
		public int CurrentTab => ViewPager?.CurrentItem ?? -1;
		public int DefaultTab { get; init; } = 0;

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			RegisterBackPressedCallback = true;
			ViewPager = view.FindViewById<TabsViewPager>(viewPagerId);
			ViewPager.ScrollEnabled = false;
			ViewPager.OffscreenPageLimit = tabsCount;
			ViewPager.PageSelected += TabPageSelected;
			if (ViewPager.Adapter == null)
				ViewPager.Adapter = new TabsViewPager.TabsAdapter(this.ChildFragmentManager, tabsCount);
			BottomNavigationView = view.FindViewById<TabsBottomNavigationView>(bottomNavigationViewId);
			BottomNavigationView.ViewPager = ViewPager;
			NavigationRailView = view.FindViewById<TabsNavigationRailView>(navigationRailViewId);
			NavigationRailView.ViewPager = ViewPager;
			base.OnViewCreated(view, savedInstanceState);
		}

		public override void OnDestroyView() {
			if (ViewPager != null) {
				ViewPager.PageSelected -= TabPageSelected;
			}

			base.OnDestroyView();
		}

		protected virtual void TabPageSelected(object sender, ViewPager.PageSelectedEventArgs ev) {
		}

		public void CloseFragments(bool animated, int tab = -1) {
			ViewPager.CloseTabsInnerFragments(animated, tab);
		}

		public void MoveToTab(int tab) {
			BottomNavigationView.SelectedItemId = tab;
			NavigationRailView.SelectedItemId = tab;
		}

		protected override void OnBackPressedCallback() {
			if (ViewPager.CurrentFragment is var fragment) {
				var baseFragment = fragment as IBaseFragment;
				bool handled = baseFragment?.OnBackPressed() ?? false;
				if (handled)
					return;
			}
			if (CurrentTab != DefaultTab) {
				MoveToTab(DefaultTab);
			} else {
				ToggleBackPressedCallback(false);
				Activity.OnBackPressedDispatcher.OnBackPressed();
				ToggleBackPressedCallback(true);
			}
		}

		protected override void OnConfigurationChanged(LayoutState layoutState, bool landscape) {
			switch (layoutState) {
				case LayoutState.Small:// when !landscape:
					BottomNavigationView.Visibility = ViewStates.Visible;
					NavigationRailView.Visibility = ViewStates.Gone;
					break;
/*				case LayoutState.Small when landscape:
					BottomNavigationView.Visibility = ViewStates.Gone;
					NavigationRailView.Visibility = ViewStates.Visible;
					NavigationRailView.Collapse();
					break;*/
				case LayoutState.Medium:
					BottomNavigationView.Visibility = ViewStates.Gone;
					NavigationRailView.Visibility = ViewStates.Visible;
					NavigationRailView.Collapse();
					break;
				case LayoutState.Large:
					BottomNavigationView.Visibility = ViewStates.Gone;
					NavigationRailView.Visibility = ViewStates.Visible;
					NavigationRailView.Expand();
					break;
			}
		}
	}
}