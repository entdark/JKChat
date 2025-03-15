using Android.OS;
using Android.Views;

using AndroidX.Fragment.App;
using AndroidX.ViewPager.Widget;

using JKChat.Android.Controls;
using JKChat.Core.ViewModels.Base;

namespace JKChat.Android.Views.Base {
	public abstract class TabsFragment<TViewModel> : BaseFragment<TViewModel>, ITabsView where TViewModel : class, IBaseViewModel {
		private readonly int tabsCount;

		protected TabsViewPager ViewPager { get; private set; }
		protected TabsBottomNavigationView BottomNavigationView { get; private set; }

		public Fragment CurrentTabFragment => ViewPager?.CurrentFragment;
		public int CurrentTab => ViewPager?.CurrentItem ?? -1;

		public int ViewPagerId { get; init; }
		public int BottomNavigationViewId { get; init; }
		public int DefaultTab { get; init; } = 0;

		public TabsFragment(int layoutId, int viewPagerId, int bottomNavigationViewId, int tabsCount) : base(layoutId) {
			ViewPagerId = viewPagerId;
			BottomNavigationViewId = bottomNavigationViewId;
			this.tabsCount = tabsCount;
			RegisterBackPressedCallback = true;
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			ViewPager = view.FindViewById<TabsViewPager>(ViewPagerId);
			ViewPager.ScrollEnabled = false;
			ViewPager.OffscreenPageLimit = 3;
			ViewPager.PageSelected += TabPageSelected;
			if (ViewPager.Adapter == null)
				ViewPager.Adapter = new TabsViewPager.TabsAdapter(this.Context, this.ChildFragmentManager, this.tabsCount);
			BottomNavigationView = view.FindViewById<TabsBottomNavigationView>(BottomNavigationViewId);
			BottomNavigationView.ViewPager = ViewPager;
		}

		public override void OnDestroyView() {
			if (ViewPager != null) {
				ViewPager.PageSelected -= TabPageSelected;
			}

			base.OnDestroyView();
		}

		private void TabPageSelected(object sender, ViewPager.PageSelectedEventArgs ev) {
//			ToggleBackPressedCallback(ev.Position != DefaultTab);
		}

		public void CloseFragments(bool animated, int tab = -1) {
			ViewPager.CloseTabsInnerFragments(animated, tab);
		}

		public void MoveToTab(int tab) {
			ViewPager.SetCurrentItem(tab, true);
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
	}
}