using System;
using System.Collections.Generic;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;

using AndroidX.Core.OS;

using Java.Interop;

using JKChat.Core;

using MvvmCross.Exceptions;

namespace JKChat.Android.Controls {
	[Register("JKChat.Android.Controls.TabsBottomNavigationView")]
	public class TabsBottomNavigationView : Google.Android.Material.BottomNavigation.BottomNavigationView {
		private const string bundlePages = nameof(TabsBottomNavigationView) + nameof(bundlePages);
		private const string bundleCurrentIndex = nameof(TabsBottomNavigationView) + nameof(bundleCurrentIndex);
		private const string bundleSavedState = nameof(TabsBottomNavigationView) + nameof(bundleSavedState);

		private TabsViewPager viewPager;
		public TabsViewPager ViewPager {
			get => viewPager;
			set {
				if (viewPager != null)
					viewPager.PageSelected -= PageSelected;
				viewPager = value;
				if (viewPager != null)
					viewPager.PageSelected += PageSelected;
			}
		}

		public Func<int, bool> HandleItemSelection { get; set; }

		private void PageSelected(object sender, AndroidX.ViewPager.Widget.ViewPager.PageSelectedEventArgs ev) {
			this.Menu.FindItem(ev.Position)?.SetChecked(true);
		}

		private readonly Dictionary<Type, Tuple<int, int>> pages = new();

		public TabsBottomNavigationView(Context context) : base(context) {
			Initialize();
		}

		public TabsBottomNavigationView(Context context, IAttributeSet attrs) : base(context, attrs) {
			Initialize();
		}

		public TabsBottomNavigationView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {
			Initialize();
		}

		protected TabsBottomNavigationView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
			Initialize();
		}

		private void Initialize() {
			ItemSelected += NavigationItemSelected;
			ItemReselected += NavigationItemReselected;
		}

		private void NavigationItemSelected(object sender, ItemSelectedEventArgs ev) {
			bool allow = HandleItemSelection?.Invoke(ev.Item.ItemId) ?? true;
			if (!allow)
				return;
			ViewPager?.SetCurrentItem(ev.Item.ItemId, true);
			ev.Handled = true;
		}

		private void NavigationItemReselected(object sender, ItemReselectedEventArgs ev) {
			ev.Item.SetChecked(false);
		}

		public bool TryRegisterViewModel(Type viewModelType, string title, int iconDrawableResourceId) {
			int menuId = ViewPager.Adapter.Count - 1;
			return TryRegisterViewModel(viewModelType, title, iconDrawableResourceId, menuId);
		}

		private bool TryRegisterViewModel(Type viewModelType, string title, int iconDrawableResourceId, int menuId) {
			if (pages.ContainsKey(viewModelType))
				return false;

			// item id should match index of page.
			var menuItem = Menu.Add(0, menuId, 0, new Java.Lang.String(title));

			if (menuItem is null)
				throw new MvxException("Failed to create BottomNavigationView MenuItem");

			if (iconDrawableResourceId != int.MinValue)
				menuItem.SetIcon(iconDrawableResourceId);

			pages.Add(viewModelType, new(menuId, iconDrawableResourceId));
			return true;
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				ItemSelected -= NavigationItemSelected;
				ItemReselected -= NavigationItemReselected;
				ViewPager = null;
			}

			base.Dispose(disposing);
		}

		protected override IParcelable OnSaveInstanceState() {
			var parcelable = base.OnSaveInstanceState();
			var bundle = new Bundle();
			bundle.PutParcelable(bundleSavedState, parcelable);
			if (pages.IsNullOrEmpty())
				return bundle;

			var pagesParcelable = new IParcelable[pages.Count];
			int i = 0;

			foreach (var page in pages) {
				var menuItem = Menu.GetItem(page.Value.Item1);
				var pageParcelable = new TabsBottomNavigationViewPageParcelable() {
					Type = page.Key,
					MenuId = menuItem.ItemId,
					MenuTitle = (menuItem.TitleFormatted as Java.Lang.String)?.ToString() ?? string.Empty,
					MenuDrawableResourceId = page.Value.Item2
				};
				pagesParcelable[i] = pageParcelable;
				i++;
			}

			bundle.PutInt(bundleCurrentIndex, ViewPager?.CurrentItem ?? 0);
			bundle.PutParcelableArray(bundlePages, pagesParcelable);
			return bundle;
		}

		protected override void OnRestoreInstanceState(IParcelable state) {
			var bundle = state as Bundle;
			var parcelable = bundle != null ? (BundleCompat.GetParcelable(bundle, bundleSavedState, Class) as IParcelable) : state;
			base.OnRestoreInstanceState(parcelable);

			if (bundle != null) {
				var pagesParcelable = BundleCompat.GetParcelableArray(bundle, bundlePages, Class);

				if (pagesParcelable == null)
					return;

				pages.Clear();

				foreach (TabsBottomNavigationViewPageParcelable pageParcelable in pagesParcelable)
					TryRegisterViewModel(pageParcelable.Type, pageParcelable.MenuTitle, pageParcelable.MenuDrawableResourceId, pageParcelable.MenuId);

				int currentItem = bundle.GetInt(bundleCurrentIndex, 0);
				Menu.FindItem(currentItem)?.SetChecked(true);
			}
		}

		public class TabsBottomNavigationViewPageParcelable : Java.Lang.Object, IParcelable {
			public Type Type { get; init; }
			public int MenuId { get; init; }
			public string MenuTitle { get; init; }
			public int MenuDrawableResourceId { get; init; }

			[ExportField("CREATOR")]
			public static TabsBottomNavigationViewPageParcelableCreator InititalizeCreator() {
				return new TabsBottomNavigationViewPageParcelableCreator();
			}

			public TabsBottomNavigationViewPageParcelable() {}

			public TabsBottomNavigationViewPageParcelable(Parcel source) {
				string type = source.ReadString();
				MenuId = source.ReadInt();
				MenuTitle = source.ReadString();
				MenuDrawableResourceId = source.ReadInt();

				Type = Type.GetType(type);
			}

			public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags) {
				dest.WriteString(Type.AssemblyQualifiedName);
				dest.WriteInt(MenuId);
				dest.WriteString(MenuTitle);
				dest.WriteInt(MenuDrawableResourceId);
			}

			public int DescribeContents() {
				return 0;
			}
		}

		public sealed class TabsBottomNavigationViewPageParcelableCreator : Java.Lang.Object, IParcelableCreator {
			public Java.Lang.Object CreateFromParcel(Parcel source) {
				return new TabsBottomNavigationViewPageParcelable(source);
			}

			public Java.Lang.Object []NewArray(int size) {
				return new TabsBottomNavigationViewPageParcelable[size];
			}
		}
	}
}