using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Android.Views.Chat;
using JKChat.Core.ViewModels.Main;
using JKChat.Core.ViewModels.Settings;

using MvvmCross.Platforms.Android.Presenters.Attributes;

namespace JKChat.Android.Views.Settings {
	[BottomNavigationViewPresentation(
		"Settings",
		Resource.Id.content_viewpager,
		Resource.Id.navigationview,
		Resource.Drawable.ic_settings,
		typeof(MainViewModel)
	)]
	public class SettingsFragment : BaseFragment<SettingsViewModel> {
		public SettingsFragment() : base(Resource.Layout.settings_page) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			SetUpNavigation(false);
		}

		protected override void ActivityExit() {}

		protected override void ActivityPopEnter() {}
	}
	[MvxFragmentPresentation(
		typeof(MainViewModel),
		Resource.Id.content_secondary,
		true,
		Resource.Animation.fragment_slide_rtl,
		Resource.Animation.fragment_hslide_rtl,
		Resource.Animation.fragment_hslide_ltr,
		Resource.Animation.fragment_slide_ltr
	)]
	public class SettingsFragment2 : BaseFragment<SettingsViewModel2> {
		public SettingsFragment2() : base(Resource.Layout.settings_page) { }
	}
}