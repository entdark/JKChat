using System;

using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;

using AndroidX.Fragment.App;

using MvvmCross.Platforms.Android.Views.ViewPager;

namespace JKChat.Android.Controls {
	[Register("JKChat.Android.Controls.TabsViewPager")]
	public class TabsViewPager : AndroidX.ViewPager.Widget.ViewPager {
		public bool ScrollEnabled { get; set; }

		public Fragment CurrentFragment {
			get {
				var fragment = Adapter?.InstantiateItem(null, CurrentItem) as Fragment;
				return fragment;
			}
		}

		public new TabsAdapter Adapter {
			get => base.Adapter as TabsAdapter;
			set => base.Adapter = value;
		}

		public TabsViewPager(Context context) : base(context) {
		}

		public TabsViewPager(Context context, IAttributeSet attrs) : base(context, attrs) {
		}

		protected TabsViewPager(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		public override bool OnTouchEvent(MotionEvent ev) {
			return ScrollEnabled && base.OnTouchEvent(ev);
		}

		public override bool OnInterceptTouchEvent(MotionEvent ev) {
			return ScrollEnabled && base.OnInterceptTouchEvent(ev);
		}

		public void CloseTabsInnerFragments(bool animated, int tab = -1) {
			var adapter = Adapter as TabsAdapter;
			for (int i = 0, count = adapter?.FragmentsInfo?.Count ?? 0; i < count; i++) {
				if (tab >= 0 && tab != i)
					continue;
				var tabFragment = adapter?.InstantiateItem(null, i) as Fragment;
				var fragmentManager = tabFragment?.ChildFragmentManager;
				int backStackCount = fragmentManager?.BackStackEntryCount ?? 0;
//				IBaseFragment.DisableAnimations = !animated;
				for (int j = 0; j < backStackCount; ++j) {
					if (animated)
						fragmentManager.PopBackStackImmediate();
					else
						fragmentManager.PopBackStack();
				}
//				IBaseFragment.DisableAnimations = false;
			}
		}

		//source: https://github.com/anne-k/TabBarViewPagerAdapter
		public class TabsAdapter : MvxCachingFragmentStatePagerAdapter {
			private readonly int tabsCount;

			public override int Count => FragmentsInfo?.Count != tabsCount ? 0 : base.Count;

			protected TabsAdapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

			public TabsAdapter(FragmentManager fragmentManager, int tabsCount) : base(fragmentManager, new()) {
				this.tabsCount = tabsCount;
			}

			public override void NotifyDataSetChanged() {
				if (FragmentsInfo.Count != tabsCount)
					return;
				base.NotifyDataSetChanged();
			}
		}
	}
}