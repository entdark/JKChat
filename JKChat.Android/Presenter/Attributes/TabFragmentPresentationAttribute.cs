using System;

using JKChat.Android.Views.Main;

using MvvmCross;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Presenters.Attributes;

namespace JKChat.Android.Presenter.Attributes {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class TabFragmentPresentationAttribute : MvxViewPagerFragmentPresentationAttribute {
		public int BottomNavigationViewResourceId { get; set; }
		public int IconDrawableResourceId { get; set; }

		public TabFragmentPresentationAttribute(string title, int iconDrawableResourceId) : this(
			title,
			Resource.Id.tabs_viewpager,
			Resource.Id.tabs_navigationview,
			iconDrawableResourceId,
			fragmentHostViewType: typeof(MainFragment)
		) {}

		public TabFragmentPresentationAttribute(
			string title,
			int viewPagerResourceId,
			int bottomNavigationViewResourceId,
			int iconDrawableResourceId = int.MinValue,
			Type activityHostViewModelType = null,
			bool addToBackStack = false,
			Type fragmentHostViewType = null,
			bool isCacheableFragment = false
		) : base(
			title, viewPagerResourceId, activityHostViewModelType,
			addToBackStack, fragmentHostViewType, isCacheableFragment
		) {
			BottomNavigationViewResourceId = bottomNavigationViewResourceId;
			IconDrawableResourceId = iconDrawableResourceId;
		}

		public TabFragmentPresentationAttribute(
			string title,
			string viewPagerResourceId,
			string bottomNavigationViewResourceId,
			int iconDrawableResourceId = int.MinValue,
			Type activityHostViewModelType = null,
			bool addToBackStack = false,
			Type fragmentHostViewType = null,
			bool isCacheableFragment = false
		) : base(
			title, viewPagerResourceId, activityHostViewModelType,
			addToBackStack, fragmentHostViewType, isCacheableFragment
		) {
			var context = Mvx.IoCProvider.Resolve<IMvxAndroidGlobals>().ApplicationContext;

			BottomNavigationViewResourceId = !string.IsNullOrEmpty(bottomNavigationViewResourceId)
				? context.Resources!.GetIdentifier(bottomNavigationViewResourceId, "id", context.PackageName)
				: global::Android.Resource.Id.Content;

			IconDrawableResourceId = iconDrawableResourceId;
		}
	}
}