using System;
using System.Collections.Generic;

using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace JKChat.Android.Controls {
	[Register("JKChat.Android.Controls.BottomNavigationView")]
	public class BottomNavigationView : Google.Android.Material.BottomNavigation.BottomNavigationView {
		private ViewPager viewPager;
		public ViewPager ViewPager {
			get => viewPager;
			set {
				if (viewPager != null) {
					viewPager.PageSelected -= PageSelected;
				}
				viewPager = value;
				if (viewPager != null) {
					viewPager.ScrollEnabled = false;
					viewPager.PageSelected += PageSelected;
				}
			}
		}

		private void PageSelected(object sender, AndroidX.ViewPager.Widget.ViewPager.PageSelectedEventArgs ev) {
			this.Menu.GetItem(ev.Position).SetChecked(true);
		}

		private readonly Dictionary<Type, IMenuItem> pages = new Dictionary<Type, IMenuItem>();

		protected BottomNavigationView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
			Initialize();
		}

		public BottomNavigationView(Context context) : base(context) {
			Initialize();
		}

		public BottomNavigationView(Context context, IAttributeSet attrs) : base(context, attrs) {
			Initialize();
		}

		public BottomNavigationView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {
			Initialize();
		}

		private void Initialize() {
			ItemSelected += NavigationItemSelected;
			ItemReselected += NavigationItemReselected;
		}

		private void NavigationItemSelected(object sender, ItemSelectedEventArgs ev) {
			ViewPager?.SetCurrentItem(ev.Item.ItemId, true);
			ev.Handled = true;
		}

		private void NavigationItemReselected(object sender, ItemReselectedEventArgs ev) {
			ev.Item.SetChecked(false);
		}

		public virtual bool DidRegisterViewModelType(Type viewModelType) {
			return pages.ContainsKey(viewModelType);
		}

		public virtual void RegisterViewModel(IMenuItem menuItem, Type viewModelType) {
			pages.Add(viewModelType, menuItem);
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				ItemSelected -= NavigationItemSelected;
				ItemReselected -= NavigationItemReselected;
				ViewPager = null;
			}

			base.Dispose(disposing);
		}
	}
}