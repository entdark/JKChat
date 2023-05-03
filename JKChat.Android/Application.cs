using System;
using System.Text;

using Android.App;
using Android.Runtime;

using JKChat.Core;

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
	public class Application : MvxAndroidApplication<Setup, App> {
		public Application() {
		}

		public Application(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		public override void OnCreate() {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			AppCenter.Start(Core.ApiKeys.AppCenter.Android, typeof(Crashes));

			base.OnCreate();
		}
	}
}