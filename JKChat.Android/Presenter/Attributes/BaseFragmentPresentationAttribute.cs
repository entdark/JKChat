using System;

using MvvmCross.Platforms.Android.Presenters.Attributes;

namespace JKChat.Android.Presenter.Attributes {
	public class BaseFragmentPresentationAttribute : MvxFragmentPresentationAttribute {
		public BaseFragmentPresentationAttribute(
			Type activityHostViewModelType = null,
			int fragmentContentId = 16908290,
			bool addToBackStack = false,
			int enterAnimation = int.MinValue,
			int exitAnimation = int.MinValue,
			int popEnterAnimation = int.MinValue,
			int popExitAnimation = int.MinValue,
			int transitionStyle = int.MinValue,
			Type fragmentHostViewType = null,
			bool registerBackPressedCallback = false,
			bool isCacheableFragment = false,
			string tag = null,
			string popBackStackImmediateName = "",
			MvxPopBackStack popBackStackImmediateFlag = MvxPopBackStack.Inclusive,
			bool addFragment = false
		) : base(
			activityHostViewModelType, fragmentContentId, addToBackStack,
			enterAnimation, exitAnimation, popEnterAnimation, popExitAnimation,
			transitionStyle, fragmentHostViewType, isCacheableFragment, tag,
			popBackStackImmediateName, popBackStackImmediateFlag, addFragment
		) {
			RegisterBackPressedCallback = registerBackPressedCallback;
		}

		public BaseFragmentPresentationAttribute(
			Type activityHostViewModelType = null,
			string fragmentContentResourceName = null,
			bool addToBackStack = false,
			string enterAnimation = null,
			string exitAnimation = null,
			string popEnterAnimation = null,
			string popExitAnimation = null,
			string transitionStyle = null,
			Type fragmentHostViewType = null,
			bool registerBackPressedCallback = false,
			bool isCacheableFragment = false,
			string tag = null,
			string popBackStackImmediateName = "",
			MvxPopBackStack popBackStackImmediateFlag = MvxPopBackStack.Inclusive,
			bool addFragment = false
		) : base(
			activityHostViewModelType, fragmentContentResourceName, addToBackStack,
			enterAnimation, exitAnimation, popEnterAnimation, popExitAnimation,
			transitionStyle, fragmentHostViewType, isCacheableFragment, tag,
			popBackStackImmediateName, popBackStackImmediateFlag, addFragment
		) {
			RegisterBackPressedCallback = registerBackPressedCallback;
		}

		public bool RegisterBackPressedCallback { get; set; }
	}
}
