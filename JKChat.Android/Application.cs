using System;

using Android.App;
using Android.Runtime;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;

using MvvmCross.Platforms.Android.Views;

namespace JKChat.Android {
	[Application(
		//AllowBackup = false,
		Icon = "@mipmap/ic_launcher",
		RoundIcon = "@mipmap/ic_launcher_round",
		Label = "@string/app_name",
		Theme = "@style/AppTheme",
		ResizeableActivity = true
	)]
	public class Application : global::Android.App.Application/*MvxAndroidApplication<Setup, App>*/ {
		public Application() {
		}

		public Application(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		public override void OnCreate() {
			AppCenter.Start(Core.ApiKeys.AppCenter.Android, typeof(Crashes));

			base.OnCreate();
		}
	}
}