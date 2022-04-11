using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Android.Views;

using JKChat.Android.Controls;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.Navigation.Hints;

using MvvmCross.Exceptions;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Presenters;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Presenters;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

namespace JKChat.Android.Presenter {
	public class AndroidViewPresenter : MvxAndroidViewPresenter {
		public AndroidViewPresenter(IEnumerable<Assembly> androidViewAssemblies) : base(androidViewAssemblies) {
		}

		public override void RegisterAttributeTypes() {
			base.RegisterAttributeTypes();
			AttributeTypesToActionsDictionary.Register<BottomNavigationViewPresentationAttribute>(ShowBottomNavigationFragment, CloseViewPagerFragment);
			AttributeTypesToActionsDictionary.Register<PushFragmentPresentationAttribute>(ShowFragment, CloseFragment);
		}

		protected virtual async Task<bool> ShowBottomNavigationFragment(
			Type view,
			BottomNavigationViewPresentationAttribute attribute,
			MvxViewModelRequest request) {
			//ValidateArguments(view, attribute, request);

			var showViewPagerFragment = await ShowViewPagerFragment(view, attribute, request).ConfigureAwait(true);
			if (!showViewPagerFragment)
				return false;

			ViewPager viewPager = null;
			BottomNavigationView bottomNavigationView = null;

			// check for a ViewPager inside a Fragment
			if (attribute.FragmentHostViewType != null) {
				var fragment = GetFragmentByViewType(attribute.FragmentHostViewType);

				viewPager = fragment?.View.FindViewById<ViewPager>(attribute.ViewPagerResourceId);
				bottomNavigationView = fragment?.View.FindViewById<BottomNavigationView>(attribute.BottomNavigationViewResourceId);
			}

			// check for a ViewPager inside an Activity
			if (CurrentActivity.IsActivityAlive() && attribute?.ActivityHostViewModelType != null) {
				viewPager = CurrentActivity?.FindViewById<ViewPager>(attribute.ViewPagerResourceId);
				bottomNavigationView = CurrentActivity?.FindViewById<BottomNavigationView>(attribute.BottomNavigationViewResourceId);
			}

			if (viewPager == null || bottomNavigationView == null)
				throw new MvxException("ViewPager or BottomNavigationView not found");

			bottomNavigationView.ViewPager ??= viewPager;
			if (!bottomNavigationView.DidRegisterViewModelType(request.ViewModelType!)) {
				// item id should match index of page.
				var menuItem = bottomNavigationView.Menu.Add(0, viewPager.Adapter.Count - 1, 0, attribute?.Title);

				if (menuItem is null)
					throw new MvxException("Failed to create BottomNavigationView MenuItem");

				if (attribute!.IconDrawableResourceId != int.MinValue) {
					menuItem.SetIcon(attribute.IconDrawableResourceId);
				}

				bottomNavigationView.RegisterViewModel(menuItem, request.ViewModelType);
			}

			return true;
		}

		public override async Task<bool> ChangePresentation(MvxPresentationHint hint) {
			if (hint is PopToRootPresentationHint popToRootHint) {
				if (popToRootHint.ViewModelType != null && CurrentFragmentManager?.BackStackEntryCount > 0) {
					var request = new MvxViewModelInstanceRequest(popToRootHint.ViewModelType);
					var attributeAction = GetPresentationAttributeAction(request, out var attribute);
					if (attribute is MvxFragmentPresentationAttribute fragmentAttribute) {
						while (CurrentFragmentManager.BackStackEntryCount > 0) {
							var fragment = CurrentFragmentManager?.Fragments?.LastOrDefault();
							if ((fragment as IMvxFragmentView)?.ViewModel is IMvxViewModel viewModel && (popToRootHint.Condition?.Invoke(viewModel) ?? true)) {
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
			}
			return await base.ChangePresentation(hint);
		}

		protected override IMvxFragmentView CreateFragment(AndroidX.Fragment.App.FragmentManager fragmentManager, MvxBasePresentationAttribute attribute, Type fragmentType) {
			var fragmentView = base.CreateFragment(fragmentManager, attribute, fragmentType);
			if (fragmentView is IBaseFragment baseFragment) {
				baseFragment.Order = fragmentManager?.BackStackEntryCount ?? 0;
			}
			return fragmentView;
		}
	}
}