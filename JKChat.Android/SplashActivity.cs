using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MvvmCross.Platforms.Android.Views;

namespace JKChat.Android {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", NoHistory = true, ConfigurationChanges = ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
	public class SplashActivity : MvxSplashScreenActivity {
		public SplashActivity() : base(Resource.Layout.splash_screen) {}
	}
}