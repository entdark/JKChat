using System;

using MvvmCross;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Presenters.Attributes;

namespace JKChat.Android.Presenter.Attributes {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class BottomNavigationViewPresentationAttribute : MvxViewPagerFragmentPresentationAttribute {

		public BottomNavigationViewPresentationAttribute(
			string title,
			int viewPagerResourceId,
			int bottomNavigationViewResourceId,
			int iconDrawableResourceId = int.MinValue,
			Type activityHostViewModelType = null,
			bool addToBackStack = false,
			Type fragmentHostViewType = null,
			bool isCacheableFragment = false) : base(title, viewPagerResourceId, activityHostViewModelType,
			addToBackStack, fragmentHostViewType, isCacheableFragment) {
			BottomNavigationViewResourceId = bottomNavigationViewResourceId;
			IconDrawableResourceId = iconDrawableResourceId;
		}

		public BottomNavigationViewPresentationAttribute(
			string title,
			string viewPagerResourceId,
			string bottomNavigationViewResourceId,
			int iconDrawableResourceId = int.MinValue,
			Type activityHostViewModelType = null,
			bool addToBackStack = false,
			Type fragmentHostViewType = null,
			bool isCacheableFragment = false) : base(title, viewPagerResourceId,
			activityHostViewModelType, addToBackStack, fragmentHostViewType, isCacheableFragment) {
			var context = Mvx.IoCProvider.Resolve<IMvxAndroidGlobals>().ApplicationContext;

			BottomNavigationViewResourceId = !string.IsNullOrEmpty(bottomNavigationViewResourceId)
				? context.Resources!.GetIdentifier(bottomNavigationViewResourceId, "id", context.PackageName)
				: global::Android.Resource.Id.Content;

			IconDrawableResourceId = iconDrawableResourceId;
		}

		/// <summary>
		/// The resource id used to get the BottomNavigationView from the view
		/// </summary>
		public int BottomNavigationViewResourceId { get; set; }

		/// <summary>
		/// The resource id used to get the menu item from the view
		/// </summary>
		public int IconDrawableResourceId { get; set; }
	}
}