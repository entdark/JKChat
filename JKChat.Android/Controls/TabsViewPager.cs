using System;
using System.Collections.Generic;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.OS;
using AndroidX.Fragment.App;

using Java.Interop;
using Java.Lang;

using MvvmCross.Platforms.Android.Views;
using MvvmCross.Platforms.Android.Views.ViewPager;
using MvvmCross.ViewModels;

namespace JKChat.Android.Controls {
	[Register("JKChat.Android.Controls.TabsViewPager")]
	public class TabsViewPager : AndroidX.ViewPager.Widget.ViewPager {
		public bool ScrollEnabled { get; set; }

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

		public Fragment CurrentFragment {
			get {
				var adapter = Adapter as MvxCachingFragmentStatePagerAdapter;
				//HACK: actually getting an existing fragment: https://github.com/MvvmCross/MvvmCross/blob/bde315c52b61c84f1e1f0d7f913a1c14359e486b/MvvmCross/Platforms/Android/Views/ViewPager/MvxCachingFragmentPagerAdapter.cs#L93
				//TODO: test if restoration works correctly
				var fragment = adapter?.InstantiateItem(null, CurrentItem) as Fragment;
				return fragment;
			}
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
			private const string bundleFragmentsInfoKey = nameof(MvxCachingFragmentStatePagerAdapter) + nameof(bundleFragmentsInfoKey);
			private readonly FragmentManager fragmentManager;

			protected TabsAdapter(IntPtr javaReference, JniHandleOwnership transfer)
				: base(javaReference, transfer) { }

			public TabsAdapter(Context context, FragmentManager fragmentManager, List<MvxViewPagerFragmentInfo> fragmentsInfo) : base(context, fragmentManager, fragmentsInfo) {
				this.fragmentManager = fragmentManager;
			}

			public override IParcelable SaveState() {
				var bundle = base.SaveState() as Bundle;
				SaveFragmentsInfoState(bundle);
				return bundle;
			}

			public override void RestoreState(IParcelable state, ClassLoader loader) {
				base.RestoreState(state, loader);

				if (state is Bundle bundle) {
					RestoreFragmentsInfoState(bundle);
				}
			}

			public override Fragment GetItem(int position, Fragment.SavedState fragmentSavedState = null) {
				var fragment = base.GetItem(position, fragmentSavedState);

				// If the MvxViewPagerFragmentInfo for this position doesn't have the ViewModel, overwrite it with a new MvxViewPagerFragmentInfo that has the ViewModel we just created.
				// Not doing this means the ViewModel gets recreated every time the Fragment gets recreated!
				if (FragmentsInfo != null && FragmentsInfo.Count > position && fragment is IMvxFragmentView mvxFragment && mvxFragment.ViewModel != null) {
					var oldFragInfo = FragmentsInfo[position];

					if (oldFragInfo != null && oldFragInfo.Request is not MvxViewModelInstanceRequest) {
						var viewModelInstanceRequest = new MvxViewModelInstanceRequest(mvxFragment.ViewModel);
						var newFragInfo = new MvxViewPagerFragmentInfo(oldFragInfo.Title, oldFragInfo.Tag, oldFragInfo.FragmentType, viewModelInstanceRequest);
						FragmentsInfo[position] = newFragInfo;
					}
				}

				return fragment;
			}

			private void SaveFragmentsInfoState(Bundle bundle) {
				if (bundle == null || FragmentsInfo == null || FragmentsInfo.Count == 0)
					return;

				var fragmentInfoParcelables = new IParcelable[FragmentsInfo.Count];
				int i = 0;

				foreach (var fragInfo in FragmentsInfo) {
					var parcelable = new ViewPagerFragmentInfoParcelable() {
						FragmentType = fragInfo.FragmentType,
						ViewModelType = fragInfo.Request.ViewModelType,
						Title = fragInfo.Title,
						Tag = fragInfo.Tag
					};
					fragmentInfoParcelables[i] = parcelable;
					i++;
				}

				bundle.PutParcelableArray(bundleFragmentsInfoKey, fragmentInfoParcelables);
			}

			private void RestoreFragmentsInfoState(Bundle bundle) {
				if (bundle == null)
					return;
				
				var fragmentInfoParcelables = BundleCompat.GetParcelableArray(bundle, bundleFragmentsInfoKey, Class);

				if (fragmentInfoParcelables == null)
					return;

				// First, we create a list of the ViewPager fragments that were restored by Android.
				var fragments = GetFragmentsFromBundle(bundle);

				// Now we get the FragmentInfo data for each fragment from the bundle.
				int i = 0;
				foreach (ViewPagerFragmentInfoParcelable parcelable in fragmentInfoParcelables) {
					MvxViewPagerFragmentInfo fragInfo = null;

					var fragment = fragments[i];

					if (i < fragments.Count) {
						if (fragment is IMvxFragmentView mvxFragment && mvxFragment.ViewModel != null) {
							// The fragment was already restored by Android with its old ViewModel (cached by MvvmCross).
							// Add the ViewModel to the FragmentInfo object so the adapter won't instantiate a new one.
							var viewModelInstanceRequest = new MvxViewModelInstanceRequest(mvxFragment.ViewModel);
							fragInfo = new MvxViewPagerFragmentInfo(parcelable.Title, parcelable.Tag, parcelable.FragmentType, viewModelInstanceRequest);
						}
					}

					if (fragInfo == null) {
						// Either the fragment doesn't exist or it doesn't have a ViewModel. 
						// Fall back to a FragmentInfo with the ViewModelType. The adapter will create a ViewModel in GetItem where we will add it to the FragmentInfo.
						var viewModelRequest = new MvxViewModelRequest(parcelable.ViewModelType);
						fragInfo = new MvxViewPagerFragmentInfo(parcelable.Title, parcelable.Tag, parcelable.FragmentType, viewModelRequest);
					}

					FragmentsInfo.Add(fragInfo);
					i++;
				}

				NotifyDataSetChanged();
			}

			private List<Fragment> GetFragmentsFromBundle(Bundle bundle) {
				var fragments = new List<Fragment>();
				if (bundle == null || fragmentManager == null || fragmentManager.Fragments == null) {
					return fragments;
				}

				// This is how the base adapter retrieves its fragments from the bundle.
				// Copy-pasted here because the base adapter's fragment list is private
				var keys = bundle.KeySet();
				foreach (var key in keys) {
					if (!key.StartsWith("f"))
						continue;

					var index = Integer.ParseInt(key[1..]);

					if (fragmentManager.Fragments == null) return fragments;

					var f = fragmentManager.GetFragment(bundle, key);
					if (f != null) {
						while (fragments.Count <= index)
							fragments.Add(null);

						fragments[index] = f;
					}
				}

				return fragments;
			}
		}

		public class ViewPagerFragmentInfoParcelable : Java.Lang.Object, IParcelable {
			public Type FragmentType { get; init; }
			public Type ViewModelType { get; init; }
			public string Title { get; init; }
			public string Tag { get; init; }

			[ExportField("CREATOR")]
			public static ViewPagerFragmentInfoParcelableCreator InititalizeCreator() {
				return new ViewPagerFragmentInfoParcelableCreator();
			}

			public ViewPagerFragmentInfoParcelable() {
			}

			public ViewPagerFragmentInfoParcelable(Parcel source) {
				string fragmentType = source.ReadString();
				string viewModelType = source.ReadString();
				Title = source.ReadString();
				Tag = source.ReadString();

				FragmentType = Type.GetType(fragmentType);
				ViewModelType = Type.GetType(viewModelType);
			}

			public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags) {
				dest.WriteString(FragmentType.AssemblyQualifiedName);
				dest.WriteString(ViewModelType.AssemblyQualifiedName);
				dest.WriteString(Title);
				dest.WriteString(Tag);
			}

			public int DescribeContents() {
				return 0;
			}
		}

		public sealed class ViewPagerFragmentInfoParcelableCreator : Java.Lang.Object, IParcelableCreator {
			public Java.Lang.Object CreateFromParcel(Parcel source) {
				return new ViewPagerFragmentInfoParcelable(source);
			}

			public Java.Lang.Object []NewArray(int size) {
				return new ViewPagerFragmentInfoParcelable[size];
			}
		}
	}
}