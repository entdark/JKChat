using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using AndroidX.Fragment.App;

using JKChat.Android.Controls;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.Navigation.Hints;
using JKChat.Core.ViewModels.Base;

using Microsoft.Extensions.Logging;

using MvvmCross.Exceptions;
using MvvmCross.Logging;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Presenters;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Presenters;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

namespace JKChat.Android.Presenter {
	public class AndroidViewPresenter : MvxAndroidViewPresenter {
		public AndroidViewPresenter(IEnumerable<Assembly> androidViewAssemblies) : base(androidViewAssemblies) {}

		public override void RegisterAttributeTypes() {
			base.RegisterAttributeTypes();
			AttributeTypesToActionsDictionary.Register<RootFragmentPresentationAttribute>(ShowRootFragment, CloseRootFragment);
			AttributeTypesToActionsDictionary.Register<TabFragmentPresentationAttribute>(ShowTabFragment, CloseViewPagerFragment);
			AttributeTypesToActionsDictionary.Register<PushFragmentPresentationAttribute>(ShowPushFragment, ClosePushFragment);
			AttributeTypesToActionsDictionary.Register<ModalFragmentPresentationAttribute>(ShowModalFragment, CloseModalFragment);
		}

		protected Task<bool> ShowPushFragment(Type view, PushFragmentPresentationAttribute attribute, MvxViewModelRequest request) {
			return base.ShowFragment(view, attribute, request);
		}

		protected Task<bool> ClosePushFragment(IMvxViewModel viewModel, PushFragmentPresentationAttribute attribute) {
			return base.CloseFragment(viewModel, attribute);
		}

		protected Task<bool> ShowModalFragment(Type view, ModalFragmentPresentationAttribute attribute, MvxViewModelRequest request) {
			return base.ShowFragment(view, attribute, request);
		}

		protected Task<bool> CloseModalFragment(IMvxViewModel viewModel, ModalFragmentPresentationAttribute attribute) {
			return base.CloseFragment(viewModel, attribute);
		}

		protected virtual async Task<bool> ShowTabFragment(Type view, TabFragmentPresentationAttribute attribute, MvxViewModelRequest request) {
//			ValidateArguments(view, attribute, request);
			var showViewPagerFragment = await ShowViewPagerFragment(view, attribute, request).ConfigureAwait(true);
			if (!showViewPagerFragment)
				return false;

			TabsViewPager viewPager = null;
			TabsBottomNavigationView bottomNavigationView = null;

			// check for a ViewPager inside a Fragment
			if (attribute.FragmentHostViewType != null) {
				var fragment = GetFragmentByViewType(attribute.FragmentHostViewType);

				viewPager = fragment?.View.FindViewById<TabsViewPager>(attribute.ViewPagerResourceId);
				bottomNavigationView = fragment?.View.FindViewById<TabsBottomNavigationView>(attribute.BottomNavigationViewResourceId);
			}

			// check for a ViewPager inside an Activity
			if (CurrentActivity.IsActivityAlive() && attribute?.ActivityHostViewModelType != null) {
				viewPager = CurrentActivity?.FindViewById<TabsViewPager>(attribute.ViewPagerResourceId);
				bottomNavigationView = CurrentActivity?.FindViewById<TabsBottomNavigationView>(attribute.BottomNavigationViewResourceId);
			}

			if (viewPager == null || bottomNavigationView == null)
				throw new MvxException("ViewPager or BottomNavigationView not found");

			bottomNavigationView.ViewPager ??= viewPager;
			if (!bottomNavigationView.TryRegisterViewModel(request.ViewModelType, attribute.Title, attribute.IconDrawableResourceId))
				return false;

			return true;
		}

		protected Task<bool> ShowRootFragment(Type view, MvxFragmentPresentationAttribute attribute, MvxViewModelRequest request) {
			if (attribute.FragmentHostViewType != null) {
				ShowNestedFragment(view, attribute, request);
				return Task.FromResult(true);
			}
			if (attribute.ActivityHostViewModelType == null) {
				attribute.ActivityHostViewModelType = GetCurrentActivityViewModelType();
			}
			Type currentActivityViewModelType = GetCurrentActivityViewModelType();
			if (attribute.ActivityHostViewModelType != currentActivityViewModelType) {
				MvxLogHost.Default?.Log(LogLevel.Warning, "Activity host with ViewModelType {activityHostViewModelType} is not CurrentTopActivity. Showing Activity before showing Fragment for {viewModelType}", new object[2] { attribute.ActivityHostViewModelType, attribute.ViewModelType });
				PendingRequest = request;
				ShowHostActivity(attribute);
			} else if (CurrentActivity.IsActivityAlive()) {
				if (CurrentActivity.FindViewById(attribute.FragmentContentId) == null) {
					throw new InvalidOperationException("FrameLayout to show Fragment not found");
				}
				CloseFragments(false);
				PerformShowFragmentTransaction(CurrentActivity.SupportFragmentManager, attribute, request);
			}
			return Task.FromResult(true);
		}

		protected Task<bool> CloseRootFragment(IMvxViewModel viewModel, MvxFragmentPresentationAttribute attribute) {
			return base.CloseFragment(viewModel, attribute);
		}

		protected void CloseFragments(bool animated) {
			if (CurrentFragmentManager.Fragments?.FirstOrDefault() is ITabsView tabsFragment) {
//				tabsFragment.CloseFragments(animated);
			}

			if (!animated)
				IBaseFragment.DisableAnimations = true;
			for (int i = 0, count = CurrentFragmentManager.BackStackEntryCount; i < count; ++i) {
				if (animated) {
					CurrentFragmentManager.PopBackStack();
				} else {
					CurrentFragmentManager.PopBackStackImmediate();
				}
			}
			IBaseFragment.DisableAnimations = false;
		}

		protected override IMvxFragmentView CreateFragment(FragmentManager fragmentManager, MvxBasePresentationAttribute attribute, Type fragmentType) {
			var fragmentView = base.CreateFragment(fragmentManager, attribute, fragmentType);
			if (fragmentView is IBaseFragment baseFragment) {
				int order = 0;
				if (attribute is PushFragmentPresentationAttribute || attribute is ModalFragmentPresentationAttribute) {
					order = (fragmentManager?.BackStackEntryCount ?? 0) + 1;
				} else if (attribute is RootFragmentPresentationAttribute) {
					order = 0;
				}
				//if nested fragments
				if (fragmentManager != CurrentFragmentManager) {
					order += (CurrentFragmentManager?.BackStackEntryCount ?? 0);
				}
				baseFragment.Order = order;
				if (attribute is BaseFragmentPresentationAttribute baseAttribute) {
					baseFragment.RegisterBackPressedCallback = baseAttribute.RegisterBackPressedCallback;
				}
			}
			return fragmentView;
		}

		public override async Task<bool> ChangePresentation(MvxPresentationHint hint) {
			if (hint is PopToRootPresentationHint popToRootHint) {
				if (popToRootHint.ViewModelType != null && CurrentFragmentManager?.BackStackEntryCount > 0) {
					var request = new MvxViewModelInstanceRequest(popToRootHint.ViewModelType);
					var attributeAction = GetPresentationAttributeAction(request, out var attribute);
					if (attribute is MvxFragmentPresentationAttribute fragmentAttribute) {
						while (CurrentFragmentManager.BackStackEntryCount > 0) {
							var fragment = CurrentFragmentManager?.Fragments?.LastOrDefault();
							if (fragment is IMvxFragmentView { ViewModel: IMvxViewModel viewModel } && (viewModel.GetType() != popToRootHint.ViewModelType
									|| (viewModel is IFromRootNavigatingViewModel fromRootViewModel && fromRootViewModel.ShouldLetOtherNavigateFromRoot(popToRootHint.Data)))) {
								await Close(viewModel);
							} else {
								popToRootHint.PoppedToRoot = false;
								return false;
							}
						}
					}
				}
				CloseFragments();
				popToRootHint.PoppedToRoot = true;
				return true;
			}/* else if (hint is MoveToTabPresentationHint moveToTabHint) {
				if (CurrentFragmentManager.Fragments?.FirstOrDefault() is ITabsView tabsFragment) {
					int oldCurrentTab = tabsFragment.CurrentTab;
					tabsFragment.MoveToTab(moveToTabHint.Tab);
					if (moveToTabHint.PopToRoot) {
						tabsFragment.CloseFragments(true, tabsFragment.CurrentTab);
						if (oldCurrentTab != tabsFragment.CurrentTab)
							tabsFragment.CloseFragments(true, oldCurrentTab);
					}
					return true;
				}
				return false;
			}*/
			return await base.ChangePresentation(hint);
		}
	}
}